using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using GroupDocs.Editor.UI.Api.Controllers.RequestModels;
using GroupDocs.Editor.UI.Api.Models.DocumentConvertor;
using GroupDocs.Editor.UI.Api.Models.Storage;
using GroupDocs.Editor.UI.Api.Models.Storage.Requests;
using GroupDocs.Editor.UI.Api.Models.Storage.Responses;
using GroupDocs.Editor.UI.Api.Services.Interfaces;
using GroupDocs.Editor.UI.Api.Services.Options;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GroupDocs.Editor.UI.Api.Services.Implementation
{
    public class AzureBlobStorage : IStorage
    {
        private readonly BlobContainerClient _client;
        private readonly IIdGeneratorService _idGenerator;
        private readonly ILogger<AzureBlobStorage> _logger;
        private readonly AzureBlobOptions _azureBlobOptions;
        public AzureBlobStorage(
            IOptions<AzureBlobOptions> options,
            IIdGeneratorService idGenerator,
            ILogger<AzureBlobStorage> logger)
        {
            _azureBlobOptions = options.Value;
            string connStr = $"DefaultEndpointsProtocol=https;AccountName={_azureBlobOptions.AccountName};AccountKey={_azureBlobOptions.AccountKey}";
            BlobContainerClient client = new(connStr, _azureBlobOptions.ContainerName);
            this._client = client;
            this._idGenerator = idGenerator;
            this._logger = logger;
        }

        public async Task<IEnumerable<StorageResponse<StorageFile>>> SaveFile(IEnumerable<FileContent> fileContents, Guid documentCode, string subIndex = "")
        {
            List<StorageResponse<StorageFile>> totalResult = new();

            foreach (FileContent one in fileContents)
            {
                var azureFileName = BuildUrlName(new[] { documentCode.ToString(), subIndex, one.FileName });
                BlobClient blob = this._client.GetBlobClient(azureFileName);
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
                            DocumentCode = documentCode,
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
                        DocumentCode = documentCode,
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

        public async Task<IEnumerable<StorageResponse<StorageMetaFile>>> UploadFiles(IEnumerable<UploadOriginalRequest> files)
        {
            var uploadOriginalRequests = files.ToList();
            if (!uploadOriginalRequests.Any())
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
            List<StorageResponse<StorageMetaFile>> result = new();
            foreach (UploadOriginalRequest one in uploadOriginalRequests)
            {
                try
                {
                    Guid documentCode = _idGenerator.GenerateDocumentCode();
                    var storageFiles = await this.SaveFile(new[] { one.FileContent }, documentCode);
                    StorageResponse<StorageFile>? storageFileSingle = storageFiles.FirstOrDefault();
                    if (storageFileSingle is not { IsSuccess: true })
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
                    this._logger?.LogError(e, "Failed save file {FileName} to Azure Blob Storage", one.FileContent.FileName);

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

        public async Task<StorageResponse> RemoveFolder(string folderSubPath)
        {
            if (string.IsNullOrWhiteSpace(folderSubPath)) { return StorageResponse.CreateFailed(); }

            Azure.Pageable<BlobItem> blobItems = this._client.GetBlobs(prefix: folderSubPath);
            int deletedCounter = 0;
            foreach (BlobItem blobItem in blobItems)
            {
                BlobClient blobClient = this._client.GetBlobClient(blobItem.Name);
                await blobClient.DeleteIfExistsAsync();
                deletedCounter++;
            }
            return deletedCounter == 0 ? StorageResponse.CreateNotExist() : StorageResponse.CreateSuccess();
        }

        public async Task<StorageResponse> RemoveFile(string fileSubPath)
        {
            BlobClient blob = this._client.GetBlobClient(fileSubPath);
            Azure.Response<bool> result = await blob.DeleteIfExistsAsync();
            return result.Value ? StorageResponse.CreateSuccess() : StorageResponse.CreateNotExist();
        }

        public async Task<StorageDisposableResponse<Stream>> DownloadFile(string fileSubPath)
        {
            BlobClient blob = this._client.GetBlobClient(fileSubPath);
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

        public async Task<StorageResponse<string>> GetFileText(string fileSubPath)
        {
            StorageDisposableResponse<Stream> file = await this.DownloadFile(fileSubPath);
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
            await this.RemoveFile(Path.Combine(currentContent.DocumentCode.ToString(),
                currentContent.SubCode.ToString(),
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
                await this.RemoveFile(Path.Combine(currentContent.DocumentCode.ToString(), currentContent.SubCode.ToString(), resource.OldResorceName));
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

        public string BuildUrlName(string[] names)
        {
            var nonEmptyNames = names.Where(name => !string.IsNullOrWhiteSpace(name));
            return string.Join('/', nonEmptyNames);
        }
    }
}
