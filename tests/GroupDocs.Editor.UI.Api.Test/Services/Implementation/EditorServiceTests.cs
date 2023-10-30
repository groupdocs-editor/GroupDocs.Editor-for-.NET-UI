using AutoMapper;
using FluentAssertions;
using GroupDocs.Editor.Formats;
using GroupDocs.Editor.Metadata;
using GroupDocs.Editor.Options;
using GroupDocs.Editor.UI.Api.Models.DocumentConvertor;
using GroupDocs.Editor.UI.Api.Models.Editor;
using GroupDocs.Editor.UI.Api.Models.Storage;
using GroupDocs.Editor.UI.Api.Models.Storage.Requests;
using GroupDocs.Editor.UI.Api.Models.Storage.Responses;
using GroupDocs.Editor.UI.Api.Services.Implementation;
using GroupDocs.Editor.UI.Api.Services.Interfaces;
using GroupDocs.Editor.UI.Api.Test.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text.Json;

namespace GroupDocs.Editor.UI.Api.Test.Services.Implementation;

public class EditorServiceTests
{
    private readonly MockRepository _mockRepository;

    private readonly Mock<IStorage> _mockStorage;
    private readonly Mock<IMetaFileStorageCache> _mockMetaFileStorageCache;
    private readonly Mock<IMapper> _mockMapper;

    public EditorServiceTests()
    {
        _mockRepository = new MockRepository(MockBehavior.Strict);
        _mockStorage = _mockRepository.Create<IStorage>();
        _mockMetaFileStorageCache = _mockRepository.Create<IMetaFileStorageCache>();
        _mockMapper = _mockRepository.Create<IMapper>();
    }

    private EditorService CreateService()
    {
        return new EditorService(
            _mockStorage.Object,
            new NullLogger<EditorService>(),
            _mockMetaFileStorageCache.Object,
            _mockMapper.Object);
    }

    [Fact]
    public void GetDocumentInfo()
    {
        // Arrange
        var service = CreateService();
        using Stream stream = TestFile.WordProcessing.OpenFile();
        ILoadOptions loadOptions = new WordProcessingLoadOptions();

        // Act
        var result = service.GetDocumentInfo(stream, loadOptions);

        // Assert
        result.PageCount.Should().Be(1);
        result.Size.Should().Be(2911864);
        result.Format.Should().Be(WordProcessingFormats.Docx);
        result.IsEncrypted.Should().BeFalse();
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task SaveDocument()
    {
        // Arrange
        var service = CreateService();
        await using Stream stream = TestFile.WordProcessing.OpenFile();
        ILoadOptions loadOptions = new WordProcessingLoadOptions();
        Guid code = Guid.NewGuid();
        StorageDocumentInfo storageDocumentInfo = new()
        {
            Format = WordProcessingFormats.Docx,
            IsEncrypted = false,
            PageCount = 1,
            Size = 2911864
        };
        WordProcessingEditOptions editOptions = new(true)
        {
            FontExtraction = FontExtractionOptions.ExtractAll,
            ExtractOnlyUsedFont = true
        };

        StorageMetaFile expected = new()
        {
            DocumentCode = code,
            DocumentInfo = storageDocumentInfo,
            OriginalFile = new StorageFile
            {
                DocumentCode = code,
                FileName = TestFile.WordProcessing.GetFileName(),
                FileLink = ""
            }
        };
        StorageFile newFile = new()
        { DocumentCode = code, FileName = TestFile.WordProcessing.GetFileName(), FileLink = "http://ss.com" };
        _mockStorage.Setup(a => a.UploadFiles(It.IsAny<List<UploadOriginalRequest>>())).ReturnsAsync(
            new List<StorageResponse<StorageMetaFile>> { StorageResponse<StorageMetaFile>.CreateSuccess(expected) });
        await using Stream streamStorageMetaFile = new MemoryStream();
        await JsonSerializer.SerializeAsync(streamStorageMetaFile, expected);

        _mockStorage.Setup(a => a.SaveFile(It.IsAny<IEnumerable<FileContent>>(), code, "0"))
            .ReturnsAsync(new List<StorageResponse<StorageFile>> { StorageResponse<StorageFile>.CreateSuccess(newFile) });
        _mockMapper.Setup(a => a.Map<StorageDocumentInfo>(It.IsAny<IDocumentInfo>())).Returns(storageDocumentInfo);
        _mockMetaFileStorageCache.Setup(a => a.UpdateFiles(It.IsAny<StorageMetaFile>())).ReturnsAsync(expected);
        // Act
        var result = await service.SaveDocument(new SaveDocumentRequest
        { EditOptions = editOptions, FileName = TestFile.WordProcessing.GetFileName(), LoadOptions = loadOptions, Stream = stream });

        // Assert
        result.Should().Be(expected);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task ConvertToHtml()
    {
        // Arrange
        var service = CreateService();
        Guid documentCode = Guid.NewGuid();
        ILoadOptions loadOptions = new WordProcessingLoadOptions();
        WordProcessingEditOptions editOptions = new(true)
        {
            FontExtraction = FontExtractionOptions.ExtractAll,
            ExtractOnlyUsedFont = true
        };
        await using Stream stream = TestFile.WordProcessing.OpenFile();
        StorageDocumentInfo storageDocumentInfo = new()
        {
            Format = WordProcessingFormats.Docx,
            IsEncrypted = false,
            PageCount = 1,
            Size = 2911864
        };

        StorageMetaFile expected = new()
        {
            DocumentCode = documentCode,
            DocumentInfo = storageDocumentInfo,
            OriginalFile = new StorageFile
            {
                DocumentCode = documentCode,
                FileName = TestFile.WordProcessing.GetFileName(),
                FileLink = ""
            }
        };
        StorageFile newFile = new()
        { DocumentCode = documentCode, FileName = TestFile.WordProcessing.GetFileName(), FileLink = "http://ss.com" };
        _mockStorage.Setup(a => a.DownloadFile(Path.Combine(documentCode.ToString(), TestFile.WordProcessing.GetFileName())))
            .ReturnsAsync(StorageDisposableResponse<Stream>.CreateSuccess(stream));
        _mockStorage.Setup(a => a.SaveFile(It.IsAny<IEnumerable<FileContent>>(), documentCode, "0"))
            .ReturnsAsync(new List<StorageResponse<StorageFile>> { StorageResponse<StorageFile>.CreateFailed(newFile) });
        _mockMetaFileStorageCache.Setup(a => a.UpdateFiles(It.IsAny<StorageMetaFile>())).ReturnsAsync(expected);
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(documentCode)).ReturnsAsync(expected);
        // Act
        var result = await service.ConvertToHtml(documentCode, editOptions, loadOptions);

        // Assert
        result.Should().Be(expected);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task ConvertPreviews()
    {
        // Arrange
        var service = CreateService();
        Guid documentCode = Guid.NewGuid();
        await using Stream stream = TestFile.WordProcessing.OpenFile();
        StorageDocumentInfo storageDocumentInfo = new()
        {
            Format = WordProcessingFormats.Docx,
            IsEncrypted = false,
            PageCount = 1,
            Size = 2911864
        };
        StorageMetaFile expected = new()
        {
            DocumentCode = documentCode,
            DocumentInfo = storageDocumentInfo,
            OriginalFile = new StorageFile
            {
                DocumentCode = documentCode,
                FileName = TestFile.WordProcessing.GetFileName(),
                FileLink = ""
            }
        };
        StorageFile newFile = new()
        { DocumentCode = documentCode, FileName = "preview.swg", FileLink = "http://ss.com" };
        _mockStorage.Setup(a => a.DownloadFile(Path.Combine(documentCode.ToString(), TestFile.WordProcessing.GetFileName())))
            .ReturnsAsync(StorageDisposableResponse<Stream>.CreateSuccess(stream));
        ILoadOptions loadOptions = new WordProcessingLoadOptions();
        _mockMetaFileStorageCache.Setup(a => a.UpdateFiles(It.IsAny<StorageMetaFile>())).ReturnsAsync(expected);
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(documentCode)).ReturnsAsync(expected);
        _mockStorage.Setup(a => a.SaveFile(It.IsAny<IEnumerable<FileContent>>(), documentCode, "preview"))
            .ReturnsAsync(new List<StorageResponse<StorageFile>> { StorageResponse<StorageFile>.CreateSuccess(newFile) });
        // Act
        var result = await service.ConvertPreviews(
            documentCode,
            loadOptions);

        // Assert
        result.Should().NotBeNull();
        result?.PreviewImages.Should().NotBeNull();
        result?.PreviewImages.Keys.Should().HaveCount(1);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task SaveToPdf()
    {
        // Arrange
        var service = CreateService();
        Guid documentCode = Guid.NewGuid();
        await using Stream stream = TestFile.WordProcessing.OpenFile();
        ILoadOptions loadOptions = new WordProcessingLoadOptions();
        PdfSaveOptions pdfSaveOptions = new() { OptimizeMemoryUsage = true };
        StorageDocumentInfo storageDocumentInfo = new()
        {
            Format = WordProcessingFormats.Docx,
            IsEncrypted = false,
            PageCount = 1,
            Size = 2911864
        };
        StorageMetaFile metaFile = new()
        {
            DocumentCode = documentCode,
            DocumentInfo = storageDocumentInfo,
            OriginalFile = new StorageFile
            {
                DocumentCode = documentCode,
                FileName = TestFile.WordProcessing.GetFileName(),
                FileLink = ""
            }
        };
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(documentCode)).ReturnsAsync(metaFile);
        _mockStorage.Setup(a => a.DownloadFile(Path.Combine(documentCode.ToString(), TestFile.WordProcessing.GetFileName())))
            .ReturnsAsync(StorageDisposableResponse<Stream>.CreateSuccess(stream));
        // Act
        using var result = await service.SaveToPdf(new DownloadPdfRequest()
        {
            DocumentCode = documentCode,
            LoadOptions = loadOptions,
            SaveOptions = pdfSaveOptions
        });

        // Assert
        result.Should().NotBeNull();
        result?.FileName.Should().Be(TestFile.WordProcessing.ChangeExtension("pdf"));
        result?.ResourceStream.Should().NotBeNull();
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task ConvertToDocument()
    {
        // Arrange
        var service = CreateService();
        Guid documentCode = Guid.NewGuid();
        await using Stream stream = TestFile.WordProcessing.OpenFile();
        DownloadDocumentRequest request = new()
        {
            DocumentCode = documentCode,
            Format = "doc",
            LoadOptions = new WordProcessingLoadOptions(),
            SaveOptions = new WordProcessingSaveOptions(WordProcessingFormats.Doc) { EnablePagination = true, OptimizeMemoryUsage = true }
        };
        StorageDocumentInfo storageDocumentInfo = new()
        {
            Format = WordProcessingFormats.Docx,
            IsEncrypted = false,
            PageCount = 1,
            Size = 2911864
        };
        StorageMetaFile metaFile = new()
        {
            DocumentCode = documentCode,
            DocumentInfo = storageDocumentInfo,
            OriginalFile = new StorageFile
            {
                DocumentCode = documentCode,
                FileName = TestFile.WordProcessing.GetFileName(),
                FileLink = ""
            }
        };
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(documentCode)).ReturnsAsync(metaFile);
        _mockStorage.Setup(a => a.DownloadFile(Path.Combine(documentCode.ToString(), TestFile.WordProcessing.GetFileName())))
            .ReturnsAsync(StorageDisposableResponse<Stream>.CreateSuccess(stream));
        // Act
        using var result = await service.ConvertToDocument(request);

        // Assert
        result.Should().NotBeNull();
        result?.FileName.Should().Be(TestFile.WordProcessing.ChangeExtension("doc"));
        result?.ResourceStream.Should().NotBeNull();
        _mockRepository.VerifyAll();
    }
}