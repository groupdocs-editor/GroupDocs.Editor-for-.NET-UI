using GroupDocs.Editor.Formats;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GroupDocs.Editor.UI.Api.JsonConverters;

public class SpreadsheetFormatJsonConverter : JsonConverter<SpreadsheetFormats>
{
    public override SpreadsheetFormats Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        return SpreadsheetFormats.FromExtension(reader.GetString());
    }

    public override void Write(
        Utf8JsonWriter writer,
        SpreadsheetFormats format,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(format.Extension);
    }
}