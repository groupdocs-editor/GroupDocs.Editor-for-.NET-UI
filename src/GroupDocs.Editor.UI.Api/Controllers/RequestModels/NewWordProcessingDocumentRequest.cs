using System.ComponentModel.DataAnnotations;
using GroupDocs.Editor.Options;

namespace GroupDocs.Editor.UI.Api.Controllers.RequestModels;

public class NewWordProcessingDocumentRequest
{
    /// <summary>
    /// Gets or sets the edit options.
    /// </summary>
    /// <value>
    /// The edit options.
    /// </value>
    [Required]
    public WordProcessingEditOptions EditOptions { get; set; }
}