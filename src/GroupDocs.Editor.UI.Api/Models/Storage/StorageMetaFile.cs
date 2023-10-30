namespace GroupDocs.Editor.UI.Api.Models.Storage;

/// <summary>
/// Represents the structure of a storage system that stores original documents, previews, and converted documents.
/// </summary>
public class StorageMetaFile
{
    /// <summary>
    /// Gets or sets the original document information.
    /// </summary>
    /// <value>
    /// The original file.
    /// </value>
    public StorageFile OriginalFile { get; set; }

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

    /// <summary>
    /// Gets or sets the preview images structure links.
    /// </summary>
    /// <value>
    /// The preview images.
    /// </value>
    public IDictionary<int, StorageFile> PreviewImages { get; set; } = new Dictionary<int, StorageFile>();

    /// <summary>
    /// The converted documents is located in the specified subfolder.
    /// For WordProcessing, the subfolder index is always 0. For Presentation and Spreadsheet, the subfolder index corresponds to the index of the worksheet or slide.
    /// </summary>
    /// <value>
    /// The storage sub files.
    /// </value>
    public IDictionary<int, StorageSubFile> StorageSubFiles { get; set; } = new Dictionary<int, StorageSubFile>();
}