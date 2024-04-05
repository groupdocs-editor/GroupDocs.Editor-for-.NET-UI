using AutoMapper;
using GroupDocs.Editor.Formats;
using GroupDocs.Editor.UI.Api.Controllers.RequestModels;
using GroupDocs.Editor.UI.Api.Controllers.RequestModels.Pdf;
using GroupDocs.Editor.UI.Api.Controllers.ResponseModels;
using GroupDocs.Editor.UI.Api.Extensions;
using GroupDocs.Editor.UI.Api.Models.Editor;
using GroupDocs.Editor.UI.Api.Models.Storage;
using GroupDocs.Editor.UI.Api.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement.Mvc;

namespace GroupDocs.Editor.UI.Api.Controllers;

[ApiController]
[FeatureGate(EditorProductFamily.Pdf)]
[ApiExplorerSettings(GroupName = EditorProductFamily.WordProcessing)]
[Route("[controller]")]
public class PdfController : ControllerBase
{
    private readonly ILogger<PdfController> _logger;
    private readonly IPdfEditorService _editorService;
    private readonly IStorage _storage;
    private readonly IPdfStorageCache _storageCache;
    private readonly IMapper _mapper;

    public PdfController(
        ILogger<PdfController> logger,
        IPdfEditorService editorService,
        IMapper mapper, IStorage storage,
        IPdfStorageCache storageCache)
    {
        _logger = logger;
        _editorService = editorService;
        _mapper = mapper;
        _storage = storage;
        _storageCache = storageCache;
    }


    /// <summary>
    /// This method uploads the specified document while converting it to HTML based on the provided input options.
    /// </summary>
    /// <param name="file">The upload request. Also specify load and edit option for converting to Html document.</param>
    /// <returns></returns>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(PdfUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload([FromForm] PdfUploadRequest file)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState.ValidationState);
        }

        try
        {
            var document = await _editorService.UploadDocument(_mapper.Map<UploadDocumentRequest>(file));
            return Ok(document);
        }
        catch (Exception e)
        {
            return BadRequest(e);
        }
    }

    [HttpPost("edit")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Edit([FromBody] PdfEditRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState.ValidationState);
        }
        _logger.LogInformation("try to get html file {request}", request);

        var meta = await _storageCache.DownloadFile(request.DocumentCode);
        if (meta == null)
        {
            return BadRequest("file not exist");
        }

        if (meta.StorageSubFiles.TryGetValue("0", out var page))
        {
            if (request.EditOptions.IsOptionsEquals(page.EditOptions))
            {
                var response = await _storage.DownloadFile(PathBuilder.New(page.DocumentCode, new[] { page.SubCode, page.EditedHtmlName }));
                if (response is not { IsSuccess: true } || response.Response == null)
                {
                    return BadRequest(response.Status.ToString());
                }
                return File(response.Response, "text/html", page.EditedHtmlName);
            }

            meta.StorageSubFiles.Remove("0");
            await _storageCache.UpdateFiles(meta);
        }

        var newContent = await _editorService.ConvertToHtml(meta, request.EditOptions, meta.OriginalLoadOptions);
        meta.StorageSubFiles.TryGetValue("0", out var pageSaved);
        return File(newContent ?? Stream.Null, "text/html", pageSaved?.EditedHtmlName);
    }

    /// <summary>
    /// Downloads a specific document in the target format. If the original document is available, it will be converted.
    /// If there are edited versions, the latest one will be used for conversion.
    /// </summary>
    /// <param name="request">The request with specific document code, load and save option.</param>
    /// <returns></returns>
    [HttpPost("downloadInFormat")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Download(PdfDownloadRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState.ValidationState);
        }

        var result =
            await _editorService.ConvertToDocument(_mapper.Map<DownloadDocumentRequest>(request));
        if (result == null)
        {
            return BadRequest("Cannot generate file for download");
        }
        new FileExtensionContentTypeProvider().TryGetContentType(result.FileName, out var contentType);

        return File(result.ResourceStream, contentType ?? "application/octet-stream", result.FileName);
    }

    /// <summary>
    /// Updates the specified HTML content.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns></returns>
    [HttpPost("update")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update([FromBody] UpdateContentRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState.ValidationState);
        }

        var meta = await _storageCache.DownloadFile(request.DocumentCode);
        if (meta == null)
        {
            return BadRequest("file not exist");
        }
        var response = await _editorService.UpdateHtmlContent(meta.StorageSubFiles["0"], request.HtmlContents);
        if (response is not { IsSuccess: true } || response.Response == null)
        {
            return BadRequest(response.Status.ToString());
        }

        if (meta.StorageSubFiles.ContainsKey(response.Response.SubCode))
        {
            meta.StorageSubFiles.Remove(response.Response.SubCode);
        }

        meta.StorageSubFiles.Add(response.Response.SubCode, response.Response);
        await _storageCache.UpdateFiles(meta);
        return NoContent();
    }

    /// <summary>
    /// Uploads the resource and allows replacing an existing one when specifying the OldResourceName.
    /// </summary>
    /// <param name="resource">The resource.</param>
    /// <returns></returns>
    [HttpPost("uploadResource")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(StorageFile), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadResource([FromForm] UploadResourceRequest resource)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState.ValidationState);
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState.ValidationState);
        }

        var meta = await _storageCache.DownloadFile(resource.DocumentCode);
        if (meta == null)
        {
            return BadRequest("file not exist");
        }

        var response = await _editorService.UpdateResource(
            meta.StorageSubFiles["0"], resource);
        if (response is not { IsSuccess: true } || response.Response == null)
        {
            return BadRequest(response.Status.ToString());
        }

        if (meta.StorageSubFiles.ContainsKey(response.Response.SubCode))
        {
            meta.StorageSubFiles.Remove(response.Response.SubCode);
        }

        meta.StorageSubFiles.Add(response.Response.SubCode, response.Response);
        await _storageCache.UpdateFiles(meta);
        return Ok(response.AdditionalData);
    }

    /// <summary>
    /// Get all stylesheets in the specified document.
    /// </summary>
    /// <param name="documentCode">The request.</param>
    /// <returns></returns>
    [HttpPost("stylesheets/{documentCode:guid}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(IEnumerable<StorageFile>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Stylesheets(Guid documentCode)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState.ValidationState);
        }
        _logger.LogInformation("try to upload resource file {request}", documentCode);
        var meta = await _storageCache.DownloadFile(documentCode);
        if (meta == null)
        {
            return BadRequest("file not exist");
        }

        var page = meta.StorageSubFiles["0"];
        return Ok(page.Stylesheets);
    }



    [HttpGet("metaInfo/{documentCode:guid}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(PdfStorageInfo), StatusCodes.Status200OK)]
    public async Task<IActionResult> MetaInfo(Guid documentCode)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState.ValidationState);
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState.ValidationState);
        }

        var meta = await _storageCache.DownloadFile(documentCode);
        if (meta == null)
        {
            return BadRequest("file not exist");
        }

        return Ok(_mapper.Map<PdfStorageInfo>(meta));
    }

    [HttpGet("supportedFormats")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(Dictionary<string, string>), StatusCodes.Status200OK)]
    public IActionResult SupportedFormats()
    {
        Dictionary<string, string> result = new() { { FixedLayoutFormats.Pdf.Extension, FixedLayoutFormats.Pdf.Name } };
        return Ok(result);
    }
}