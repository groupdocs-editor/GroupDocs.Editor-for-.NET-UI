using GroupDocs.Editor.UI.Api.Services.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GroupDocs.Editor.UI.Api.Services.Licensing;

internal class RemoteUrlFileLicenseService : LicenseServiceBase<RemoteUrlFileLicenseService>
{
    public RemoteUrlFileLicenseService(
        IHostApplicationLifetime appLifetime,
        ILogger<RemoteUrlFileLicenseService> logger,
        IOptions<LicenseOptions> options) : base(appLifetime, logger, options)
    {
    }

    protected override void SetLicenseLocked()
    {
        using HttpClient client = new();
        var data = client.GetAsync(_options.Source).Result;
        _license.SetLicense(data.Content.ReadAsStream());
        _licenseSet = true;
        _logger.LogInformation("License was set success");
    }
}