using GroupDocs.Editor.Options;
using System.ComponentModel.DataAnnotations;

namespace GroupDocs.Editor.UI.Api.Controllers.RequestModels.WordProcessing;

public class WordProcessingNewDocumentRequest
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