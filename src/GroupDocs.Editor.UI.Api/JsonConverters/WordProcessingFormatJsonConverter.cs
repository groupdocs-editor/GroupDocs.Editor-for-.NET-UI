using GroupDocs.Editor.Formats;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GroupDocs.Editor.UI.Api.JsonConverters;

public class WordProcessingFormatJsonConverter : JsonConverter<WordProcessingFormats>
{
    public override WordProcessingFormats Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        return WordProcessingFormats.FromExtension(reader.GetString());
    }

    public override void Write(
        Utf8JsonWriter writer,
        WordProcessingFormats format,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(format.Extension);
    }
}