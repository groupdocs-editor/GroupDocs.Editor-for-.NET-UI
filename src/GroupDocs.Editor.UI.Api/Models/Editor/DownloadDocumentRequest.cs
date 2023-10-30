using GroupDocs.Editor.Options;

namespace GroupDocs.Editor.UI.Api.Models.Editor;

public class DownloadDocumentRequest
{
    public Guid DocumentCode { get; set; }

    public string Format { get; set; }

    public ILoadOptions LoadOptions { get; set; }

    public ISaveOptions SaveOptions { get; set; }
}