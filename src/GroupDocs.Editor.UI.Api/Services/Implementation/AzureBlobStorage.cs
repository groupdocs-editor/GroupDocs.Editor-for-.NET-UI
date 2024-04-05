using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using GroupDocs.Editor.UI.Api.Models.DocumentConvertor;
using GroupDocs.Editor.UI.Api.Models.Storage;
using GroupDocs.Editor.UI.Api.Models.Storage.Responses;
using GroupDocs.Editor.UI.Api.Services.Interfaces;
using GroupDocs.Editor.UI.Api.Services.Options;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GroupDocs.Editor.UI.Api.Services.Implementation;

public class AzureBlobStorage : IStorage
{
    private readonly BlobContainerClient _client;
    private readonly ILogger<AzureBlobStorage> _logger;
    private readonly AzureBlobOptions _azureBlobOptions;
    public AzureBlobStorage(
        IOptions<AzureBlobOptions> options,
        ILogger<AzureBlobStorage> logger)
    {
        _azureBlobOptions = options.Value;
        string connStr = $"DefaultEndpointsProtocol=https;AccountName={_azureBlobOptions.AccountName};AccountKey={_azureBlobOptions.AccountKey}";
        BlobContainerClient client = new(connStr, _azureBlobOptions.ContainerName);
        _client = client;
        _logger = logger;
    }

    public async Task<IEnumerable<StorageResponse<StorageFile>>> SaveFile(IEnumerable<FileContent> fileContents, PathBuilder prefixPath)
    {
        List<StorageResponse<StorageFile>> totalResult = new();

        foreach (FileContent one in fileContents)
        {
            var azureFileName = prefixPath.ToAzurePath();
            BlobClient blob = _client.GetBlobClient(azureFileName);
            Azure.Response<BlobContentInfo> result;
            try
            {
                one.ResourceStream.Seek(0, SeekOrigin.Begin);
                result = await blob.UploadAsync(one.ResourceStream);
            }
            catch (Azure.RequestFailedException ex)
            {
                if (ex.ErrorCode == "BlobAlreadyExists")
                {
                    StorageResponse<StorageFile> oneResultFinal = StorageResponse<StorageFile>.CreateSuccess(new StorageFile
                    {
                        DocumentCode = prefixPath.DocumentCode,
                        FileLink = azureFileName,
                        FileName = one.FileName
                    });
                    totalResult.Add(oneResultFinal);
                    continue;
                }

                throw;
            }
            string reason = result.GetRawResponse().ReasonPhrase;
            if (reason == "Created")
            {
                StorageResponse<StorageFile> oneResultFinal = StorageResponse<StorageFile>.CreateSuccess(new StorageFile
                {
                    DocumentCode = prefixPath.DocumentCode,
                    FileLink = GetFileLink(blob, azureFileName),
                    FileName = one.FileName
                });
                totalResult.Add(oneResultFinal);
            }
            else
            {
                throw new Exception($"Response reason phrase is {reason}");
            }
        }
        return totalResult;
    }

    public async Task<StorageResponse> RemoveFolder(PathBuilder path)
    {

        Azure.Pageable<BlobItem> blobItems = _client.GetBlobs(prefix: path.ToAzurePath());
        int deletedCounter = 0;
        foreach (BlobItem blobItem in blobItems)
        {
            BlobClient blobClient = _client.GetBlobClient(blobItem.Name);
            await blobClient.DeleteIfExistsAsync();
            deletedCounter++;
        }
        return deletedCounter == 0 ? StorageResponse.CreateNotExist() : StorageResponse.CreateSuccess();
    }

    public async Task<StorageResponse> RemoveFile(PathBuilder path)
    {
        BlobClient blob = _client.GetBlobClient(path.ToAzurePath());
        Azure.Response<bool> result = await blob.DeleteIfExistsAsync();
        return result.Value ? StorageResponse.CreateSuccess() : StorageResponse.CreateNotExist();
    }

    public async Task<StorageDisposableResponse<Stream>> DownloadFile(PathBuilder path)
    {
        BlobClient blob = _client.GetBlobClient(path.ToAzurePath());
        try
        {
            await using Stream stream = await blob.OpenReadAsync();
            MemoryStream outputStream = new();
            await stream.CopyToAsync(outputStream);
            outputStream.Seek(0, SeekOrigin.Begin);
            return StorageDisposableResponse<Stream>.CreateSuccess(outputStream);
        }
        catch (Azure.RequestFailedException ex)
        {
            if (ex.ErrorCode == "BlobNotFound")
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

    public string GetFileLink(BlobClient blobClient, string fileKey)
    {
        if (!blobClient.CanGenerateSasUri) return fileKey;
        new FileExtensionContentTypeProvider().TryGetContentType(fileKey, out var contentType);
        BlobSasBuilder sasBuilder = new()
        {
            BlobContainerName = _azureBlobOptions.ContainerName,
            BlobName = fileKey,
            ExpiresOn = DateTimeOffset.UtcNow.AddDays(_azureBlobOptions.LinkExpiresDays ?? 1),
            ContentType = contentType

        };
        sasBuilder.SetPermissions(BlobContainerSasPermissions.Read);
        return blobClient.GenerateSasUri(sasBuilder).AbsoluteUri;
    }
}