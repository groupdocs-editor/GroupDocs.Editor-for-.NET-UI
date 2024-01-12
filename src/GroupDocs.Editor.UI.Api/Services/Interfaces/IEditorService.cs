using GroupDocs.Editor.Formats;
using GroupDocs.Editor.Metadata;
using GroupDocs.Editor.Options;
using GroupDocs.Editor.UI.Api.Controllers.RequestModels;
using GroupDocs.Editor.UI.Api.Models.DocumentConvertor;
using GroupDocs.Editor.UI.Api.Models.Editor;
using GroupDocs.Editor.UI.Api.Models.Storage;
using GroupDocs.Editor.UI.Api.Models.Storage.Responses;

namespace GroupDocs.Editor.UI.Api.Services.Interfaces;

public interface IEditorService<TLoadOptions, TEditOptions> : IDisposable
    where TLoadOptions : ILoadOptions
    where TEditOptions : IEditOptions
{
    public Task<StorageMetaFile<TLoadOptions, TEditOptions>?> CreateDocument(CreateDocumentRequest request);
    public IDocumentInfo GetDocumentInfo(Stream stream, TLoadOptions loadOptions);

    public Task<StorageMetaFile<TLoadOptions, TEditOptions>?> UploadDocument(UploadDocumentRequest request);

    public Task<string?> ConvertToHtml(StorageMetaFile<TLoadOptions, TEditOptions> metaFile, TEditOptions? editOptions,
        ILoadOptions? loadOptions);

    public Task<StorageMetaFile<TLoadOptions, TEditOptions>?> ConvertPreviews(Guid documentCode);

    public Task<FileContent?> SaveToPdf(DownloadPdfRequest request);

    public Task<FileContent?> ConvertToDocument(DownloadDocumentRequest request);

    public IEnumerable<TFormat> GetSupportedFormats<TFormat>() where TFormat : IDocumentFormat;

    Task<StorageResponse<StorageSubFile<TEditOptions>>> UpdateHtmlContent(StorageSubFile<TEditOptions> currentContent, string htmlContents);
    Task<StorageUpdateResourceResponse<StorageSubFile<TEditOptions>, StorageFile>> UpdateResource(StorageSubFile<TEditOptions> currentContent, UploadResourceRequest resource);
}