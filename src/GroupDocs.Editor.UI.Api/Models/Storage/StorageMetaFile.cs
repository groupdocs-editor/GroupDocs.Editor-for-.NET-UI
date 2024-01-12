using GroupDocs.Editor.Options;

namespace GroupDocs.Editor.UI.Api.Models.Storage;

/// <summary>
/// Represents the structure of a storage system that stores original documents, previews, and converted documents.
/// </summary>
public class StorageMetaFile<TLoadOptions, TEditOptions>
    where TLoadOptions : ILoadOptions
    where TEditOptions : IEditOptions
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

    /// <summary>
    /// Gets or sets the preview images structure links.
    /// </summary>
    /// <value>
    /// The preview images.
    /// </value>
    public IDictionary<string, StorageFile> PreviewImages { get; set; } = new Dictionary<string, StorageFile>();

    /// <summary>
    /// The converted documents is located in the specified subfolder.
    /// For WordProcessing, the subfolder index is always 0. For Presentation and Spreadsheet, the subfolder index corresponds to the index of the worksheet or slide.
    /// </summary>
    /// <value>
    /// The storage sub files.
    /// </value>
    public IDictionary<string, StorageSubFile<TEditOptions>> StorageSubFiles { get; set; } = new Dictionary<string, StorageSubFile<TEditOptions>>();
}