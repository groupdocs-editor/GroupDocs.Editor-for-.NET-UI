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


namespace GroupDocs.Editor.UI.Api.Services.Implementation;

public class AwsS3Storage : IStorage, IDisposable
{
    private readonly IAmazonS3 _s3Client;
    private readonly ILogger<AwsS3Storage> _logger;
    private readonly IIdGeneratorService _idGenerator;
    private readonly AwsOptions _customAwsOptions;

    public AwsS3Storage(
        IAmazonS3 s3Client,
        ILogger<AwsS3Storage> logger,
        IIdGeneratorService idGenerator,
        IOptions<AwsOptions> customAwsOptions)
    {
        _s3Client = s3Client;
        _logger = logger;
        _idGenerator = idGenerator;
        _customAwsOptions = customAwsOptions.Value;
    }

    public async Task<IEnumerable<StorageResponse<StorageMetaFile>>> UploadFiles(IEnumerable<UploadOriginalRequest> files)
    {
        if (object.ReferenceEquals(null, files) || files.Any() == false)
        {
            return new List<StorageResponse<StorageMetaFile>>
            {
                StorageResponse<StorageMetaFile>.CreateNotExist(new StorageMetaFile
                {
                    DocumentCode = _idGenerator.GenerateEmptyDocumentCode(),
                    OriginalFile = new StorageFile
                    {
                        DocumentCode = _idGenerator.GenerateEmptyDocumentCode(),
                        FileName = string.Empty
                    }
                })
            };
        }
        List<StorageResponse<StorageMetaFile>> result = new List<StorageResponse<StorageMetaFile>>();
        foreach (UploadOriginalRequest one in files)
        {
            try
            {
                Guid documentCode = _idGenerator.GenerateDocumentCode();
                var storageFiles = await this.SaveFile(new[] { one.FileContent }, documentCode);
                StorageResponse<StorageFile> storageFileSingle = storageFiles.FirstOrDefault();                
                if (storageFileSingle == null || storageFileSingle is not { IsSuccess: true })
                {
                    result.Add(StorageResponse<StorageMetaFile>.CreateFailed(new StorageMetaFile()));
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
                result.Add(StorageResponse<StorageMetaFile>.CreateFailed(new StorageMetaFile
                {
                    DocumentInfo = one.DocumentInfo,
                    DocumentCode = Guid.Empty,
                    OriginalFile = new StorageFile
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
        List<StorageResponse<StorageFile>> totalResult = new List<StorageResponse<StorageFile>>();

        foreach (FileContent one in fileContents)
        {
            PutObjectRequest request = new PutObjectRequest
            {
                BucketName = this._customAwsOptions.BucketName,
                Key = one.FileName,
                InputStream = one.ResourceStream,
            };
            PutObjectResponse oneResult = await this._s3Client.PutObjectAsync(request);
            StorageResponse<StorageFile> oneResultFinal = StorageResponse<StorageFile>.CreateSuccess(new StorageFile
            {
                DocumentCode = documentCode,
                FileLink = one.FileName,
                FileName = one.FileName
            });
            totalResult.Add(oneResultFinal);
        }
        return totalResult;
    }

    public StorageResponse RemoveFolder(string folderSubPath)
    {
        if (string.IsNullOrWhiteSpace(folderSubPath)) { return StorageResponse.CreateFailed(); }

        ListObjectsV2Request listObjectsRequest = new ListObjectsV2Request
        {
            BucketName = this._customAwsOptions.BucketName,
            Prefix = folderSubPath
        };
        DeleteObjectsRequest deleteObjectsRequest = new DeleteObjectsRequest
        {
            BucketName = this._customAwsOptions.BucketName
        };

        ListObjectsV2Response listObjectsResponse = null;
        int deletedCounter = 0;
        do
        {
            listObjectsResponse = this._s3Client.ListObjectsV2Async(listObjectsRequest).Result;

            foreach (S3Object item in listObjectsResponse.S3Objects)
            {
                deleteObjectsRequest.AddKey(item.Key);
                if (deleteObjectsRequest.Objects.Count == 1000)
                {
                    this._s3Client.DeleteObjectsAsync(deleteObjectsRequest);
                    deletedCounter += deleteObjectsRequest.Objects.Count;
                    deleteObjectsRequest.Objects.Clear();
                }
            }
            listObjectsRequest.ContinuationToken = listObjectsResponse.NextContinuationToken;
        } while (listObjectsResponse.IsTruncated);
        //delete remnants (lesser then 1000) after all per-1000 deletions within loop above
        if (deleteObjectsRequest.Objects.Count > 0)
        {
            deletedCounter += deleteObjectsRequest.Objects.Count;
            this._s3Client.DeleteObjectsAsync(deleteObjectsRequest);
        }
        if (deletedCounter == 0)
        {
            return StorageResponse.CreateNotExist();
        }
        else
        {
            return StorageResponse.CreateSuccess();
        }
    }

    public StorageResponse RemoveFile(string fileSubPath)
    {
        if (this.Exists(fileSubPath) == false)
        {
            return StorageResponse.CreateNotExist();
        }
        DeleteObjectRequest request = new DeleteObjectRequest
        {
            BucketName = this._customAwsOptions.BucketName,
            Key = fileSubPath
        };
        DeleteObjectResponse result = this._s3Client.DeleteObjectAsync(request).Result;
        return StorageResponse.CreateSuccess();        
    }

    private bool Exists(string fileKey)
    {
        GetObjectMetadataRequest request = new GetObjectMetadataRequest()
        {
            BucketName = this._customAwsOptions.BucketName,
            Key = fileKey
        };
        try
        {
            GetObjectMetadataResponse result = this._s3Client.GetObjectMetadataAsync(request).Result;
            return result != null;
        }
        catch(System.AggregateException ex)
        {
            if (ex.InnerException != null & ex.InnerException is Amazon.S3.AmazonS3Exception && 
                ((Amazon.S3.AmazonS3Exception)ex.InnerException).StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
            else
            {
                //status wasn't not found, so throw the exception
                throw;
            }            
        }
    }

    public async Task<StorageDisposableResponse<Stream>> DownloadFile(string fileSubPath)
    {
        GetObjectRequest request = new GetObjectRequest
        {
            BucketName = this._customAwsOptions.BucketName,
            Key = fileSubPath,
        };
        try
        {
            using (GetObjectResponse response = await this._s3Client.GetObjectAsync(request))
            {
                if (response.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return StorageDisposableResponse<Stream>.CreateNotExist(Stream.Null);
                }
                using (Stream responseStream = response.ResponseStream)
                {
                    MemoryStream memoryStream = new MemoryStream();
                    responseStream.CopyTo(memoryStream);

                    return StorageDisposableResponse<Stream>.CreateSuccess(memoryStream);
                }
            }
        }
        catch(Amazon.S3.AmazonS3Exception ex)
        {
            if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return StorageDisposableResponse<Stream>.CreateNotExist(Stream.Null);
            }
            throw;
        }        
    }

    public async Task<StorageResponse<string>> GetFileText(string fileSubPath)
    {
        StorageDisposableResponse<Stream> file = await this.DownloadFile(fileSubPath);
        if (file.IsSuccess == false)
        {
            return StorageResponse<string>.CreateFailed(string.Empty);
        }
        using (StreamReader reader = new StreamReader(file.Response))
        {
            string content = reader.ReadToEnd();
            return StorageResponse<string>.CreateSuccess(content);
        }
    }

    public async Task<StorageResponse<StorageSubFile>> UpdateHtmlContent(StorageSubFile currentContent, string htmlContents)
    {
        this.RemoveFile(Path.Combine(currentContent.DocumentCode.ToString(), currentContent.SubCode.ToString(),
            currentContent.EditedHtmlName));
        using var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        await writer.WriteAsync(htmlContents);
        await writer.FlushAsync();
        stream.Seek(0, SeekOrigin.Begin);
        var storageFile =
            (await this.SaveFile(new[] { new FileContent { FileName = currentContent.EditedHtmlName, ResourceStream = stream } },
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
            this.RemoveFile(Path.Combine(currentContent.DocumentCode.ToString(), currentContent.SubCode.ToString(), resource.OldResorceName));
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
            (await this.SaveFile(new[] { new FileContent { FileName = resource.File.FileName, ResourceStream = fileStream } },
                currentContent.DocumentCode, currentContent.SubCode.ToString())).FirstOrDefault();
        if (storageFile is not { IsSuccess: true } || storageFile.Response == null)
        {
            return StorageUpdateResourceResponse<StorageSubFile, StorageFile>.CreateFailed(new StorageSubFile(), new StorageFile());
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