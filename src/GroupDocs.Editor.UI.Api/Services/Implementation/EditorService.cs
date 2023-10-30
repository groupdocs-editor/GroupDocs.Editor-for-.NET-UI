using AutoMapper;
using GroupDocs.Editor.Formats;
using GroupDocs.Editor.HtmlCss.Resources;
using GroupDocs.Editor.HtmlCss.Resources.Images.Vector;
using GroupDocs.Editor.Metadata;
using GroupDocs.Editor.Options;
using GroupDocs.Editor.UI.Api.Models.DocumentConvertor;
using GroupDocs.Editor.UI.Api.Models.Editor;
using GroupDocs.Editor.UI.Api.Models.Storage;
using GroupDocs.Editor.UI.Api.Models.Storage.Requests;
using GroupDocs.Editor.UI.Api.Services.Interfaces;
using GroupDocs.Editor.UI.Api.Services.Options;
using Microsoft.Extensions.Logging;

namespace GroupDocs.Editor.UI.Api.Services.Implementation;

public class EditorService : IEditorService, IDisposable
{
    private readonly ILogger<EditorService> _logger;
    private readonly IMetaFileStorageCache _metaFileStorageCache;
    private readonly IStorage _storage;
    protected readonly IMapper _mapper;
    protected Editor? _editor;
    protected Stream? _originalFIleStream;

    public EditorService(IStorage storage, ILogger<EditorService> logger, IMetaFileStorageCache metaFileStorageCache, IMapper mapper)
    {
        _storage = storage;
        _logger = logger;
        _metaFileStorageCache = metaFileStorageCache;
        _mapper = mapper;
    }

    /// <summary>
    /// Gets the document information.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <param name="loadOptions">The load options.</param>
    /// <returns></returns>
    public IDocumentInfo GetDocumentInfo(Stream stream, ILoadOptions loadOptions)
    {
        CreateEditorIfNotExist(stream, loadOptions);
        return _editor!.GetDocumentInfo(loadOptions.Password);
    }

    public async Task<IDocumentInfo> GetDocumentInfo(StorageMetaFile meta, ILoadOptions loadOptions)
    {
        await CreateEditorIfNotExist(meta, loadOptions);
        return _editor!.GetDocumentInfo(loadOptions.Password);
    }

    /// <summary>
    /// Convert the document to HTML and save to storage.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns></returns>
    public async Task<StorageMetaFile?> SaveDocument(SaveDocumentRequest request)
    {
        try
        {
            var response = (await _storage.UploadFiles(new List<UploadOriginalRequest>
            {
                new()
                {
                    DocumentInfo = _mapper.Map<StorageDocumentInfo>(GetDocumentInfo(request.Stream, request.LoadOptions)),
                    FileContent = new FileContent {FileName = request.FileName, ResourceStream = request.Stream}
                }
            })).FirstOrDefault();
            if (response is not { IsSuccess: true } || response.Response == null)
            {
                _logger.LogError("failed upload file {fileName}", request.FileName);
                return null;
            }

            await ConvertToHtml(response.Response, request.EditOptions, request.LoadOptions);
            return response.Response;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Catch error while save file {fileName}", request.FileName);
            return null;
        }
    }

    /// <summary>
    /// Converts the target document, retrieved from storage, to HTML.
    /// </summary>
    /// <param name="documentCode">The document folder code.</param>
    /// <param name="editOptions">The edit options.</param>
    /// <param name="loadOptions">The load options.</param>
    /// <returns></returns>
    public async Task<StorageMetaFile?> ConvertToHtml(Guid documentCode, IEditOptions editOptions, ILoadOptions loadOptions)
    {
        var meta = await _metaFileStorageCache.DownloadFile(documentCode);
        if (meta == null)
        {
            return null;
        }

        return await ConvertToHtml(meta, editOptions, loadOptions);
    }

    /// <summary>
    /// Converts the target document, retrieved from storage, to HTML.
    /// </summary>
    /// <param name="metaFile">The meta file.</param>
    /// <param name="editOptions">The edit options.</param>
    /// <param name="loadOptions">The load options.</param>
    /// <returns></returns>
    public async Task<StorageMetaFile> ConvertToHtml(StorageMetaFile metaFile, IEditOptions editOptions, ILoadOptions loadOptions)
    {
        try
        {
            var subIndex = editOptions switch
            {
                PresentationEditOptions presentationEditOptions => presentationEditOptions.SlideNumber,
                SpreadsheetEditOptions spreadsheetEdit => spreadsheetEdit.WorksheetIndex,
                _ => 0
            };
            var convertedFile = new StorageSubFile
            {
                SubCode = subIndex,
                SourceDocumentName = metaFile.OriginalFile.FileName,
                DocumentCode = metaFile.DocumentCode,
            };
            HtmlSaveOptions saveOptions = new()
            {
                EmbedStylesheetsIntoMarkup = false,
                AttributeValueDelimiter = HtmlCss.Serialization.QuoteType.SingleQuote,
                HtmlTagCase = HtmlCss.Serialization.TagRenderingCase.LowerCase,
                SavingCallback = new StorageCallback(_storage, convertedFile)
            };

            await CreateEditorIfNotExist(metaFile, loadOptions);
            using EditableDocument doc = _editor!.Edit(editOptions);
            await using Stream document = new MemoryStream();
            await using (StreamWriter writer = new(document))
            {
                doc.Save(writer, saveOptions);
                await writer.FlushAsync();
                var storageFile = (await _storage
                    .SaveFile(new[] { new FileContent { FileName = convertedFile.EditedHtmlName, ResourceStream = writer.BaseStream } },
                        metaFile.DocumentCode, convertedFile.SubCode.ToString())).FirstOrDefault();
                if (storageFile is { IsSuccess: true, Response: not null })
                {
                    convertedFile.SourceDocument = storageFile.Response;
                }
            }

            metaFile.StorageSubFiles.Add(subIndex, convertedFile);
            await _metaFileStorageCache.UpdateFiles(metaFile);
            return metaFile;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to convert file {FileName}", metaFile.OriginalFile.FileName);
            throw;
        }
    }

    /// <summary>
    /// Converts the target document, retrieved from storage, to preview images list.
    /// </summary>
    /// <param name="documentCode">The document folder code.</param>
    /// <param name="loadOptions">The load options.</param>
    /// <returns></returns>
    public async Task<StorageMetaFile?> ConvertPreviews(Guid documentCode, ILoadOptions loadOptions)
    {
        const string previewFolder = "preview";
        var metaFile = await _metaFileStorageCache.DownloadFile(documentCode);
        if (metaFile == null)
        {
            return null;
        }

        IDocumentInfo documentInfo = await GetDocumentInfo(metaFile, loadOptions);
        for (int i = 0; i < documentInfo.PageCount; i++)
        {
            using SvgImage? svgPreview = GetPreview(documentInfo, i);
            if (svgPreview == null)
            {
                continue;
            }
            using FileContent file = new();
            file.FileName = $"{i}.svg";
            file.ResourceStream = svgPreview.ByteContent;
            var preview =
                (await _storage.SaveFile(new List<FileContent> { file }, documentCode, previewFolder)).FirstOrDefault();
            if (preview is { IsSuccess: true, Response: not null })
            {
                metaFile.PreviewImages.Add(i, preview.Response);
            }
        }

        await _metaFileStorageCache.UpdateFiles(metaFile);
        return metaFile;
    }

    /// <summary>
    /// Saves the document to PDF format. If an edited version of the document exists, it will be converted; otherwise, the original document will be converted.
    /// </summary>
    /// <returns></returns>
    public async Task<FileContent?> SaveToPdf(DownloadPdfRequest request)
    {
        var metaFile = await _metaFileStorageCache.DownloadFile(request.DocumentCode);
        if (metaFile == null)
        {
            return null;
        }


        try
        {
            await CreateEditorIfNotExist(metaFile, request.LoadOptions);
            using EditableDocument doc = metaFile.StorageSubFiles.Any(a => a.Value.IsEdited)
                ? await EditableDocumentFromMarkup(metaFile.StorageSubFiles[0])
                : _editor!.Edit();
            MemoryStream outputPdfStream = new();
            _editor!.Save(doc, outputPdfStream, request.SaveOptions);
            outputPdfStream.Seek(0, SeekOrigin.Begin);
            return new FileContent
            {
                FileName = $"{Path.GetFileNameWithoutExtension(metaFile.OriginalFile.FileName)}.pdf",
                ResourceStream = outputPdfStream
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed save to PDF");
            return null;
        }
    }

    /// <summary>
    /// Converts the document to the specified output format.
    /// If an edited version of the document exists, it will be converted; otherwise, the original document will be converted.
    /// </summary>
    /// <param name="request">The save option.</param>
    /// <returns></returns>
    public async Task<FileContent?> ConvertToDocument(DownloadDocumentRequest request)
    {
        var metaFile = await _metaFileStorageCache.DownloadFile(request.DocumentCode);
        if (metaFile == null)
        {
            return null;
        }
        await CreateEditorIfNotExist(metaFile, request.LoadOptions);
        using EditableDocument doc = metaFile.StorageSubFiles.Any(a => a.Value.IsEdited)
            ? await EditableDocumentFromMarkup(metaFile.StorageSubFiles[0])
            : _editor!.Edit();
        MemoryStream outputStream = new();
        _editor!.Save(doc, outputStream, request.SaveOptions);
        outputStream.Seek(0, SeekOrigin.Begin);
        return new FileContent { FileName = $"{Path.GetFileNameWithoutExtension(metaFile.OriginalFile.FileName)}.{request.Format}", ResourceStream = outputStream };
    }

    public IEnumerable<IDocumentFormat> GetSupportedFormats()
    {
        return WordProcessingFormats.All.Cast<IDocumentFormat>().GroupBy(a => a.Extension).Select(a => a.First());
    }
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _editor?.Dispose();
            _originalFIleStream?.Dispose();
        }
    }

    private static SvgImage? GetPreview(IDocumentInfo documentInfo, int pageIndex)
    {
        switch (documentInfo)
        {
            case EbookDocumentInfo:
            case EmailDocumentInfo:
            case FixedLayoutDocumentInfo:
            case MarkdownDocumentInfo:
                return null;
            case PresentationDocumentInfo presentationDocumentInfo:
                {
                    return presentationDocumentInfo.GeneratePreview(pageIndex);
                }
            case WordProcessingDocumentInfo wordProcessingDocumentInfo:
                {
                    return wordProcessingDocumentInfo.GeneratePreview(pageIndex);
                }
            case SpreadsheetDocumentInfo spreadsheetDocumentInfo:
                {
                    return spreadsheetDocumentInfo.GeneratePreview(pageIndex);
                }
            case TextualDocumentInfo:
                break;
            default:
                return null;
        }

        return null;
    }

    private async Task<EditableDocument> EditableDocumentFromMarkup(StorageSubFile oneSubFile)
    {
        var response =
            await _storage.GetFileText(Path.Combine(oneSubFile.DocumentCode.ToString(), oneSubFile.SubCode.ToString(),
                oneSubFile.EditedHtmlName));
        if (response is not { IsSuccess: true } || response.Response == null)
        {
            throw new ArgumentException("Cannot get a html file from storage", nameof(response.Response));
        }

        List<IHtmlResource> resources = new(oneSubFile.AllResources.Count());
        foreach (var oneFile in oneSubFile.AllResources)
        {
            var stream = await _storage.DownloadFile(Path.Combine(oneSubFile.DocumentCode.ToString(), oneSubFile.SubCode.ToString(), oneFile.FileName));

            if (stream is not { IsSuccess: true } || stream.Response == null)
            {
                continue;
            }
            IHtmlResource parsedResource =
                ResourceTypeDetector.TryDetectResource(stream.Response, oneFile.FileName, null);
            if (parsedResource == null)
            {
                continue;
            }

            resources.Add(parsedResource);
        }
        return EditableDocument.FromMarkup(response.Response, resources);
    }

    private void CreateEditorIfNotExist(Stream stream, ILoadOptions loadOptions)
    {
        _editor ??= new Editor(delegate { return stream; }, delegate { return loadOptions; });
    }

    private async Task CreateEditorIfNotExist(StorageMetaFile meta, ILoadOptions loadOptions)
    {
        if (_editor == null)
        {
            var response =
                await _storage.DownloadFile(Path.Combine(meta.DocumentCode.ToString(), meta.OriginalFile.FileName));
            if (response is not { IsSuccess: true } || response.Response == null)
            {
                _logger.LogError("Cannot download file {file}", meta.OriginalFile.FileName);
                return;
            }

            _originalFIleStream = response.Response;
            _editor = new Editor(delegate { return _originalFIleStream; }, delegate { return loadOptions; });
        }
    }
}