using System.Text.Json.Serialization;

namespace GroupDocs.Editor.UI.Api.Models.Storage;

public class StorageSubFile
{
    public int SubCode { get; set; }
    public bool IsEdited { get; set; }
    public StorageFile SourceDocument { get; set; }
    public string SourceDocumentName { get; set; }
    public string EditedHtmlName => $"{Path.GetFileNameWithoutExtension(SourceDocumentName)}.html";
    public Guid DocumentCode { get; set; }
    public ICollection<StorageFile> Images { get; set; } = new List<StorageFile>();
    public ICollection<StorageFile> Stylesheets { get; set; } = new List<StorageFile>();
    public ICollection<StorageFile> Fonts { get; set; } = new List<StorageFile>();
    public ICollection<StorageFile> Audios { get; set; } = new List<StorageFile>();

    [JsonIgnore] public IEnumerable<StorageFile> AllResources => Images.Union(Stylesheets).Union(Fonts).Union(Audios);
}