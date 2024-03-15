using System.ComponentModel.DataAnnotations;
using GroupDocs.Editor.Options;
using Microsoft.AspNetCore.Mvc;

namespace GroupDocs.Editor.UI.Api.Controllers.RequestModels.Email;

/// <summary>
/// Request for Downloads a specific document in the target format.
/// </summary>
public class EmailDownloadRequest
{
    /// <summary>
    /// Gets or sets the document code.
    /// </summary>
    /// <value>
    /// The document code.
    /// </value>
    [Required]
    [FromQuery] public Guid DocumentCode { get; set; }

    /// <summary>
    /// Gets or sets the format.
    /// </summary>
    /// <value>
    /// The format.
    /// </value>
    [Required]
    [FromQuery] public string Format { get; set; }

    /// <summary>
    /// Gets or sets the save options.
    /// </summary>
    /// <value>
    /// The save options.
    /// </value>
    [Required]
    public EmailSaveOptions SaveOptions { get; set; }
}