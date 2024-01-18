using GroupDocs.Editor.Formats;

namespace GroupDocs.Editor.UI.Api.Models.Editor;

public class CreateDocumentRequest
{
    public string FileName { get; set; }

    public IDocumentFormat Format { get; set; }
}