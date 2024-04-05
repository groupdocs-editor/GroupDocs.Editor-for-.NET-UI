using FluentAssertions;
using GroupDocs.Editor.UI.Api.Models.Storage;
using GroupDocs.Editor.UI.Api.Models.Storage.Responses;
using GroupDocs.Editor.UI.Api.Services.Implementation;
using GroupDocs.Editor.UI.Api.Services.Options;
using GroupDocs.Editor.UI.Api.Test.SetupApp;
using Microsoft.Extensions.Logging.Abstractions;

namespace GroupDocs.Editor.UI.Api.Test.Services.Implementation;

public class AzureBlobServiceTests
{
    private readonly AzureBlobStorage _storage;

    public AzureBlobServiceTests()
    {
        var azureConfiguration = TestConfigHelper.IConfiguration().BuildAzureTestOption();
        AzureBlobOptions azureBlobOptions = new()
        {
            AccountKey = (string.IsNullOrWhiteSpace(azureConfiguration.AccountKey)
                ? Environment.GetEnvironmentVariable("EDITOR_AZURE_KEY")
                : azureConfiguration.AccountKey) ?? "",
            AccountName = (string.IsNullOrWhiteSpace(azureConfiguration.AccountName)
                ? Environment.GetEnvironmentVariable("EDITOR_AZURE_NAME")
                : azureConfiguration.AccountName) ?? "",
            ContainerName = (string.IsNullOrWhiteSpace(azureConfiguration.ContainerName)
                ? Environment.GetEnvironmentVariable("EDITOR_AZURE_CONTAINER")
                : azureConfiguration.ContainerName) ?? "",
            LinkExpiresDays = 360
        };
        _storage = new AzureBlobStorage(
            Microsoft.Extensions.Options.Options.Create(azureBlobOptions),
            new NullLogger<AzureBlobStorage>());
    }

    [Fact]
    public async void OnlyDownloadExistingFile()
    {
        using StorageDisposableResponse<Stream> downloaded =
            await _storage.DownloadFile(PathBuilder.New(Guid.NewGuid(), new[] {"WordProcessing.docx"}));
        downloaded.Should().NotBeNull();
        downloaded.Status.Should().BeOneOf(StorageActionStatus.Success, StorageActionStatus.NotExist);
        if (downloaded.Status != StorageActionStatus.Success) return;
        downloaded.Response.Should().NotBeNull().And.BeOfType<MemoryStream>();
        downloaded.Response?.CanRead.Should().BeTrue();
        downloaded.Response?.CanSeek.Should().BeTrue();
        downloaded.Response?.Length.Should().BeGreaterThan(0);
        downloaded.Response?.Position.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task OnlyDeleteExistingFile()
    {
        StorageResponse deletedStatus =
            await _storage.RemoveFile(PathBuilder.New(Guid.NewGuid(), new[] {"WordProcessing.docx"}));
        deletedStatus.Should().NotBeNull();
        deletedStatus.Status.Should().BeOneOf(StorageActionStatus.Success, StorageActionStatus.NotExist);
    }

}