using GroupDocs.Editor.Options;
using System.ComponentModel.DataAnnotations;

namespace GroupDocs.Editor.UI.Api.Controllers.RequestModels.WordProcessing;

public class WordProcessingToPdfDownloadRequest
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
    /// Gets or sets the save options.
    /// </summary>
    /// <value>
    /// The save options.
    /// </value>
    [Required]
    public PdfSaveOptions SaveOptions { get; set; }
}