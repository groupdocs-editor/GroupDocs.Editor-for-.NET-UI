using GroupDocs.Editor.Formats;
using System.ComponentModel.DataAnnotations;

namespace GroupDocs.Editor.UI.Api.Controllers.RequestModels.Spreadsheet;

public class SpreadsheetNewDocumentRequest
{
    public string? FileName { get; set; }

    [Required]
    public string Format { get; set; } = SpreadsheetFormats.Xlsx.Extension;
}