using GroupDocs.Editor.Formats;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GroupDocs.Editor.UI.Api.JsonConverters;

public class EmailFormatJsonConverter : JsonConverter<EmailFormats>
{
    public override EmailFormats Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        return EmailFormats.FromExtension(reader.GetString());
    }

    public override void Write(
        Utf8JsonWriter writer,
        EmailFormats format,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(format.Extension);
    }
}