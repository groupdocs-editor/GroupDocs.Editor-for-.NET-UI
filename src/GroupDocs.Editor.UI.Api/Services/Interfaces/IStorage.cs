using GroupDocs.Editor.UI.Api.Controllers.RequestModels;
using GroupDocs.Editor.UI.Api.Models.DocumentConvertor;
using GroupDocs.Editor.UI.Api.Models.Storage;
using GroupDocs.Editor.UI.Api.Models.Storage.Requests;
using GroupDocs.Editor.UI.Api.Models.Storage.Responses;

namespace GroupDocs.Editor.UI.Api.Services.Interfaces;

public interface IStorage
{
    public Task<IEnumerable<StorageResponse<StorageMetaFile>>> UploadFiles(IEnumerable<UploadOriginalRequest> files);
    public Task<IEnumerable<StorageResponse<StorageFile>>> SaveFile(IEnumerable<FileContent> fileContents, Guid documentCode, string subIndex = "");
    public Task<StorageResponse> RemoveFolder(string folderSubPath);
    public Task<StorageResponse> RemoveFile(string fileSubPath);
    public Task<StorageDisposableResponse<Stream>> DownloadFile(string fileSubPath);
    Task<StorageResponse<string>> GetFileText(string fileSubPath);
    Task<StorageResponse<StorageSubFile>> UpdateHtmlContent(StorageSubFile currentContent, string htmlContents);
    Task<StorageUpdateResourceResponse<StorageSubFile, StorageFile>> UpdateResource(StorageSubFile currentContent, UploadResourceRequest resource);
}