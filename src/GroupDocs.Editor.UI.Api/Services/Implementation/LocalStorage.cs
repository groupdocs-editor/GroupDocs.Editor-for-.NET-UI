using GroupDocs.Editor.UI.Api.Models.DocumentConvertor;
using GroupDocs.Editor.UI.Api.Models.Storage;
using GroupDocs.Editor.UI.Api.Models.Storage.Responses;
using GroupDocs.Editor.UI.Api.Services.Interfaces;
using GroupDocs.Editor.UI.Api.Services.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Web;

namespace GroupDocs.Editor.UI.Api.Services.Implementation;

public class LocalStorage : IStorage
{
    private readonly ILogger<LocalStorage> _logger;
    private readonly LocalStorageOptions _options;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LocalStorage(
        ILogger<LocalStorage> logger,
        IOptions<LocalStorageOptions> options,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
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
        List<StorageResponse<StorageFile>> result = new List<StorageResponse<StorageFile>>();
        foreach (FileContent fileContent in fileContents)
        {
            string folder = Path.Combine(_options.RootFolder, prefixPath.ToPath());
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            await using FileStream fileStream = File.Open(Path.Combine(folder, fileContent.FileName), FileMode.OpenOrCreate);
            fileContent.ResourceStream.Seek(0, SeekOrigin.Begin);
            await fileContent.ResourceStream.CopyToAsync(fileStream);
            UriBuilder uriBuilder = GetFullUri();
            uriBuilder.Path += prefixPath.AppendKey(HttpUtility.UrlEncode(fileContent.FileName)).ToUriPath();
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
        string folder = Path.Combine(_options.RootFolder, path.ToPath());
        if (!Directory.Exists(folder)) return Task.FromResult(StorageResponse.CreateNotExist());
        Directory.Delete(folder, true);
        return Task.FromResult(StorageResponse.CreateSuccess());

    }

    public Task<StorageResponse> RemoveFile(PathBuilder path)
    {
        string file = Path.Combine(_options.RootFolder, path.ToPath());
        if (!File.Exists(file)) return Task.FromResult(StorageResponse.CreateNotExist());
        File.Delete(file);
        return Task.FromResult(StorageResponse.CreateSuccess());

    }

    public async Task<StorageDisposableResponse<Stream>> DownloadFile(PathBuilder path)
    {
        string file = Path.Combine(_options.RootFolder, path.ToPath());
        if (!File.Exists(file))
        {
            return StorageDisposableResponse<Stream>.CreateNotExist(Stream.Null);
        }

        await using FileStream fileStream = File.Open(file, FileMode.Open);
        MemoryStream memoryStream = new();
        await fileStream.CopyToAsync(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);
        return StorageDisposableResponse<Stream>.CreateSuccess(memoryStream);
    }

    public async Task<StorageResponse<string>> GetFileText(PathBuilder path)
    {
        string file = Path.Combine(_options.RootFolder, path.ToPath());
        if (!File.Exists(file))
        {
            return StorageResponse<string>.CreateFailed(string.Empty);
        }
        string fileText = await File.ReadAllTextAsync(file);
        return StorageResponse<string>.CreateSuccess(fileText);
    }

    private UriBuilder GetFullUri()
    {
        HttpRequest? request = _httpContextAccessor.HttpContext?.Request;

        if (request == null)
            throw new InvalidOperationException("No active HTTP request.");

        UriBuilder uriBuilder = new UriBuilder
        {
            Scheme = request.Scheme,
            Host = request.Host.Host,
            Port = request.Host.Port ?? (request.Scheme == "https" ? 443 : 80),
            Path = _options.BaseUrl
        };

        return uriBuilder;
    }
}