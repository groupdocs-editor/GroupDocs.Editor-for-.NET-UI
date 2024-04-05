using AutoMapper;
using GroupDocs.Editor.Formats;
using GroupDocs.Editor.HtmlCss.Resources;
using GroupDocs.Editor.HtmlCss.Resources.Images.Vector;
using GroupDocs.Editor.Metadata;
using GroupDocs.Editor.Options;
using GroupDocs.Editor.UI.Api.Controllers.RequestModels;
using GroupDocs.Editor.UI.Api.Models.DocumentConvertor;
using GroupDocs.Editor.UI.Api.Models.Editor;
using GroupDocs.Editor.UI.Api.Models.Storage;
using GroupDocs.Editor.UI.Api.Models.Storage.Responses;
using GroupDocs.Editor.UI.Api.Services.Interfaces;
using GroupDocs.Editor.UI.Api.Services.Options;
using Microsoft.Extensions.Logging;
using System.Drawing.Imaging;

namespace GroupDocs.Editor.UI.Api.Services.Implementation;

public class EditorService<TLoadOptions, TEditOptions> : IEditorService<TLoadOptions, TEditOptions>
    where TLoadOptions : ILoadOptions
    where TEditOptions : IEditOptions
{
    private readonly ILogger<EditorService<TLoadOptions, TEditOptions>> _logger;
    private readonly IMetaFileStorageCache<TLoadOptions, TEditOptions> _metaFileStorageCache;
    private readonly IStorage _storage;
    protected readonly IMapper _mapper;
    protected Editor? _editor;
    protected Stream? _originalFIleStream;
    private readonly IIdGeneratorService _idGenerator;

    public EditorService(
        IStorage storage,
        ILogger<EditorService<TLoadOptions, TEditOptions>> logger,
        IMetaFileStorageCache<TLoadOptions, TEditOptions> metaFileStorageCache,
        IMapper mapper,
        IIdGeneratorService idGenerator)
    {
        _storage = storage;
        _logger = logger;
        _metaFileStorageCache = metaFileStorageCache;
        _mapper = mapper;
        _idGenerator = idGenerator;
    }

    /// <summary>
    /// Creates the new document by fomat.
    /// </summary>
    /// <param name="request">The request with file name and expected format.</param>
    /// <returns></returns>
    public async Task<StorageMetaFile<TLoadOptions, TEditOptions>?> CreateDocument(CreateDocumentRequest request)
    {
        try
        {
            StorageMetaFile<TLoadOptions, TEditOptions>? metaFile = null;
            using Editor editor = new(DocumentAction, request.Format);
            if (metaFile == null) { return null; }
            var info = editor.GetDocumentInfo(null);
            metaFile.DocumentInfo = _mapper.Map<StorageDocumentInfo>(info);
            await _metaFileStorageCache.UpdateFiles(metaFile);
            return metaFile;

            void DocumentAction(Stream stream)
            {
                var documentCode = _idGenerator.GenerateDocumentCode();
                var originalFile = _storage.SaveFile(new List<FileContent>
                {
                    new()
                    {
                        FileName = request.FileName,
                        ResourceStream = stream,
                        ResourceType = ResourceType.OriginalDocument
                    }
                }, PathBuilder.New(documentCode));
                metaFile = new StorageMetaFile<TLoadOptions, TEditOptions>
                {
                    DocumentCode = documentCode,
                    OriginalFile = originalFile.Result.FirstOrDefault()?.Response ?? throw new FileLoadException()
                };
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Catch error while save file {fileName}", request.FileName);
            throw;
        }
    }

    /// <summary>
    /// Convert the document to HTML and save to storage.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns></returns>
    public async Task<StorageMetaFile<TLoadOptions, TEditOptions>> UploadDocument(UploadDocumentRequest request)
    {
        try
        {
            await using Stream documentStream = request.Stream;
            using Editor editor = new(delegate { return documentStream; }, delegate { return request.LoadOptions; });
            var documentCode = _idGenerator.GenerateDocumentCode();
            var originalFile = await _storage.SaveFile(new List<FileContent>
            {
                new()
                {
                    FileName = request.FileName,
                    ResourceStream = request.Stream,
                    ResourceType = ResourceType.OriginalDocument
                }
            }, PathBuilder.New(documentCode));
            documentStream.Seek(0, SeekOrigin.Begin);
            await documentStream.FlushAsync();
            StorageMetaFile<TLoadOptions, TEditOptions> metaFile = new()
            {
                DocumentInfo =
                    _mapper.Map<StorageDocumentInfo>(editor.GetDocumentInfo(request.LoadOptions?.Password ?? null)),
                DocumentCode = documentCode,
                OriginalFile = originalFile.FirstOrDefault()?.Response ?? throw new FileLoadException(),
                OriginalLoadOptions = (TLoadOptions?)request.LoadOptions
            };
            await _metaFileStorageCache.UpdateFiles(metaFile);
            return metaFile;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Catch error while save file {fileName}", request.FileName);
            throw;
        }
    }

    /// <summary>
    /// Gets the document information.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <param name="loadOptions">The load options.</param>
    /// <returns></returns>
    public IDocumentInfo GetDocumentInfo(Stream stream, TLoadOptions loadOptions)
    {
        using Editor editor = new(delegate { return stream; }, delegate { return loadOptions; });
        return editor.GetDocumentInfo(loadOptions.Password ?? null);
    }

    /// <summary>
    /// Converts the target document, retrieved from storage, to HTML.
    /// </summary>
    /// <param name="metaFile">code of the document.</param>
    /// <param name="editOptions">The edit options.</param>
    /// <param name="loadOptions">The load options.</param>
    /// <exception cref="ArgumentNullException">in case when document's metafile is not exist or cannot download original file.</exception>
    /// <returns></returns>
    public async Task<Stream?> ConvertToHtml(StorageMetaFile<TLoadOptions, TEditOptions> metaFile, TEditOptions? editOptions,
        ILoadOptions? loadOptions)
    {
        try
        {

            string subIndex = editOptions switch
            {
                PresentationEditOptions presentationEditOptions => presentationEditOptions.SlideNumber.ToString(),
                SpreadsheetEditOptions spreadsheetEdit => spreadsheetEdit.WorksheetIndex.ToString(),
                _ => "0"
            };
            var convertedFile = new StorageSubFile<TEditOptions>(metaFile.OriginalFile.FileName, subIndex)
            { DocumentCode = metaFile.DocumentCode, EditOptions = editOptions };
            await _storage.RemoveFolder(PathBuilder.New(convertedFile.DocumentCode, new []{ convertedFile.SubCode }));
            HtmlSaveOptions saveOptions = new()
            {
                EmbedStylesheetsIntoMarkup = false,
                AttributeValueDelimiter = HtmlCss.Serialization.QuoteType.SingleQuote,
                HtmlTagCase = HtmlCss.Serialization.TagRenderingCase.LowerCase,
                SavingCallback = new StorageCallback<TEditOptions>(_storage, convertedFile)
            };
            using var originalDocument =
                await _storage.DownloadFile(PathBuilder.New(convertedFile.DocumentCode, new[] { metaFile.OriginalFile.FileName }));
            if (originalDocument is not { IsSuccess: true } || originalDocument.Response == null)
            {
                _logger.LogError("Cannot download file {file}", metaFile.OriginalFile.FileName);
                throw new ArgumentNullException($"Cannot download file {metaFile.OriginalFile.FileName}");
            }
            using Editor editor = new(delegate { return originalDocument.Response; }, delegate { return loadOptions; });
            using EditableDocument doc = editor.Edit(editOptions);
            await using Stream document = new MemoryStream();
            await using StreamWriter writer = new(document);
            doc.Save(writer, saveOptions);
            await writer.FlushAsync();
            var storageFile = (await _storage
                .SaveFile(
                    new[]
                    {
                        new FileContent
                        {
                            FileName = convertedFile.EditedHtmlName,
                            ResourceStream = writer.BaseStream,
                            ResourceType = ResourceType.HtmlContent,
                        }
                    },
                    PathBuilder.New(metaFile.DocumentCode, new[] { convertedFile.SubCode }))).FirstOrDefault();
            if (storageFile is { IsSuccess: true, Response: not null })
            {
                convertedFile.Resources.Add(storageFile.Response.FileName, storageFile.Response);
            }
            metaFile.StorageSubFiles.Add(subIndex, convertedFile);
            await _metaFileStorageCache.UpdateFiles(metaFile);
            await document.FlushAsync();
            document.Seek(0, SeekOrigin.Begin);
            Stream result = new MemoryStream();
            await document.CopyToAsync(result);
            await result.FlushAsync();
            result.Seek(0, SeekOrigin.Begin);
            return result;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to convert file with code: {documentCode}", metaFile.DocumentCode);
            throw;
        }
    }

    /// <summary>
    /// Converts the target document, retrieved from storage, to preview images list.
    /// </summary>
    /// <param name="documentCode">The document folder code.</param>
    /// <returns></returns>
    public async Task<StorageMetaFile<TLoadOptions, TEditOptions>?> ConvertPreviews(Guid documentCode)
    {
        const string previewFolder = "preview";
        var metaFile = await _metaFileStorageCache.DownloadFile(documentCode);
        if (metaFile == null)
        {
            return null;
        }
        using var originalDocument =
            await _storage.DownloadFile(PathBuilder.New(metaFile.DocumentCode, new[] { metaFile.OriginalFile.FileName }));
        if (originalDocument is not { IsSuccess: true } || originalDocument.Response == null)
        {
            _logger.LogError("Cannot download file {file}", metaFile.OriginalFile.FileName);
            throw new ArgumentNullException($"Cannot download file {metaFile.OriginalFile.FileName}");
        }

        using Editor editor = new(delegate { return originalDocument.Response; }, delegate { return metaFile.OriginalLoadOptions; });
        IDocumentInfo documentInfo = editor.GetDocumentInfo(metaFile.OriginalLoadOptions?.Password ?? null);
        for (int i = 0; i < documentInfo.PageCount; i++)
        {
            using SvgImage? svgPreview = GetPreview(documentInfo, i);
            if (svgPreview == null)
            {
                continue;
            }
            using FileContent file = new()
            {
                FileName = $"{i}.svg",
                ResourceStream = svgPreview.ByteContent,
                ResourceType = ResourceType.Preview
            };
            var preview =
                (await _storage.SaveFile(new List<FileContent> { file }, PathBuilder.New(documentCode, new[] { previewFolder }))).FirstOrDefault();
            if (preview is { IsSuccess: true, Response: not null })
            {
                metaFile.PreviewImages.Add(i.ToString(), preview.Response);
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
            using var originalDocument =
                await _storage.DownloadFile(PathBuilder.New(metaFile.DocumentCode, new[] { metaFile.OriginalFile.FileName }));
            if (originalDocument is not { IsSuccess: true } || originalDocument.Response == null)
            {
                _logger.LogError("Cannot download file {file}", metaFile.OriginalFile.FileName);
                throw new ArgumentNullException($"Cannot download file {metaFile.OriginalFile.FileName}");
            }
            using Editor editor = new(delegate { return originalDocument.Response; }, delegate { return metaFile.OriginalLoadOptions; });
            using EditableDocument doc = metaFile.StorageSubFiles.Any(a => a.Value.IsEdited)
                ? await EditableDocumentFromMarkup(metaFile.StorageSubFiles["0"])
                : editor.Edit();
            MemoryStream outputPdfStream = new();
            editor.Save(doc, outputPdfStream, request.SaveOptions);
            outputPdfStream.Seek(0, SeekOrigin.Begin);
            return new FileContent
            {
                FileName = $"{Path.GetFileNameWithoutExtension(metaFile.OriginalFile.FileName)}.pdf",
                ResourceStream = outputPdfStream,
                ResourceType = ResourceType.ConvertedDocument
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
        using var originalDocument =
            await _storage.DownloadFile(PathBuilder.New(metaFile.DocumentCode, new[] { metaFile.OriginalFile.FileName }));
        if (originalDocument is not { IsSuccess: true } || originalDocument.Response == null)
        {
            _logger.LogError("Cannot download file {file}", metaFile.OriginalFile.FileName);
            throw new ArgumentNullException($"Cannot download file {metaFile.OriginalFile.FileName}");
        }
        using Editor editor = new(delegate { return originalDocument.Response; }, delegate { return metaFile.OriginalLoadOptions; });
        using EditableDocument doc = metaFile.StorageSubFiles.Any(a => a.Value.IsEdited)
            ? await EditableDocumentFromMarkup(metaFile.StorageSubFiles["0"])
            : editor.Edit();
        MemoryStream outputStream = new();
        editor.Save(doc, outputStream, request.SaveOptions);
        outputStream.Seek(0, SeekOrigin.Begin);
        return new FileContent
        {
            FileName = $"{Path.GetFileNameWithoutExtension(metaFile.OriginalFile.FileName)}.{request.Format}",
            ResourceStream = outputStream,
            ResourceType = ResourceType.ConvertedDocument
        };
    }

    public IEnumerable<TFormat> GetSupportedFormats<TFormat>() where TFormat : IDocumentFormat
    {
        if (typeof(TFormat) == typeof(WordProcessingFormats))
        {
            return WordProcessingFormats.All.Cast<TFormat>().GroupBy(a => a.Extension).Select(a => a.First());
        }
        if (typeof(TFormat) == typeof(PresentationFormats))
        {
            return PresentationFormats.All.Cast<TFormat>().GroupBy(a => a.Extension).Select(a => a.First());
        }
        if (typeof(TFormat) == typeof(SpreadsheetFormats))
        {
            return SpreadsheetFormats.All.Cast<TFormat>().GroupBy(a => a.Extension).Select(a => a.First());
        }
        if (typeof(TFormat) == typeof(EmailFormats))
        {
            return EmailFormats.All.Cast<TFormat>().GroupBy(a => a.Extension).Select(a => a.First());
        }
        return WordProcessingFormats.All.Cast<TFormat>().GroupBy(a => a.Extension).Select(a => a.First());
    }

    public async Task<StorageResponse<StorageSubFile<TEditOptions>>> UpdateHtmlContent(StorageSubFile<TEditOptions> currentContent,
        string htmlContents)
    {
        await _storage.RemoveFile(PathBuilder.New(currentContent.DocumentCode, new[] { currentContent.SubCode, currentContent.EditedHtmlName }));
        if (currentContent.Resources.ContainsKey(currentContent.EditedHtmlName))
        {
            currentContent.Resources.Remove(currentContent.EditedHtmlName);
        }
        using var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        await writer.WriteAsync(htmlContents);
        await writer.FlushAsync();
        stream.Seek(0, SeekOrigin.Begin);
        var storageFile =
            (await _storage.SaveFile(new[] { new FileContent { FileName = currentContent.EditedHtmlName, ResourceStream = stream, ResourceType = ResourceType.HtmlContent } },
                PathBuilder.New(currentContent.DocumentCode, new[] { currentContent.SubCode }))).FirstOrDefault();
        if (storageFile is not { IsSuccess: true } || storageFile.Response == null)
        {
            return StorageResponse<StorageSubFile<TEditOptions>>.CreateFailed(new StorageSubFile<TEditOptions>(string.Empty, string.Empty));
        }
        currentContent.IsEdited = true;
        currentContent.Resources.Add(storageFile.Response.FileName, storageFile.Response);
        return StorageResponse<StorageSubFile<TEditOptions>>.CreateSuccess(currentContent);
    }

    public async Task<StorageUpdateResourceResponse<StorageSubFile<TEditOptions>, StorageFile>> UpdateResource(StorageSubFile<TEditOptions> currentContent, UploadResourceRequest resource)
    {
        if (!string.IsNullOrWhiteSpace(resource.OldResourceName))
        {
            await _storage.RemoveFile(PathBuilder.New(currentContent.DocumentCode, new[] { currentContent.SubCode, resource.OldResourceName }));
            if (currentContent.Resources.ContainsKey(resource.OldResourceName))
            {
                currentContent.Resources.Remove(resource.OldResourceName);
            }
        }
        ResourceType type;
        switch (resource.ResourceType)
        {
            case ResourceType.Stylesheet:
                type = ResourceType.Stylesheet;
                break;
            case ResourceType.Image:
                type = ResourceType.Image;
                break;
            case ResourceType.Font:
                type = ResourceType.Font;
                break;
            case ResourceType.Audio:
                type = ResourceType.Audio;
                break;
            default:
                _logger.LogError("Resource with type: {type} is unknown", resource.ResourceType);
                throw new ArgumentOutOfRangeException($"Resource with type: {resource.ResourceType} is unknown");
        }
        await using var fileStream = resource.File.OpenReadStream();
        var storageFile =
            (await _storage.SaveFile(new[] { new FileContent { FileName = resource.File.FileName, ResourceStream = fileStream, ResourceType = type } },
                PathBuilder.New(currentContent.DocumentCode, new[] { currentContent.SubCode }))).FirstOrDefault();
        if (storageFile is not { IsSuccess: true } || storageFile.Response == null)
        {
            return StorageUpdateResourceResponse<StorageSubFile<TEditOptions>, StorageFile>.CreateFailed(new StorageSubFile<TEditOptions>(string.Empty, string.Empty), new StorageFile());
        }
        currentContent.Resources.Add(storageFile.Response.FileName, storageFile.Response);

        return StorageUpdateResourceResponse<StorageSubFile<TEditOptions>, StorageFile>.CreateSuccess(currentContent, storageFile.Response);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) { return; }
        _editor?.Dispose();
        _originalFIleStream?.Dispose();
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

    private async Task<EditableDocument> EditableDocumentFromMarkup(StorageSubFile<TEditOptions> oneSubFile)
    {
        try
        {
            var response =
                await _storage.GetFileText(PathBuilder.New(oneSubFile.DocumentCode, new[] { oneSubFile.SubCode, oneSubFile.EditedHtmlName }));
            if (response is not { IsSuccess: true } || response.Response == null)
            {
                throw new ArgumentException("Cannot get a html file from storage", nameof(response.Response));
            }

            List<IHtmlResource> resources = new(oneSubFile.Resources.Count);
            foreach (var oneFile in oneSubFile.Resources.Values)
            {
                var stream = await _storage.DownloadFile(PathBuilder.New(oneSubFile.DocumentCode, new[] { oneSubFile.SubCode, oneFile.FileName }));
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
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to generate EditableDocument from markup");
            throw;
        }
    }
}