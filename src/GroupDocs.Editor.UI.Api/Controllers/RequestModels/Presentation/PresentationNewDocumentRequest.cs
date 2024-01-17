using GroupDocs.Editor.Formats;
using System.ComponentModel.DataAnnotations;

namespace GroupDocs.Editor.UI.Api.Controllers.RequestModels.Presentation;

public class PresentationNewDocumentRequest
{
    public string? FileName { get; set; }

    [Required]
    public string Format { get; set; } = PresentationFormats.Pptx.Extension;
}