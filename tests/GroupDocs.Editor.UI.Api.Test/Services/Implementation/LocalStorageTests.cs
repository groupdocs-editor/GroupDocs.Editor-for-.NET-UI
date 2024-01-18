using FluentAssertions;
using GroupDocs.Editor.UI.Api.Models.DocumentConvertor;
using GroupDocs.Editor.UI.Api.Services.Implementation;
using GroupDocs.Editor.UI.Api.Services.Interfaces;
using GroupDocs.Editor.UI.Api.Services.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace GroupDocs.Editor.UI.Api.Test.Services.Implementation;

public class LocalStorageTests : IDisposable
{
    private readonly MockRepository _mockRepository;
    private readonly IOptions<LocalStorageOptions> _mockOptions;
    private readonly Mock<IIdGeneratorService> _mockIdGeneratorService;

    public LocalStorageTests()
    {
        _mockRepository = new MockRepository(MockBehavior.Strict);
        LocalStorageOptions opt = new()
        {
            BaseUrl = @"https://localhost:7240/LocalFile/download/",
            RootFolder = "files",
        };
        _mockOptions = Microsoft.Extensions.Options.Options.Create(opt);
        _mockIdGeneratorService = _mockRepository.Create<IIdGeneratorService>();
    }

    private LocalStorage CreateLocalStorage()
    {
        Directory.CreateDirectory("files");
        return new LocalStorage(
            new NullLogger<LocalStorage>(),
            _mockOptions,
            _mockIdGeneratorService.Object);
    }

    [Fact]
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
        Guid documentCode = Guid.NewGuid();

        // Act
        var result = await localStorage.SaveFile(fileContents, documentCode);

        // Assert
        result.Should().HaveCount(1);
        File.Exists(Path.Combine(_mockOptions.Value.RootFolder, documentCode.ToString(), "test.docx")).Should().BeTrue();
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task RemoveFolder()
    {
        // Arrange
        var localStorage = CreateLocalStorage();
        Directory.CreateDirectory(Path.Combine("files", "toDelete"));
        const string folderSubPath = "toDelete";

        // Act
        var result = await localStorage.RemoveFolder(folderSubPath);

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
        var path = Path.Combine("files", "toDelete.txt");
        await File.WriteAllTextAsync(path, "test");

        // Act
        var result = await localStorage.RemoveFile("toDelete.txt");

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
        var path = Path.Combine("files", "toDelete.txt");
        await File.WriteAllTextAsync(path, "test");

        // Act
        using var result = await localStorage.DownloadFile("toDelete.txt");

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
        var path = Path.Combine("files", "toDelete.txt");
        await File.WriteAllTextAsync(path, "test");

        // Act
        var response = await localStorage.GetFileText("toDelete.txt");

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