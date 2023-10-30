using GroupDocs.Editor.Options;

namespace GroupDocs.Editor.UI.Api.Models.Editor;

public class SaveDocumentRequest
{
    public string FileName { get; set; }
    public Stream Stream { get; set; }
    public IEditOptions EditOptions { get; set; }
    public ILoadOptions LoadOptions { get; set; }
}