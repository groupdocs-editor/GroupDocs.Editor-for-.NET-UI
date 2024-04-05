using GroupDocs.Editor.UI.Api.Models.DocumentConvertor;
using GroupDocs.Editor.UI.Api.Models.Storage;
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
    public LocalStorage(
        ILogger<LocalStorage> logger,
        IOptions<LocalStorageOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Saves the files to the target subfolder, or if a subfolder is not specified, saves them into the root folder.
    /// </summary>
    /// <param name="fileContents">The file contents.</param>
    /// <param name="prefixPath">The document's path.</param>
    /// <returns>list of the instance of <see cref="StorageFile"/></returns>
    public async Task<IEnumerable<StorageResponse<StorageFile>>> SaveFile(IEnumerable<FileContent> fileContents, PathBuilder prefixPath)
    {
        var result = new List<StorageResponse<StorageFile>>();
        foreach (var fileContent in fileContents)
        {
            var folder = Path.Combine(_options.RootFolder, prefixPath.ToPath());
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            await using var fileStream = File.Open(Path.Combine(folder, fileContent.FileName), FileMode.OpenOrCreate);
            fileContent.ResourceStream.Seek(0, SeekOrigin.Begin);
            await fileContent.ResourceStream.CopyToAsync(fileStream);
            UriBuilder uriBuilder = new(_options.BaseUrl);
            uriBuilder.Path += prefixPath.AppendKey(HttpUtility.UrlEncode(fileContent.FileName)).ToUriPath();
                //string.IsNullOrWhiteSpace(subIndex) ? $"{documentCode}/{HttpUtility.UrlEncode(fileContent.FileName)}" : $"{documentCode}/{subIndex}/{HttpUtility.UrlEncode(fileContent.FileName)}";
            result.Add(StorageResponse<StorageFile>.CreateSuccess(new StorageFile
            {
                DocumentCode = prefixPath.DocumentCode,
                FileLink = uriBuilder.Uri.ToString(),
                FileName = fileContent.FileName,
                ResourceType = fileContent.ResourceType
            }));
        }

        return result;
    }

    public Task<StorageResponse> RemoveFolder(PathBuilder path)
    {
        var folder = Path.Combine(_options.RootFolder, path.ToPath());
        if (!Directory.Exists(folder)) return Task.FromResult(StorageResponse.CreateNotExist());
        Directory.Delete(folder, true);
        return Task.FromResult(StorageResponse.CreateSuccess());

    }

    public Task<StorageResponse> RemoveFile(PathBuilder path)
    {
        var file = Path.Combine(_options.RootFolder, path.ToPath());
        if (!File.Exists(file)) return Task.FromResult(StorageResponse.CreateNotExist());
        File.Delete(file);
        return Task.FromResult(StorageResponse.CreateSuccess());

    }

    public async Task<StorageDisposableResponse<Stream>> DownloadFile(PathBuilder path)
    {
        var file = Path.Combine(_options.RootFolder, path.ToPath());
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

    public async Task<StorageResponse<string>> GetFileText(PathBuilder path)
    {
        var file = Path.Combine(_options.RootFolder, path.ToPath());
        if (!File.Exists(file))
        {
            return StorageResponse<string>.CreateFailed(string.Empty);
        }
        string fileText = await File.ReadAllTextAsync(file);
        return StorageResponse<string>.CreateSuccess(fileText);
    }
}