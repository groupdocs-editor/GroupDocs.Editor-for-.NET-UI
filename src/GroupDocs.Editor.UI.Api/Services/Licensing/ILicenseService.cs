using Microsoft.Extensions.Hosting;

namespace GroupDocs.Editor.UI.Api.Services.Licensing;

internal interface ILicenseService : IHostedService
{
    public void SetLicense();
}