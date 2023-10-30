using System.ComponentModel.DataAnnotations;

namespace GroupDocs.Editor.UI.Api.Controllers.RequestModels;

/// <summary>
/// Retrieves converted content based on the provided Code and SubIndex.
/// </summary>
public class ContentRequest
{
    /// <summary>
    /// Gets or sets the document code.
    /// </summary>
    /// <value>
    /// The document code.
    /// </value>
    [Required]
    public Guid DocumentCode { get; set; }

    /// <summary>
    /// The target subfolder index-name where the files should be saved.
    /// </summary>
    /// <value>
    /// The index of the sub.
    /// </value>
    public int SubIndex { get; set; }
}