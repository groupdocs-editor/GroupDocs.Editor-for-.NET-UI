using GroupDocs.Editor.UI.Api.Models.DocumentConvertor;
using GroupDocs.Editor.UI.Api.Models.Storage;
using GroupDocs.Editor.UI.Api.Models.Storage.Responses;

namespace GroupDocs.Editor.UI.Api.Services.Interfaces;

public interface IStorage
{
    public Task<IEnumerable<StorageResponse<StorageFile>>> SaveFile(IEnumerable<FileContent> fileContents, Guid documentCode, string subIndex = "");
    public Task<StorageResponse> RemoveFolder(string folderSubPath);
    public Task<StorageResponse> RemoveFile(string fileSubPath);
    public Task<StorageDisposableResponse<Stream>> DownloadFile(string fileSubPath);
    Task<StorageResponse<string>> GetFileText(string fileSubPath);
}