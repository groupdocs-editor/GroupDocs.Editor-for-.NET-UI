using Microsoft.Extensions.Hosting;

namespace GroupDocs.Editor.UI.Api.Services.Licensing;

public interface ILicenseService : IHostedService
{
    public void SetLicense();
}