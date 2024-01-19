using Amazon.S3;
using Amazon.S3.Model;
using GroupDocs.Editor.UI.Api.Models.DocumentConvertor;
using GroupDocs.Editor.UI.Api.Models.Storage;
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
    private readonly AwsOptions _customAwsOptions;

    public AwsS3Storage(
        ILogger<AwsS3Storage> logger,
        IOptions<AwsOptions> customAwsOptions)
    {
        _customAwsOptions = customAwsOptions.Value;
        _s3Client = new AmazonS3Client(_customAwsOptions.AccessKey, _customAwsOptions.SecretKey, Amazon.RegionEndpoint.GetBySystemName(_customAwsOptions.Region));
        _logger = logger;
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

        catch (AmazonS3Exception ex)
        {
            if (ex.StatusCode == HttpStatusCode.NotFound)
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