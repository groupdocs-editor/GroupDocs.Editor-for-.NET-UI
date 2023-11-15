using GroupDocs.Editor.UI.Api.Models.DocumentConvertor;

namespace GroupDocs.Editor.UI.Api.Models.Storage.Requests;

public class UploadOriginalRequest
{
    public FileContent FileContent { get; set; }
    public StorageDocumentInfo DocumentInfo { get; set; }
}