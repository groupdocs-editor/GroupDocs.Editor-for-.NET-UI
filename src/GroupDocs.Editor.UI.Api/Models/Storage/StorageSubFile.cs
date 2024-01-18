using GroupDocs.Editor.Options;
using System.Text.Json.Serialization;

namespace GroupDocs.Editor.UI.Api.Models.Storage;

public class StorageSubFile<TEditOptions> where TEditOptions : IEditOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StorageSubFile{TEditOptions}"/> class.
    /// </summary>
    /// <param name="originalDocumentName">Name of the original document.</param>
    /// <param name="subCode">The sub code.</param>
    public StorageSubFile(string originalDocumentName, string subCode)
    {
        OriginalDocumentName = originalDocumentName;
        SubCode = subCode;
    }

    public TEditOptions? EditOptions { get; set; }

    public string SubCode { get; set; }

    public bool IsEdited { get; set; }

    public string OriginalDocumentName { get; set; }

    public string EditedHtmlName => $"{Path.GetFileNameWithoutExtension(OriginalDocumentName)}.html";

    public Guid DocumentCode { get; set; }

    public Dictionary<string, StorageFile> Resources { get; set; } = new();

    [JsonIgnore] public IEnumerable<StorageFile> Images => Resources.Values.Where(a => a.ResourceType == ResourceType.Image);

    [JsonIgnore] public IEnumerable<StorageFile> Stylesheets => Resources.Values.Where(a => a.ResourceType == ResourceType.Stylesheet);

    [JsonIgnore] public IEnumerable<StorageFile> Fonts => Resources.Values.Where(a => a.ResourceType == ResourceType.Font);

    [JsonIgnore] public IEnumerable<StorageFile> Audios => Resources.Values.Where(a => a.ResourceType == ResourceType.Audio);

    [JsonIgnore] public StorageFile ConvertedDocument => Resources.Values.Single(a => a.ResourceType == ResourceType.HtmlContent);
}