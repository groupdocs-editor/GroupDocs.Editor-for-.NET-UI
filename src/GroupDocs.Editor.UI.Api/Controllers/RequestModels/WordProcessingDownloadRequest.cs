using GroupDocs.Editor.Options;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace GroupDocs.Editor.UI.Api.Controllers.RequestModels;

/// <summary>
/// Request for Downloads a specific document in the target format.
/// </summary>
public class WordProcessingDownloadRequest
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
    /// Gets or sets the load options.
    /// </summary>
    /// <value>
    /// The load options.
    /// </value>
    [Required]
    public WordProcessingLoadOptions LoadOptions { get; set; }

    /// <summary>
    /// Gets or sets the save options.
    /// </summary>
    /// <value>
    /// The save options.
    /// </value>
    [Required]
    public WordProcessingSaveOptions SaveOptions { get; set; }
}