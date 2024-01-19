using GroupDocs.Editor.Options;
using System.ComponentModel.DataAnnotations;

namespace GroupDocs.Editor.UI.Api.Controllers.RequestModels.Presentation;

public class PresentationEditRequest
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
    /// Gets or sets the edit options.
    /// </summary>
    /// <value>
    /// The edit options.
    /// </value>
    public PresentationEditOptions? EditOptions { get; set; }
}