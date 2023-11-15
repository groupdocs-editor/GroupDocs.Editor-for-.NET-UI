using GroupDocs.Editor.UI.Api.Services.Options;
using Microsoft.Extensions.Configuration;

namespace GroupDocs.Editor.UI.Api.Test.SetupApp;

public static class TestConfigHelper
{
    public static IConfigurationRoot IConfiguration()
    {
        return new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddUserSecrets("ee4eb786-0019-49ab-98c3-b58318e89d49")
            .AddEnvironmentVariables()
            .Build();
    }

    public static AwsTestOption BuildAwsTestOption(this IConfigurationRoot configuration)
    {
        AwsTestOption options = new();
        IConfigurationSection section = configuration.GetSection("AWS");
        section.Bind(options);
        return options;
    }

    public static AzureBlobOptions BuildAzureTestOption(this IConfigurationRoot configuration)
    {
        AzureBlobOptions options = new();
        IConfigurationSection section = configuration.GetSection(nameof(AzureBlobOptions));
        section.Bind(options);
        return options;
    }
}


