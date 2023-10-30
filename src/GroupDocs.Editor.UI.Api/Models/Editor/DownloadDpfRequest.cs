using GroupDocs.Editor.Options;

namespace GroupDocs.Editor.UI.Api.Models.Editor;

public class DownloadPdfRequest
{
    public Guid DocumentCode { get; set; }

    public ILoadOptions LoadOptions { get; set; }

    public PdfSaveOptions SaveOptions { get; set; }
}