using GroupDocs.Editor.UI.Api.Services.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GroupDocs.Editor.UI.Api.Services.Licensing;

public class Base64FileLicenseService : LicenseServiceBase<Base64FileLicenseService>
{
    public Base64FileLicenseService(
        IHostApplicationLifetime appLifetime,
        ILogger<Base64FileLicenseService> logger,
        IOptions<LicenseOptions> options) : base(appLifetime, logger, options)
    {
    }

    protected override void SetLicenseLocked()
    {
        using var stream = new MemoryStream(Convert.FromBase64String(_options.Source));
        _license.SetLicense(stream);
        _licenseSet = true;
        _logger.LogInformation("License was set success");
    }
}