namespace GroupDocs.Editor.UI.Api.Services.Options;

public enum LicenseSourceType
{
    /// <summary>
    /// The license stored locally.
    /// </summary>
    LocalPath = 0,

    /// <summary>
    /// The license stored remote and we should reed it by URL
    /// </summary>
    RemoteUrl = 1,

    /// <summary>
    /// The license stored base64 string
    /// </summary>
    Base64 = 2,
}