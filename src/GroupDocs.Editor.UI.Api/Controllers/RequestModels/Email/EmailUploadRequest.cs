using System.ComponentModel.DataAnnotations;
using GroupDocs.Editor.Options;
using Microsoft.AspNetCore.Http;

namespace GroupDocs.Editor.UI.Api.Controllers.RequestModels.Email;

public class EmailUploadRequest
{
    /// <summary>
    /// Gets or sets the file.
    /// </summary>
    /// <value>
    /// The file.
    /// </value>
    [Required]
    public IFormFile File { get; set; }
}