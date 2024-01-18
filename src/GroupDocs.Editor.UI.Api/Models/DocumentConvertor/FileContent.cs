using GroupDocs.Editor.UI.Api.Models.Storage;

namespace GroupDocs.Editor.UI.Api.Models.DocumentConvertor;

/// <summary>
/// Present one document or document's resource that was integrated into document.
/// </summary>
public class FileContent : IDisposable
{
    /// <summary>
    /// Gets or sets the name of the file.
    /// </summary>
    /// <value>
    /// The name of the file.
    /// </value>
    public string FileName { get; init; }

    /// <summary>
    /// Gets or sets the resource stream.
    /// </summary>
    /// <value>
    /// The resource stream.
    /// </value>
    public Stream ResourceStream { get; init; }

    public ResourceType ResourceType { get; init; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            ResourceStream?.Dispose();
        }
    }
}