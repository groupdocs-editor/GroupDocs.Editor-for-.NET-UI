using System.Text.Json;
using System.Text.Json.Serialization;
using GroupDocs.Editor.Formats;

namespace GroupDocs.Editor.UI.Api.JsonConverters;

public class PresentationFormatsJsonConverter : JsonConverter<PresentationFormats>
{
    public override PresentationFormats Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        return PresentationFormats.FromExtension(reader.GetString());
    }

    public override void Write(
        Utf8JsonWriter writer,
        PresentationFormats format,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(format.Extension);
    }
}