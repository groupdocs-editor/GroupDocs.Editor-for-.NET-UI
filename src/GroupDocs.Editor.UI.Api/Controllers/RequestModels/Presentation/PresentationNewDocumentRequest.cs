using GroupDocs.Editor.Formats;
using GroupDocs.Editor.UI.Api.JsonConverters;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GroupDocs.Editor.UI.Api.Controllers.RequestModels.Presentation;

public class PresentationNewDocumentRequest
{
    public string? FileName { get; set; }

    [Required]
    [JsonConverter(typeof(FormatJsonConverter))]
    public IDocumentFormat Format { get; set; } = PresentationFormats.Pptx;
}