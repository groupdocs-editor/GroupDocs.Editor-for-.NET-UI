using GroupDocs.Editor.Options;
using GroupDocs.Editor.UI.Api.Models.Storage;

namespace GroupDocs.Editor.UI.Api.Controllers.ResponseModels;

public class DocumentUploadResponse<TLoadOptions> where TLoadOptions : ILoadOptions
{
    /// <summary>
    /// Gets or sets the original document information.
    /// </summary>
    /// <value>
    /// The original file.
    /// </value>
    public StorageFile OriginalFile { get; set; }

    public TLoadOptions? OriginalLoadOptions { get; set; }

    /// <summary>
    /// Gets or sets the document code.
    /// </summary>
    /// <value>
    /// The document code.
    /// </value>
    public Guid DocumentCode { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the document information.
    /// </summary>
    /// <value>
    /// The document information.
    /// </value>
    public StorageDocumentInfo DocumentInfo { get; set; }
}