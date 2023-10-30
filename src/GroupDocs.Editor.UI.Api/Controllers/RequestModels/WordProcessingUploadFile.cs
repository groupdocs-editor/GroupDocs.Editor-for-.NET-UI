using GroupDocs.Editor.Options;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace GroupDocs.Editor.UI.Api.Controllers.RequestModels;

public class UploadWordProcessingRequest
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
    [Required]
    public WordProcessingLoadOptions LoadOptions { get; set; }

    /// <summary>
    /// Gets or sets the edit options.
    /// </summary>
    /// <value>
    /// The edit options.
    /// </value>
    [Required]
    public WordProcessingEditOptions EditOptions { get; set; }
}