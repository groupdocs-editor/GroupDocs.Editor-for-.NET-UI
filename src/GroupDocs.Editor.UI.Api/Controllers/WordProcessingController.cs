using AutoMapper;
using GroupDocs.Editor.UI.Api.Controllers.RequestModels;
using GroupDocs.Editor.UI.Api.Models.Editor;
using GroupDocs.Editor.UI.Api.Models.Storage;
using GroupDocs.Editor.UI.Api.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;

namespace GroupDocs.Editor.UI.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class WordProcessingController : ControllerBase
{
    private readonly ILogger<WordProcessingController> _logger;
    protected readonly IEditorService _editorService;
    protected readonly IStorage _storage;
    protected readonly IMetaFileStorageCache _storageCache;
    protected readonly IMapper _mapper;

    public WordProcessingController(ILogger<WordProcessingController> logger, IEditorService editorService, IMapper mapper, IStorage storage, IMetaFileStorageCache storageCache)
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
    [HttpPost("Upload")]
    [ProducesResponseType(typeof(StorageMetaFile), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload([FromForm] UploadWordProcessingRequest file)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState.ValidationState);
        }

        try
        {
            var document = await _editorService.SaveDocument(_mapper.Map<SaveDocumentRequest>(file));
            if (document == null)
            {
                return BadRequest(ModelState.ValidationState);
            }
            return Ok(document);
        }
        catch (Exception e)
        {
            return BadRequest(e);
        }
    }

    [HttpPost("NewDocument")]
    [ProducesResponseType(typeof(StorageMetaFile), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> NewDocument(NewWordProcessingDocumentRequest file)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState.ValidationState);
        }

        var document = await _editorService.SaveDocument(_mapper.Map<SaveDocumentRequest>(file));
        if (document == null)
        {
            return BadRequest(ModelState.ValidationState);
        }
        return Ok(document);
    }

    /// <summary>
    /// Downloads a specific document in the target format. If the original document is available, it will be converted.
    /// If there are edited versions, the latest one will be used for conversion.
    /// </summary>
    /// <param name="request">The request with specific document code, load and save option.</param>
    /// <returns></returns>
    [HttpPost("DownloadInFormat")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Download(WordProcessingDownloadRequest request)
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

    [HttpPost("DownloadPdf")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadPdf(PdfDownloadRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState.ValidationState);
        }

        var result = await _editorService.SaveToPdf(_mapper.Map<DownloadPdfRequest>(request));
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
    [HttpPost("Update")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status200OK)]
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
        var response = await _storage.UpdateHtmlContent(meta.StorageSubFiles[request.SubIndex], request.HtmlContents);
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
    [HttpPost("UploadResource")]
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

        var response = await _storage.UpdateResource(
            meta.StorageSubFiles[resource.SubIndex], resource);
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
    /// Get all previews document as the images.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns></returns>
    [HttpPost("PreviewImages")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(IDictionary<int, StorageFile>), StatusCodes.Status200OK)]
    public async Task<IActionResult> PreviewImages([FromBody] PreviewRequest request)
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

        if (!meta.PreviewImages.Any())
        {
            meta = await _editorService.ConvertPreviews(request.DocumentCode, request.LoadOptions);
        }
        return Ok(meta?.PreviewImages);
    }

    /// <summary>
    /// Get all stylesheets in the specified document.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns></returns>
    [HttpPost("Stylesheets")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ICollection<StorageFile>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Stylesheets([FromBody] StylesheetsRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState.ValidationState);
        }
        _logger.LogInformation("try to upload resource file {request}", request);
        var meta = await _storageCache.DownloadFile(request.DocumentCode);
        if (meta == null)
        {
            return BadRequest("file not exist");
        }

        var page = meta.StorageSubFiles[request.SubIndex];
        return Ok(page.Stylesheets);
    }

    [HttpPost("ConvertedContent")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConvertedContent([FromBody] ContentRequest request)
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

        var page = meta.StorageSubFiles[request.SubIndex];
        var response = await _storage.GetFileText(Path.Combine(page.DocumentCode.ToString(), page.SubCode.ToString(), page.EditedHtmlName));
        if (response is not { IsSuccess: true } || response.Response == null)
        {
            return BadRequest(response.Status.ToString());
        }
        return Ok(response.Response);
    }

    [HttpGet("DocumentStructure/{documentCode:guid}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(StorageMetaFile), StatusCodes.Status200OK)]
    public async Task<IActionResult> DocumentStructure(Guid documentCode)
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

        return Ok(meta);
    }

    [HttpGet("SupportedFormats")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(Dictionary<string, string>), StatusCodes.Status200OK)]
    public IActionResult SupportedFormats()
    {
        Dictionary<string, string> result = _editorService.GetSupportedFormats()
            .ToDictionary(format => format.Extension, format => format.Name);
        return Ok(result);
    }
}