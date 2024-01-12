using GroupDocs.Editor.Options;
using GroupDocs.Editor.UI.Api.Models.Storage;

namespace GroupDocs.Editor.UI.Api.Services.Interfaces;

public interface IMetaFileStorageCache<TLoadOptions, TEditOptions>
    where TLoadOptions : ILoadOptions
    where TEditOptions : IEditOptions
{
    public Task<StorageMetaFile<TLoadOptions, TEditOptions>?> UpdateFiles(StorageMetaFile<TLoadOptions, TEditOptions>? files);

    public Task<StorageMetaFile<TLoadOptions, TEditOptions>?> DownloadFile(Guid documentFolderCode);
}