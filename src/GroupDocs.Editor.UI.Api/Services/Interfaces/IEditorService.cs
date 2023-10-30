using GroupDocs.Editor.Formats;
using GroupDocs.Editor.Metadata;
using GroupDocs.Editor.Options;
using GroupDocs.Editor.UI.Api.Models.DocumentConvertor;
using GroupDocs.Editor.UI.Api.Models.Editor;
using GroupDocs.Editor.UI.Api.Models.Storage;

namespace GroupDocs.Editor.UI.Api.Services.Interfaces;

public interface IEditorService
{
    public IDocumentInfo GetDocumentInfo(Stream stream, ILoadOptions loadOptions);

    Task<IDocumentInfo> GetDocumentInfo(StorageMetaFile meta, ILoadOptions loadOptions);

    public Task<StorageMetaFile?> SaveDocument(SaveDocumentRequest request);

    public Task<StorageMetaFile?> ConvertToHtml(Guid documentCode, IEditOptions editOptions, ILoadOptions loadOptions);

    public Task<StorageMetaFile> ConvertToHtml(StorageMetaFile metaFile, IEditOptions editOptions, ILoadOptions loadOptions);

    public Task<StorageMetaFile?> ConvertPreviews(Guid documentCode, ILoadOptions loadOptions);

    public Task<FileContent?> SaveToPdf(DownloadPdfRequest request);

    public Task<FileContent?> ConvertToDocument(DownloadDocumentRequest request);

    public IEnumerable<IDocumentFormat> GetSupportedFormats();
}