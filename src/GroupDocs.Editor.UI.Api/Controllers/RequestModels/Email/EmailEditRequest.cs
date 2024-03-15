using System.ComponentModel.DataAnnotations;
using GroupDocs.Editor.Options;

namespace GroupDocs.Editor.UI.Api.Controllers.RequestModels.Email;

public class EmailEditRequest
{
    /// <summary>
    /// Gets or sets the document code.
    /// </summary>
    /// <value>
    /// The document code.
    /// </value>
    [Required]
    public Guid DocumentCode { get; set; }

    /// <summary>
    /// Gets or sets the edit options.
    /// </summary>
    /// <value>
    /// The edit options.
    /// </value>
    public EmailEditOptions? EditOptions { get; set; }
}