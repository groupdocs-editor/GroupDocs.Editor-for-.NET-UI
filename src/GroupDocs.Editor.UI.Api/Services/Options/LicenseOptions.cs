namespace GroupDocs.Editor.UI.Api.Services.Options;

public class LicenseOptions
{
    /// <summary>
    /// Gets or sets the type of license source. Default value <c>LocalPath</c> or <c>0</c>
    /// </summary>
    /// <value>
    /// The type.
    /// </value>
    public LicenseSourceType Type { get; set; } = LicenseSourceType.LocalPath;

    public string Source { get; set; }
}