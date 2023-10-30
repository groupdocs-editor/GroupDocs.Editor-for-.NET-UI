using GroupDocs.Editor.UI.Api.Models.Storage;

namespace GroupDocs.Editor.UI.Api.Services.Interfaces;

public interface IMetaFileStorageCache
{
    public Task<StorageMetaFile?> UpdateFiles(StorageMetaFile? files);

    public Task<StorageMetaFile?> DownloadFile(Guid documentFolderCode);
}