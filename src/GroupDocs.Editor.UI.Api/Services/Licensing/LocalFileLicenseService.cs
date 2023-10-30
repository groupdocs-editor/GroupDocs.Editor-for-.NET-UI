using GroupDocs.Editor.UI.Api.Services.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GroupDocs.Editor.UI.Api.Services.Licensing;

internal class LocalFileLicenseService : LicenseServiceBase<LocalFileLicenseService>
{
    public LocalFileLicenseService(
        IHostApplicationLifetime appLifetime,
        ILogger<LocalFileLicenseService> logger,
        IOptions<LicenseOptions> options) : base(appLifetime, logger, options)
    {
    }

    protected override void SetLicenseLocked()
    {
        _license.SetLicense(_options.Source);
        _licenseSet = true;
        _logger.LogInformation("License was set success");
    }
}