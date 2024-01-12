using GroupDocs.Editor.Formats;
using GroupDocs.Editor.UI.Api.JsonConverters;
using System.Text.Json.Serialization;

namespace GroupDocs.Editor.UI.Api.Controllers.RequestModels.WordProcessing;

public class WordProcessingNewDocumentRequest
{
    public string? FileName { get; set; }

    [JsonConverter(typeof(FormatJsonConverter))]
    public IDocumentFormat Format { get; set; }
}