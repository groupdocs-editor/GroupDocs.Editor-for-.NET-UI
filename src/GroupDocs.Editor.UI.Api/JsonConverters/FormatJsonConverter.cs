using GroupDocs.Editor.Formats;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GroupDocs.Editor.UI.Api.JsonConverters;

public class FormatJsonConverter : JsonConverter<IDocumentFormat>
{
    public override IDocumentFormat Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var text = reader.GetString();
        if (WordProcessingFormats.All.Any(a =>
                string.Equals(a.Extension, text, StringComparison.CurrentCultureIgnoreCase)))
        {
            return WordProcessingFormats.FromExtension(reader.GetString());
        }
        if (FixedLayoutFormats.All.Any(a =>
                string.Equals(a.Extension, text, StringComparison.CurrentCultureIgnoreCase)))
        {
            return FixedLayoutFormats.FromExtension(reader.GetString());
        }
        if (PresentationFormats.All.Any(a =>
                string.Equals(a.Extension, text, StringComparison.CurrentCultureIgnoreCase)))
        {
            return PresentationFormats.FromExtension(reader.GetString());
        }
        return WordProcessingFormats.Docx;
    }

    public override void Write(
        Utf8JsonWriter writer,
        IDocumentFormat format,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(format.Extension);
    }
}