namespace GroupDocs.Editor.UI.Api.Controllers.RequestModels;

public class UploadResourceRequestPaged : UploadResourceRequest
{
    /// <summary>
    /// The target subfolder index-name where the files should be saved.
    /// </summary>
    /// <value>
    /// The index of the sub.
    /// </value>
    public string SubIndex { get; set; }
}