using Amazon.S3;
using Amazon.S3.Model;
using GroupDocs.Editor.UI.Api.Controllers.RequestModels;
using GroupDocs.Editor.UI.Api.Models.DocumentConvertor;
using GroupDocs.Editor.UI.Api.Models.Storage;
using GroupDocs.Editor.UI.Api.Models.Storage.Requests;
using GroupDocs.Editor.UI.Api.Models.Storage.Responses;
using GroupDocs.Editor.UI.Api.Services.Interfaces;
using GroupDocs.Editor.UI.Api.Services.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;


namespace GroupDocs.Editor.UI.Api.Services.Implementation;

public class AwsS3Storage : IStorage, IDisposable
{
    private readonly IAmazonS3 _s3Client;
    private readonly ILogger<AwsS3Storage> _logger;
    private readonly IIdGeneratorService _idGenerator;
    private readonly AwsOptions _customAwsOptions;

    public AwsS3Storage(
        ILogger<AwsS3Storage> logger,
        IIdGeneratorService idGenerator,
        IOptions<AwsOptions> customAwsOptions)
    {
        _customAwsOptions = customAwsOptions.Value;
        _s3Client = new AmazonS3Client(_customAwsOptions.AccessKey, _customAwsOptions.SecretKey, Amazon.RegionEndpoint.GetBySystemName(_customAwsOptions.Region));
        _logger = logger;
        _idGenerator = idGenerator;
    }

    public async Task<IEnumerable<StorageResponse<StorageMetaFile>>> UploadFiles(IEnumerable<UploadOriginalRequest> files)
    {
        var uploadOriginalRequests = files.ToList();
        if (!uploadOriginalRequests.Any())
        {
            return new List<StorageResponse<StorageMetaFile>>
            {
                StorageResponse<StorageMetaFile>.CreateNotExist(new()
                {
                    DocumentCode = _idGenerator.GenerateEmptyDocumentCode(),
                    OriginalFile = new()
                    {
                        DocumentCode = _idGenerator.GenerateEmptyDocumentCode(),
                        FileName = string.Empty
                    }
                })
            };
        }
        List<StorageResponse<StorageMetaFile>> result = new();
        foreach (UploadOriginalRequest one in uploadOriginalRequests)
        {
            try
            {
                await using Stream stream = new MemoryStream();
                await one.FileContent.ResourceStream.CopyToAsync(stream);
                stream.Seek(0, SeekOrigin.Begin);
                Guid documentCode = _idGenerator.GenerateDocumentCode();
                var storageFiles =
                    await SaveFile(
                        new[] { new FileContent { FileName = one.FileContent.FileName, ResourceStream = stream } },
                        documentCode);
                StorageResponse<StorageFile>? storageFileSingle = storageFiles.FirstOrDefault();
                if (storageFileSingle is not { IsSuccess: true })
                {
                    result.Add(StorageResponse<StorageMetaFile>.CreateFailed(new()));
                    continue;
                }
                StorageMetaFile metaFile = new()
                {
                    DocumentInfo = one.DocumentInfo,
                    DocumentCode = documentCode,
                    OriginalFile = storageFileSingle.Response ?? throw new FileLoadException()
                };
                result.Add(StorageResponse<StorageMetaFile>.CreateSuccess(metaFile));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed save file {FileName}", one.FileContent.FileName);
                result.Add(StorageResponse<StorageMetaFile>.CreateFailed(new()
                {
                    DocumentInfo = one.DocumentInfo,
                    DocumentCode = Guid.Empty,
                    OriginalFile = new()
                    {
                        DocumentCode = _idGenerator.GenerateEmptyDocumentCode(),
                        FileName = one.FileContent.FileName
                    }
                }));
            }

        }
        return result;
    }

    public async Task<IEnumerable<StorageResponse<StorageFile>>> SaveFile(IEnumerable<FileContent> fileContents, Guid documentCode, string subIndex = "")
    {
        List<StorageResponse<StorageFile>> totalResult = new();
        foreach (FileContent one in fileContents)
        {
            var awsFileName = BuildUrlName(new[] { documentCode.ToString(), subIndex, one.FileName });
            PutObjectRequest request = new()
            {
                BucketName = _customAwsOptions.Bucket,
                Key = awsFileName,
                InputStream = one.ResourceStream,
            };
            PutObjectResponse oneResult = await _s3Client.PutObjectAsync(request);
            if (oneResult.HttpStatusCode != HttpStatusCode.OK) continue;

            StorageResponse<StorageFile> oneResultFinal = StorageResponse<StorageFile>.CreateSuccess(new StorageFile
            {
                DocumentCode = documentCode,
                FileLink = GetFileLink(awsFileName),
                FileName = one.FileName
            });
            totalResult.Add(oneResultFinal);
        }
        return totalResult;
    }

    public async Task<StorageResponse> RemoveFolder(string folderSubPath)
    {
        if (string.IsNullOrWhiteSpace(folderSubPath)) { return StorageResponse.CreateFailed(); }
        ListObjectsV2Request listObjectsRequest = new()
        {
            BucketName = _customAwsOptions.Bucket,
            Prefix = folderSubPath
        };
        DeleteObjectsRequest deleteObjectsRequest = new()
        {
            BucketName = _customAwsOptions.Bucket
        };

        ListObjectsV2Response listObjectsResponse = await _s3Client.ListObjectsV2Async(listObjectsRequest);
        foreach (var s3Object in listObjectsResponse.S3Objects)
        {
            deleteObjectsRequest.AddKey(s3Object.Key);
        }

        if (!deleteObjectsRequest.Objects.Any()) return StorageResponse.CreateNotExist();
        var deleteObjects = await _s3Client.DeleteObjectsAsync(deleteObjectsRequest);
        return deleteObjects.HttpStatusCode == HttpStatusCode.OK ? StorageResponse.CreateSuccess() : StorageResponse.CreateNotExist();

    }

    public async Task<StorageResponse> RemoveFile(string fileSubPath)
    {
        if (!await Exists(fileSubPath))
        {
            return StorageResponse.CreateNotExist();
        }
        DeleteObjectRequest request = new()
        {
            BucketName = _customAwsOptions.Bucket,
            Key = fileSubPath
        };
        DeleteObjectResponse result = await _s3Client.DeleteObjectAsync(request);
        return result.HttpStatusCode == HttpStatusCode.NoContent ? StorageResponse.CreateSuccess() : StorageResponse.CreateNotExist();
    }

    private async Task<bool> Exists(string fileKey)
    {
        try
        {
            GetObjectMetadataRequest request = new()
            {
                BucketName = _customAwsOptions.Bucket,
                Key = fileKey
            };
            GetObjectMetadataResponse result = await _s3Client.GetObjectMetadataAsync(request);

            return true;
        }

        catch (Amazon.S3.AmazonS3Exception ex)
        {
            if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                return false;

            //status wasn't not found, so throw the exception
            throw;
        }
    }

    public async Task<StorageDisposableResponse<Stream>> DownloadFile(string fileSubPath)
    {
        GetObjectRequest request = new()
        {
            BucketName = _customAwsOptions.Bucket,
            Key = fileSubPath,
        };
        try
        {
            using GetObjectResponse response = await _s3Client.GetObjectAsync(request);
            MemoryStream memoryStream = new();
            await response.ResponseStream.CopyToAsync(memoryStream);
            return response.HttpStatusCode == HttpStatusCode.NotFound
            ? StorageDisposableResponse<Stream>.CreateNotExist(Stream.Null)
            : StorageDisposableResponse<Stream>.CreateSuccess(memoryStream);
        }
        catch (AmazonS3Exception ex)
        {
            if (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return StorageDisposableResponse<Stream>.CreateNotExist(Stream.Null);
            }
            throw;
        }
    }

    public async Task<StorageResponse<string>> GetFileText(string fileSubPath)
    {
        StorageDisposableResponse<Stream> file = await DownloadFile(fileSubPath);
        if (file.Response == null || file.IsSuccess == false)
        {
            return StorageResponse<string>.CreateFailed(string.Empty);
        }

        using StreamReader reader = new(file.Response);
        string content = await reader.ReadToEndAsync();
        return StorageResponse<string>.CreateSuccess(content);
    }

    public async Task<StorageResponse<StorageSubFile>> UpdateHtmlContent(StorageSubFile currentContent, string htmlContents)
    {
        await RemoveFile(Path.Combine(currentContent.DocumentCode.ToString(), currentContent.SubCode.ToString(),
            currentContent.EditedHtmlName));
        using var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        await writer.WriteAsync(htmlContents);
        await writer.FlushAsync();
        stream.Seek(0, SeekOrigin.Begin);
        var storageFile =
            (await SaveFile(new[] { new FileContent { FileName = currentContent.EditedHtmlName, ResourceStream = stream } },
                currentContent.DocumentCode, currentContent.SubCode.ToString())).FirstOrDefault();
        if (storageFile is not { IsSuccess: true } || storageFile.Response == null)
        {
            return StorageResponse<StorageSubFile>.CreateFailed(new StorageSubFile());
        }
        currentContent.IsEdited = true;
        currentContent.SourceDocument = storageFile.Response;
        return StorageResponse<StorageSubFile>.CreateSuccess(currentContent);
    }

    public async Task<StorageUpdateResourceResponse<StorageSubFile, StorageFile>> UpdateResource(StorageSubFile currentContent, UploadResourceRequest resource)
    {
        if (!string.IsNullOrWhiteSpace(resource.OldResorceName))
        {
            await RemoveFile(Path.Combine(currentContent.DocumentCode.ToString(), currentContent.SubCode.ToString(), resource.OldResorceName));
            var resourceFile = currentContent.AllResources.FirstOrDefault(a => a.FileName.Equals(resource.OldResorceName));
            if (resourceFile != null)
            {
                switch (resource.ResourceType)
                {
                    case ResourceType.Stylesheet:
                        currentContent.Stylesheets.Remove(resourceFile);
                        break;
                    case ResourceType.Image:
                        currentContent.Images.Remove(resourceFile);
                        break;
                    case ResourceType.Font:
                        currentContent.Fonts.Remove(resourceFile);
                        break;
                    case ResourceType.Audio:
                        currentContent.Audios.Remove(resourceFile);
                        break;
                    default:
                        _logger.LogError("Resource with type: {type} is unknown", resource.ResourceType);
                        throw new ArgumentOutOfRangeException($"Resource with type: {resource.ResourceType} is unknown");
                }
            }
        }

        await using var fileStream = resource.File.OpenReadStream();
        var storageFile =
            (await SaveFile(new[] { new FileContent { FileName = resource.File.FileName, ResourceStream = fileStream } },
                currentContent.DocumentCode, currentContent.SubCode.ToString())).FirstOrDefault();
        if (storageFile is not { IsSuccess: true } || storageFile.Response == null)
        {
            return StorageUpdateResourceResponse<StorageSubFile, StorageFile>.CreateFailed(new(), new());
        }
        switch (resource.ResourceType)
        {
            case ResourceType.Stylesheet:
                currentContent.Stylesheets.Add(storageFile.Response);
                break;
            case ResourceType.Image:
                currentContent.Images.Add(storageFile.Response);
                break;
            case ResourceType.Font:
                currentContent.Fonts.Add(storageFile.Response);
                break;
            case ResourceType.Audio:
                currentContent.Audios.Add(storageFile.Response);
                break;
            default:
                _logger.LogError("Resource with type: {type} is unknown", resource.ResourceType);
                throw new ArgumentOutOfRangeException($"Resource with type: {resource.ResourceType} is unknown");
        }

        return StorageUpdateResourceResponse<StorageSubFile, StorageFile>.CreateSuccess(currentContent, storageFile.Response);
    }

    public string GetFileLink(string fileKey)
    {
        GetPreSignedUrlRequest request = new()
        {
            BucketName = _customAwsOptions.Bucket,
            Key = fileKey,
            Expires = DateTime.Now.AddDays(_customAwsOptions.LinkExpiresDays ?? 1)
        };
        return _s3Client.GetPreSignedURL(request);
    }

    public string BuildUrlName(string[] names)
    {
        var nonEmptyNames = new[] { _customAwsOptions.RootFolderName }.Union(names).Where(name => !string.IsNullOrWhiteSpace(name));
        return string.Join('/', nonEmptyNames);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _s3Client.Dispose();
        }
    }
}