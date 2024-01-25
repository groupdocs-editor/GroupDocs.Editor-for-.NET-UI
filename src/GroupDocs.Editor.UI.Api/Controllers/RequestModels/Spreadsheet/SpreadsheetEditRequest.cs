using GroupDocs.Editor.Options;
using System.ComponentModel.DataAnnotations;

namespace GroupDocs.Editor.UI.Api.Controllers.RequestModels.Spreadsheet;

public class SpreadsheetEditRequest
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
    public SpreadsheetEditOptions? EditOptions { get; set; }
}