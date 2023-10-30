using GroupDocs.Editor.Options;
using System.ComponentModel.DataAnnotations;

namespace GroupDocs.Editor.UI.Api.Controllers.RequestModels;

/// <summary>
/// Retrieves all previews based on the provided document Code and load options.
/// </summary>
public class PreviewRequest
{
    [Required]
    public Guid DocumentCode { get; set; }

    [Required]
    public WordProcessingLoadOptions LoadOptions { get; set; }
}