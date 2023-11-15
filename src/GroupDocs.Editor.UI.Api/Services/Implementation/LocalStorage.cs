using GroupDocs.Editor.UI.Api.Controllers.RequestModels;
using GroupDocs.Editor.UI.Api.Models.DocumentConvertor;
using GroupDocs.Editor.UI.Api.Models.Storage;
using GroupDocs.Editor.UI.Api.Models.Storage.Requests;
using GroupDocs.Editor.UI.Api.Models.Storage.Responses;
using GroupDocs.Editor.UI.Api.Services.Interfaces;
using GroupDocs.Editor.UI.Api.Services.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Web;

namespace GroupDocs.Editor.UI.Api.Services.Implementation;

public class LocalStorage : IStorage
{
    private readonly ILogger<LocalStorage> _logger;
    private readonly LocalStorageOptions _options;
    private readonly IIdGeneratorService _idGenerator;
    public LocalStorage(
        ILogger<LocalStorage> logger,
        IOptions<LocalStorageOptions> options,
        IIdGeneratorService idGenerator)
    {
        _logger = logger;
        _idGenerator = idGenerator;
        _options = options.Value;
    }

    /// <summary>
    /// Uploads the original files and initializes a StorageMetaFile entity for tracking metadata.
    /// </summary>
    /// <param name="files">The original files to be uploaded.</param>
    /// <returns>An instance of the <see cref="StorageMetaFile"/> entity for metadata initialization.</returns>
    /// <exception cref="System.IO.FileLoadException"></exception>
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

        var result = new List<StorageResponse<StorageMetaFile>>();
        foreach (var file in uploadOriginalRequests)
        {
            try
            {
                var documentCode = _idGenerator.GenerateDocumentCode();
                var storageFile = (await this.SaveFile(new[] { file.FileContent }, documentCode)).FirstOrDefault();
                if (storageFile is not { IsSuccess: true })
                {
                    result.Add(StorageResponse<StorageMetaFile>.CreateFailed(new StorageMetaFile()));
                    continue;
                }
                StorageMetaFile metaFile = new()
                {
                    DocumentInfo = file.DocumentInfo,
                    DocumentCode = documentCode,
                    OriginalFile = storageFile.Response ?? throw new FileLoadException()
                };
                result.Add(StorageResponse<StorageMetaFile>.CreateSuccess(metaFile));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed save file {FileName}", file.FileContent.FileName);
                result.Add(StorageResponse<StorageMetaFile>.CreateFailed(new StorageMetaFile
                {
                    DocumentInfo = file.DocumentInfo,
                    DocumentCode = Guid.Empty,
                    OriginalFile = new StorageFile
                    {
                        DocumentCode = _idGenerator.GenerateEmptyDocumentCode(),
                        FileName = file.FileContent.FileName
                    }
                }));
            }
        }

        return result;
    }

    /// <summary>
    /// Saves the files to the target subfolder, or if a subfolder is not specified, saves them into the root folder.
    /// </summary>
    /// <param name="fileContents">The file contents.</param>
    /// <param name="documentCode">The document code.</param>
    /// <param name="subIndex">The optional target subfolder index-name where the files should be saved.</param>
    /// <returns>list of the instance of <see cref="StorageFile"/></returns>
    public async Task<IEnumerable<StorageResponse<StorageFile>>> SaveFile(IEnumerable<FileContent> fileContents, Guid documentCode, string subIndex = "")
    {
        var result = new List<StorageResponse<StorageFile>>();
        foreach (var fileContent in fileContents)
        {
            var folder = Path.Combine(_options.RootFolder, documentCode.ToString(), subIndex);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            await using var fileStream = File.Open(Path.Combine(folder, fileContent.FileName), FileMode.OpenOrCreate);
            fileContent.ResourceStream.Seek(0, SeekOrigin.Begin);
            await fileContent.ResourceStream.CopyToAsync(fileStream);
            UriBuilder uriBuilder = new(_options.BaseUrl);
            uriBuilder.Path += string.IsNullOrWhiteSpace(subIndex) ? $"{documentCode}/{HttpUtility.UrlEncode(fileContent.FileName)}" : $"{documentCode}/{subIndex}/{HttpUtility.UrlEncode(fileContent.FileName)}";
            result.Add(StorageResponse<StorageFile>.CreateSuccess(new StorageFile
            {
                DocumentCode = documentCode,
                FileLink = uriBuilder.Uri.ToString(),
                FileName = fileContent.FileName
            }));
        }

        return result;
    }

    public Task<StorageResponse> RemoveFolder(string folderSubPath)
    {
        var folder = Path.Combine(_options.RootFolder, folderSubPath);
        if (!Directory.Exists(folder)) return Task.FromResult(StorageResponse.CreateNotExist());
        Directory.Delete(folder, true);
        return Task.FromResult(StorageResponse.CreateSuccess());

    }

    public Task<StorageResponse> RemoveFile(string fileSubPath)
    {
        var file = Path.Combine(_options.RootFolder, fileSubPath);
        if (!File.Exists(file)) return Task.FromResult(StorageResponse.CreateNotExist());
        File.Delete(file);
        return Task.FromResult(StorageResponse.CreateSuccess());

    }

    public async Task<StorageDisposableResponse<Stream>> DownloadFile(string fileSubPath)
    {
        var file = Path.Combine(_options.RootFolder, fileSubPath);
        if (!File.Exists(file))
        {
            return StorageDisposableResponse<Stream>.CreateNotExist(Stream.Null);
        }

        await using var fileStream = File.Open(file, FileMode.Open);
        MemoryStream memoryStream = new();
        await fileStream.CopyToAsync(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);
        return StorageDisposableResponse<Stream>.CreateSuccess(memoryStream);
    }

    public async Task<StorageResponse<string>> GetFileText(string fileSubPath)
    {
        var file = Path.Combine(_options.RootFolder, fileSubPath);
        if (!File.Exists(file))
        {
            return StorageResponse<string>.CreateFailed(string.Empty);
        }
        string fileText = await File.ReadAllTextAsync(file);
        return StorageResponse<string>.CreateSuccess(fileText);
    }

    public async Task<StorageResponse<StorageSubFile>> UpdateHtmlContent(StorageSubFile currentContent,
        string htmlContents)
    {
        RemoveFile(Path.Combine(currentContent.DocumentCode.ToString(), currentContent.SubCode.ToString(),
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
            RemoveFile(Path.Combine(currentContent.DocumentCode.ToString(), currentContent.SubCode.ToString(), resource.OldResorceName));
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
}