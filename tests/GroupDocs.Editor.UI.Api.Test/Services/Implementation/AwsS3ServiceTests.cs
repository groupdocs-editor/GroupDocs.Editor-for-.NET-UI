using FluentAssertions;
using GroupDocs.Editor.UI.Api.Models.Storage.Responses;
using GroupDocs.Editor.UI.Api.Services.Implementation;
using GroupDocs.Editor.UI.Api.Services.Options;
using GroupDocs.Editor.UI.Api.Test.SetupApp;
using Microsoft.Extensions.Logging.Abstractions;

namespace GroupDocs.Editor.UI.Api.Test.Services.Implementation;

public class AwsS3ServiceTests : IDisposable
{
    private readonly AwsS3Storage _storage;

    public AwsS3ServiceTests()
    {
        var awsConfiguration = TestConfigHelper.IConfiguration().BuildAwsTestOption();
        AwsOptions options = new()
        {
            Bucket = (string.IsNullOrWhiteSpace(awsConfiguration.Bucket) ? Environment.GetEnvironmentVariable("EDITOR_AWS_BUCKET") : awsConfiguration.Bucket) ?? "",
            AccessKey = (string.IsNullOrWhiteSpace(awsConfiguration.AccessKey) ? Environment.GetEnvironmentVariable("EDITOR_AWS_KEY") : awsConfiguration.AccessKey) ?? "",
            LinkExpiresDays = 1,
            Profile = "",
            Region = (string.IsNullOrWhiteSpace(awsConfiguration.Region) ? Environment.GetEnvironmentVariable("EDITOR_AWS_REGION") : awsConfiguration.Region) ?? "",
            RootFolderName = "groupdocseditorui",
            SecretKey = (string.IsNullOrWhiteSpace(awsConfiguration.SecretKey) ? Environment.GetEnvironmentVariable("EDITOR_AWS_SECRETKEY") : awsConfiguration.SecretKey) ?? ""
        };
        _storage = new AwsS3Storage(
            new NullLogger<AwsS3Storage>(),
            Microsoft.Extensions.Options.Options.Create(options)
        );
    }

    [Fact]
    public async Task RemoveNotExisting()
    {
        StorageResponse deletionNotExistantFolderResult = await _storage.RemoveFolder("Abcde_notExists");
        deletionNotExistantFolderResult.Should().NotBeNull();
        deletionNotExistantFolderResult.IsSuccess.Should().BeFalse();
        deletionNotExistantFolderResult.Status.Should().Be(StorageActionStatus.NotExist);

        StorageResponse detetionNotExistantFileResult = await _storage.RemoveFile("Abcd_NotExistantFile");
        detetionNotExistantFileResult.Should().NotBeNull();
        detetionNotExistantFileResult.IsSuccess.Should().BeFalse();
        detetionNotExistantFileResult.Status.Should().Be(StorageActionStatus.NotExist);
    }

    [Fact]
    public async void DownloadNotExistantFile()
    {
        const string filename = "Abcdef_NotExistantFilename";
        using StorageDisposableResponse<Stream> downloaded = await _storage.DownloadFile(filename);
        downloaded.Should().NotBeNull();
        downloaded.IsSuccess.Should().BeFalse();
        downloaded.Status.Should().Be(StorageActionStatus.NotExist);
        downloaded.Response.Should().NotBeNull();
        downloaded.Response.Should().BeSameAs(Stream.Null);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;
        _storage.Dispose();
    }
}