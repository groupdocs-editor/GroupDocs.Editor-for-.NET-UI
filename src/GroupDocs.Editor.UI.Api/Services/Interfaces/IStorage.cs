using GroupDocs.Editor.UI.Api.Models.DocumentConvertor;
using GroupDocs.Editor.UI.Api.Models.Storage;
using GroupDocs.Editor.UI.Api.Models.Storage.Responses;

namespace GroupDocs.Editor.UI.Api.Services.Interfaces;

public interface IStorage
{
    public Task<IEnumerable<StorageResponse<StorageFile>>> SaveFile(IEnumerable<FileContent> fileContents, PathBuilder prefixPath);
    public Task<StorageResponse> RemoveFolder(PathBuilder path);
    public Task<StorageResponse> RemoveFile(PathBuilder path);
    public Task<StorageDisposableResponse<Stream>> DownloadFile(PathBuilder path);
    Task<StorageResponse<string>> GetFileText(PathBuilder path);
}