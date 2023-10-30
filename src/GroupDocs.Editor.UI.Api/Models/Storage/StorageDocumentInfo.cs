using GroupDocs.Editor.Formats;
using GroupDocs.Editor.Metadata;
using GroupDocs.Editor.UI.Api.JsonConverters;
using System.Text.Json.Serialization;

namespace GroupDocs.Editor.UI.Api.Models.Storage;

public class StorageDocumentInfo : IDocumentInfo
{
    public int PageCount { get; set; }
    public long Size { get; set; }
    public bool IsEncrypted { get; set; }

    [JsonConverter(typeof(FormatJsonConverter))]
    public IDocumentFormat Format { get; set; }
}