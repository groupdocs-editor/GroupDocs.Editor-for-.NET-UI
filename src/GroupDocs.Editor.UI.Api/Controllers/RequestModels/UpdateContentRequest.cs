using System.ComponentModel.DataAnnotations;

namespace GroupDocs.Editor.UI.Api.Controllers.RequestModels;

/// <summary>
/// Request to updates the HTML content of a document based on its unique document code.
/// </summary>
public class UpdateContentRequest
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
    /// Gets or sets the HTML contents.
    /// </summary>
    /// <value>
    /// The HTML contents.
    /// </value>
    [Required]
    public string HtmlContents { get; set; }
}

public class UpdateContentRequestPaged : UpdateContentRequest
{
    /// <summary>
    /// The target subfolder index-name where the files should be saved.
    /// </summary>
    /// <value>
    /// The index of the sub.
    /// </value>
    public string SubIndex { get; set; }
}