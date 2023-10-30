using FluentAssertions;
using GroupDocs.Editor.Formats;
using GroupDocs.Editor.UI.Api.Models.DocumentConvertor;
using GroupDocs.Editor.UI.Api.Models.Storage;
using GroupDocs.Editor.UI.Api.Models.Storage.Requests;
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
        if (!Directory.Exists(_mockOptions.Value.RootFolder))
        {
            Directory.CreateDirectory(_mockOptions.Value.RootFolder);
        }
    }

    private LocalStorage CreateLocalStorage()
    {
        return new LocalStorage(
            new NullLogger<LocalStorage>(),
            _mockOptions,
            _mockIdGeneratorService.Object);
    }

    [Fact]
    public async Task UploadOriginalFiles()
    {
        // Arrange
        var localStorage = CreateLocalStorage();
        using FileContent file = new();
        file.FileName = "WordProcessing.docx";
        file.ResourceStream = new MemoryStream();
        StorageDocumentInfo info = new()
        { Format = WordProcessingFormats.Docx, IsEncrypted = false, PageCount = 2, Size = 2222 };

        IEnumerable<UploadOriginalRequest> files = new List<UploadOriginalRequest>
        {
            new() {DocumentInfo = info, FileContent = file}
        };
        Guid code = Guid.NewGuid();
        _mockIdGeneratorService.Setup(a => a.GenerateDocumentCode()).Returns(code);

        // Act
        var result = await localStorage.UploadFiles(files);

        // Assert
        result.Should().HaveCount(1);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task SaveFile()
    {
        // Arrange
        var localStorage = CreateLocalStorage();
        using FileContent file = new();
        file.FileName = "test.docx";
        file.ResourceStream = new MemoryStream();
        IEnumerable<FileContent> fileContents = new List<FileContent> { file };
        Guid documentCode = Guid.NewGuid();

        // Act
        var result = await localStorage.SaveFile(
            fileContents,
            documentCode);

        // Assert
        result.Should().HaveCount(1);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public void RemoveFolder()
    {
        // Arrange
        var localStorage = CreateLocalStorage();
        Directory.CreateDirectory(Path.Combine("files", "toDelete"));
        const string folderSubPath = "toDelete";

        // Act
        var result = localStorage.RemoveFolder(folderSubPath);

        // Assert
        result.Status.Should().Be(StorageActionStatus.Success);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public void RemoveFile()
    {
        // Arrange
        var localStorage = CreateLocalStorage();
        var path = Path.Combine("files", "toDelete.txt");
        File.WriteAllText(path, "test");

        // Act
        var result = localStorage.RemoveFile("toDelete.txt");

        // Assert
        result.Status.Should().Be(StorageActionStatus.Success);
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
        var response = await localStorage.DownloadFile("toDelete.txt");

        // Assert
        response.Should().NotBeNull();
        response.Status.Should().Be(StorageActionStatus.Success);
        response.Response.Should().NotBeNull();
        await response.Response.DisposeAsync();
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

    [Fact]
    public async Task UpdateHtmlContent()
    {
        // Arrange
        var localStorage = CreateLocalStorage();
        Guid documentCode = Guid.NewGuid();
        StorageSubFile currentContent = new() { DocumentCode = documentCode, SubCode = 0, SourceDocumentName = "text.docx" };
        const string htmlContents = "test";

        // Act
        var response = await localStorage.UpdateHtmlContent(currentContent, htmlContents);
        // Assert
        response.Should().NotBeNull();
        response.Status.Should().Be(StorageActionStatus.Success);
        response.Response.Should().NotBeNull();
        response.Response.IsEdited.Should().BeTrue();
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