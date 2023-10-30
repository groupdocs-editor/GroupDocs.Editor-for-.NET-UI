using GroupDocs.Editor.UI.Api.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using System.Web;

namespace GroupDocs.Editor.UI.Api.Controllers;

[ApiController]
[Route("file/{documentCode:guid}")]
public class LocalFileController : ControllerBase
{
    private readonly ILogger<LocalFileController> _logger;
    protected readonly IStorage _storage;

    public LocalFileController(
        ILogger<LocalFileController> logger,
        IStorage storage)
    {
        _logger = logger;
        _storage = storage;
    }

    /// <summary>
    /// This method is used to download a file from a subfolder within the storage. The target document is located in the specified subfolder.
    /// For WordProcessing, the subfolder index is always 0.
    /// For Presentation and Spreadsheet, the subfolder index corresponds to the index of the worksheet or slide.
    /// </summary>
    /// <param name="documentCode">The document code.</param>
    /// <param name="subDocumentIndex">Index of the sub document.</param>
    /// <param name="fileName">Name of the file.</param>
    /// <returns></returns>
    [HttpGet("{subDocumentIndex}/{fileName}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadFromSubDocument(Guid documentCode, int subDocumentIndex, string fileName)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState.ValidationState);
        }

        _logger.LogInformation("try to download file: {fileName} for page {page}, document: {documentCode}", fileName, subDocumentIndex, documentCode);
        var response = await _storage.DownloadFile(Path.Combine(documentCode.ToString(), subDocumentIndex.ToString(), HttpUtility.UrlDecode(fileName)));
        if (response is not { IsSuccess: true } || response.Response == null)
        {
            return BadRequest(response.Status.ToString());
        }
        new FileExtensionContentTypeProvider().TryGetContentType(fileName, out var contentType);
        return File(response.Response, contentType ?? "application/octet-stream", fileName);
    }

    /// <summary>
    /// This method is used to download a file from a storage.
    /// </summary>
    /// <param name="documentCode">The document code.</param>
    /// <param name="fileName">Name of the file.</param>
    /// <returns></returns>
    [HttpGet("{fileName}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Download(Guid documentCode, string fileName)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState.ValidationState);
        }
        _logger.LogInformation("try to download file: {fileName} from document: {documentCode}", fileName, documentCode);
        var response = await _storage.DownloadFile(Path.Combine(documentCode.ToString(), HttpUtility.UrlDecode(fileName)));

        if (response is not { IsSuccess: true } || response.Response == null)
        {
            return BadRequest(response.Status.ToString());
        }
        new FileExtensionContentTypeProvider().TryGetContentType(fileName, out var contentType);
        return File(response.Response, contentType ?? "application/octet-stream", fileName);
    }
}