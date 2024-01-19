using GroupDocs.Editor.Options;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace GroupDocs.Editor.UI.Api.Controllers.RequestModels.Presentation;

public class PresentationUploadRequest
{
    /// <summary>
    /// Gets or sets the file.
    /// </summary>
    /// <value>
    /// The file.
    /// </value>
    [Required]
    public IFormFile File { get; set; }

    /// <summary>
    /// Gets or sets the load options.
    /// </summary>
    /// <value>
    /// The load options.
    /// </value>
    public PresentationLoadOptions? LoadOptions { get; set; }
}