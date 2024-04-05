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

    public async Task<IEnumerable<StorageResponse<StorageFile>>> SaveFile(IEnumerable<FileContent> fileContents, PathBuilder prefixPath)
    {
        List<StorageResponse<StorageFile>> totalResult = new();
        foreach (FileContent one in fileContents)
        {
            var awsFileName = BuildUrlName(prefixPath.AppendKey(one.FileName).ToAwsPath());
            PutObjectRequest request = new()
            {
                BucketName = _customAwsOptions.Bucket,
                Key = awsFileName,
                InputStream = one.ResourceStream,
                AutoCloseStream = false
            };
            PutObjectResponse oneResult = await _s3Client.PutObjectAsync(request);
            if (oneResult.HttpStatusCode != HttpStatusCode.OK) continue;

            StorageResponse<StorageFile> oneResultFinal = StorageResponse<StorageFile>.CreateSuccess(new StorageFile
            {
                DocumentCode = prefixPath.DocumentCode,
                FileLink = GetFileLink(awsFileName),
                FileName = one.FileName,
                ResourceType = one.ResourceType
            });
            totalResult.Add(oneResultFinal);
        }
        return totalResult;
    }

    public async Task<StorageResponse> RemoveFolder(PathBuilder path)
    {
        ListObjectsV2Request listObjectsRequest = new()
        {
            BucketName = _customAwsOptions.Bucket,
            Prefix = BuildUrlName(path.ToAwsPath())
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

    public async Task<StorageResponse> RemoveFile(PathBuilder path)
    {
        if (!await Exists(path))
        {
            return StorageResponse.CreateNotExist();
        }
        DeleteObjectRequest request = new()
        {
            BucketName = _customAwsOptions.Bucket,
            Key = BuildUrlName(path.ToAwsPath())
        };
        DeleteObjectResponse result = await _s3Client.DeleteObjectAsync(request);
        return result.HttpStatusCode == HttpStatusCode.NoContent ? StorageResponse.CreateSuccess() : StorageResponse.CreateNotExist();
    }

    private async Task<bool> Exists(PathBuilder path)
    {
        try
        {
            GetObjectMetadataRequest request = new()
            {
                BucketName = _customAwsOptions.Bucket,
                Key = BuildUrlName(path.ToAwsPath())
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

    public async Task<StorageDisposableResponse<Stream>> DownloadFile(PathBuilder path)
    {
        GetObjectRequest request = new()
        {
            BucketName = _customAwsOptions.Bucket,
            Key = BuildUrlName(path.ToAwsPath()),
        };
        try
        {
            using GetObjectResponse response = await _s3Client.GetObjectAsync(request);
            MemoryStream memoryStream = new();
            await response.ResponseStream.CopyToAsync(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
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

    public async Task<StorageResponse<string>> GetFileText(PathBuilder path)
    {
        StorageDisposableResponse<Stream> file = await DownloadFile(path);
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

    public string BuildUrlName(string path)
    {
        return string.Join('/', _customAwsOptions.RootFolderName, path);
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