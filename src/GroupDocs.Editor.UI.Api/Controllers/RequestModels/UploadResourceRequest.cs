using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace GroupDocs.Editor.UI.Api.Controllers.RequestModels;

/// <summary>
/// Uploads a new resource file or replaces an existing one with the same name.
/// </summary>
public class UploadResourceRequest
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
    /// Gets or sets the document code.
    /// </summary>
    /// <value>
    /// The document code.
    /// </value>
    [Required]
    public Guid DocumentCode { get; set; }

    /// <summary>
    /// Gets or sets the name of the resource that will be update. If value is <c>string.Empty</c> or <c>null</c> do nothing.
    /// </summary>
    /// <value>
    /// The old name of the image.
    /// </value>
    public string OldResorceName { get; set; }

    /// <summary>
    /// The target subfolder index-name where the files should be saved.
    /// </summary>
    /// <value>
    /// The index of the sub.
    /// </value>
    public int SubIndex { get; set; }

    /// <summary>
    /// Gets or sets the type of the resource that try to update.
    /// </summary>
    /// <value>
    /// The type of the resource.
    /// </value>
    public ResourceType ResourceType { get; set; }
}