using GroupDocs.Editor.UI.Api.Services.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GroupDocs.Editor.UI.Api.Services.Licensing;

internal abstract class LicenseServiceBase<T> : ILicenseService where T : ILicenseService
{
    protected readonly ILogger<T> _logger;
    protected readonly LicenseOptions _options;
    protected readonly object _lock = new();
    protected bool _licenseSet;
    protected License _license = new();

    protected LicenseServiceBase(
        IHostApplicationLifetime appLifetime,
        ILogger<T> logger,
        IOptions<LicenseOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        appLifetime.ApplicationStarted.Register(SetLicense);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public void SetLicense()
    {
        if (_licenseSet) return;
        lock (_lock)
        {
            SetLicenseLocked();
        }
    }

    protected abstract void SetLicenseLocked();
}