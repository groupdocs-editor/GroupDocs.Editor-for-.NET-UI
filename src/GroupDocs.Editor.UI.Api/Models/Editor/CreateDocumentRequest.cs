using GroupDocs.Editor.Formats.Abstraction;

namespace GroupDocs.Editor.UI.Api.Models.Editor;

public class CreateDocumentRequest
{
    public string FileName { get; set; }

    public DocumentFormatBase Format { get; set; }
}