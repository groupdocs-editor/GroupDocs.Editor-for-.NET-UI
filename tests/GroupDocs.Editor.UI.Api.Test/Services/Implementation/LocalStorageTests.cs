using FluentAssertions;
using GroupDocs.Editor.UI.Api.Models.DocumentConvertor;
using GroupDocs.Editor.UI.Api.Models.Storage;
using GroupDocs.Editor.UI.Api.Services.Implementation;
using GroupDocs.Editor.UI.Api.Services.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace GroupDocs.Editor.UI.Api.Test.Services.Implementation;

public class LocalStorageTests : IDisposable
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
    private readonly MockRepository _mockRepository;
    private readonly IOptions<LocalStorageOptions> _mockOptions;
    private readonly Guid _documentCode;

    public LocalStorageTests()
    {
        _mockRepository = new MockRepository(MockBehavior.Strict);
        _httpContextAccessor = _mockRepository.Create<IHttpContextAccessor>();
        _documentCode = Guid.NewGuid();
        LocalStorageOptions opt = new()
        {
            BaseUrl = @"https://localhost:7240/LocalFile/download/",
            RootFolder = "files",
        };
        _mockOptions = Microsoft.Extensions.Options.Options.Create(opt);
    }

    private LocalStorage CreateLocalStorage()
    {
        Directory.CreateDirectory(Path.Combine("files", _documentCode.ToString()));
        return new LocalStorage(
            new NullLogger<LocalStorage>(),
            _mockOptions, _httpContextAccessor.Object);
    }

    [Fact(Skip = "stop processing")]
    public async Task SaveFile()
    {
        // Arrange
        var localStorage = CreateLocalStorage();
        await using Stream memoryStream = new MemoryStream();
        using FileContent file = new()
        {
            FileName = "test.docx",
            ResourceStream = memoryStream
        };

        IEnumerable<FileContent> fileContents = new List<FileContent> { file };

        // Act
        var result = await localStorage.SaveFile(fileContents, PathBuilder.New(_documentCode));

        // Assert
        result.Should().HaveCount(1);
        File.Exists(Path.Combine(_mockOptions.Value.RootFolder, _documentCode.ToString(), "test.docx")).Should().BeTrue();
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task RemoveFolder()
    {
        // Arrange
        var localStorage = CreateLocalStorage();
        Directory.CreateDirectory(Path.Combine("files", _documentCode.ToString(), "toDelete"));
        const string folderSubPath = "toDelete";

        // Act
        var result = await localStorage.RemoveFolder(PathBuilder.New(_documentCode, new[] { folderSubPath }));

        // Assert
        result.Status.Should().Be(StorageActionStatus.Success);
        Directory.Exists(Path.Combine("files", "toDelete")).Should().BeFalse();
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task RemoveFile()
    {
        // Arrange
        var localStorage = CreateLocalStorage();

        var path = Path.Combine("files", _documentCode.ToString(), "toDelete.txt");
        await File.WriteAllTextAsync(path, "test");

        // Act
        var result = await localStorage.RemoveFile(PathBuilder.New(_documentCode, new[] { "toDelete.txt" }));

        // Assert
        result.Status.Should().Be(StorageActionStatus.Success);
        File.Exists(path).Should().BeFalse();
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task DownloadFile()
    {
        // Arrange
        var localStorage = CreateLocalStorage();
        var path = Path.Combine("files", _documentCode.ToString(), "toDelete.txt");
        await File.WriteAllTextAsync(path, "test");

        // Act
        using var result = await localStorage.DownloadFile(PathBuilder.New(_documentCode, new[] { "toDelete.txt" }));

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(StorageActionStatus.Success);
        result.Response.Should().NotBeNull();
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task GetFileText()
    {
        // Arrange
        var localStorage = CreateLocalStorage();
        var path = Path.Combine("files", _documentCode.ToString(), "toDelete.txt");
        await File.WriteAllTextAsync(path, "test");

        // Act
        var response = await localStorage.GetFileText(PathBuilder.New(_documentCode, new[] { "toDelete.txt" }));

        // Assert
        response.Status.Should().Be(StorageActionStatus.Success);
        response.Response.Should().Be("test");
        _mockRepository.VerifyAll();
    }
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && Directory.Exists(_mockOptions.Value.RootFolder))
        {
            Directory.Delete(_mockOptions.Value.RootFolder, disposing);
        }
    }
}