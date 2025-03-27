using AutoMapper;
using FluentAssertions;
using GroupDocs.Editor.Formats;
using GroupDocs.Editor.Metadata;
using GroupDocs.Editor.Options;
using GroupDocs.Editor.UI.Api.Controllers.RequestModels;
using GroupDocs.Editor.UI.Api.Models.DocumentConvertor;
using GroupDocs.Editor.UI.Api.Models.Editor;
using GroupDocs.Editor.UI.Api.Models.Storage;
using GroupDocs.Editor.UI.Api.Models.Storage.Responses;
using GroupDocs.Editor.UI.Api.Services.Implementation;
using GroupDocs.Editor.UI.Api.Services.Interfaces;
using GroupDocs.Editor.UI.Api.Test.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Runtime.InteropServices;

namespace GroupDocs.Editor.UI.Api.Test.Services.Implementation;

public class EditorServiceSpreadsheetTests
{
    private readonly MockRepository _mockRepository;

    private readonly Mock<IStorage> _mockStorage;
    private readonly Mock<IMetaFileStorageCache<SpreadsheetLoadOptions, SpreadsheetEditOptions>> _mockMetaFileStorageCache;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IIdGeneratorService> _mockIdGeneratorService;

    public EditorServiceSpreadsheetTests()
    {
        _mockRepository = new MockRepository(MockBehavior.Strict);

        _mockStorage = _mockRepository.Create<IStorage>();
        _mockMetaFileStorageCache = _mockRepository.Create<IMetaFileStorageCache<SpreadsheetLoadOptions, SpreadsheetEditOptions>>();
        _mockMapper = _mockRepository.Create<IMapper>();
        _mockIdGeneratorService = _mockRepository.Create<IIdGeneratorService>();
    }

    private EditorService<SpreadsheetLoadOptions, SpreadsheetEditOptions> CreateService()
    {
        return new EditorService<SpreadsheetLoadOptions, SpreadsheetEditOptions>(
            _mockStorage.Object,
            new NullLogger<EditorService<SpreadsheetLoadOptions, SpreadsheetEditOptions>>(),
            _mockMetaFileStorageCache.Object,
            _mockMapper.Object,
            _mockIdGeneratorService.Object);
    }

    [Fact]
    public async Task CreateDocument()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return;
        }
        // Arrange
        var service = CreateService();
        Guid documentCode = Guid.NewGuid();
        var docInfo = new StorageDocumentInfo
        {
            Format = SpreadsheetFormats.Xlsx,
            FamilyFormat = "Spreadsheet",
            IsEncrypted = false,
            PageCount = 1,
            Size = 1
        };
        StorageFile storageFile = new()
        { DocumentCode = documentCode, FileName = "document.Xlsx", ResourceType = ResourceType.OriginalDocument };
        StorageResponse<StorageFile> storageResponse = StorageResponse<StorageFile>.CreateSuccess(storageFile);
        CreateDocumentRequest request = new() { FileName = "document.Xlsx", Format = SpreadsheetFormats.Xlsx };
        _mockStorage.Setup(a => a.SaveFile(It.IsAny<List<FileContent>>(), It.IsAny<PathBuilder>()))
            .ReturnsAsync(new List<StorageResponse<StorageFile>> { storageResponse });
        _mockMetaFileStorageCache.Setup(a =>
                a.UpdateFiles(It.IsAny<StorageMetaFile<SpreadsheetLoadOptions, SpreadsheetEditOptions>>()))
            .ReturnsAsync(new StorageMetaFile<SpreadsheetLoadOptions, SpreadsheetEditOptions>());
        _mockIdGeneratorService.Setup(a => a.GenerateDocumentCode()).Returns(documentCode);
        _mockMapper.Setup(a => a.Map<StorageDocumentInfo>(It.IsAny<SpreadsheetDocumentInfo>())).Returns(docInfo);

        // Act
        var result = await service.CreateDocument(request);

        // Assert
        result.Should().NotBeNull();
        result?.DocumentCode.Should().Be(documentCode);
        result?.OriginalLoadOptions.Should().BeNull();
        result?.OriginalFile.Should().BeEquivalentTo(new StorageFile
        {
            DocumentCode = documentCode,
            FileName = "document.Xlsx",
            ResourceType = ResourceType.OriginalDocument
        });
        result?.DocumentInfo.Should().NotBeNull();
        result?.DocumentInfo.Should().BeEquivalentTo(docInfo);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task UploadDocument()
    {
        // Arrange
        var service = CreateService();
        Guid documentCode = Guid.NewGuid();
        await using Stream stream = TestFile.Spreadsheet.OpenFile();
        StorageFile storageFile = new()
        { DocumentCode = documentCode, FileName = TestFile.Spreadsheet.Name, ResourceType = ResourceType.OriginalDocument };
        var docInfo = new StorageDocumentInfo
        {
            Format = SpreadsheetFormats.Xlsx,
            FamilyFormat = "Spreadsheet",
            IsEncrypted = false,
            PageCount = 1,
            Size = 1
        };
        StorageResponse<StorageFile> storageResponse = StorageResponse<StorageFile>.CreateSuccess(storageFile);
        UploadDocumentRequest request = new() { FileName = TestFile.Spreadsheet.Name, Stream = stream };
        _mockMapper.Setup(a => a.Map<StorageDocumentInfo>(It.IsAny<SpreadsheetDocumentInfo>())).Returns(docInfo);
        _mockIdGeneratorService.Setup(a => a.GenerateDocumentCode()).Returns(documentCode);
        _mockStorage.Setup(a => a.SaveFile(It.IsAny<List<FileContent>>(), It.IsAny<PathBuilder>()))
            .ReturnsAsync(new List<StorageResponse<StorageFile>> { storageResponse });
        _mockMetaFileStorageCache.Setup(a =>
                a.UpdateFiles(It.IsAny<StorageMetaFile<SpreadsheetLoadOptions, SpreadsheetEditOptions>>()))
            .ReturnsAsync(new StorageMetaFile<SpreadsheetLoadOptions, SpreadsheetEditOptions>());
        // Act
        var result = await service.UploadDocument(request);

        // Assert
        result.Should().NotBeNull();
        result.DocumentCode.Should().Be(documentCode);
        result.OriginalLoadOptions.Should().BeNull();
        result.OriginalFile.Should().BeEquivalentTo(new StorageFile
        {
            DocumentCode = documentCode,
            FileName = TestFile.Spreadsheet.Name,
            ResourceType = ResourceType.OriginalDocument
        });
        result.DocumentInfo.Should().NotBeNull();
        result.DocumentInfo.Should().BeEquivalentTo(docInfo);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public void GetDocumentInfo()
    {
        // Arrange
        var service = CreateService();
        using Stream stream = TestFile.Spreadsheet.OpenFile();
        SpreadsheetLoadOptions loadOptions = new();

        // Act
        var result = service.GetDocumentInfo(stream, loadOptions);

        // Assert
        result.PageCount.Should().Be(1);
        result.Size.Should().Be(8401);
        result.Format.Should().Be(SpreadsheetFormats.Xlsx);
        result.IsEncrypted.Should().BeFalse();
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task ConvertToHtml()
    {
        // Arrange
        var service = CreateService();
        Guid documentCode = Guid.NewGuid();
        await using Stream stream = TestFile.Spreadsheet.OpenFile();
        StorageMetaFile<SpreadsheetLoadOptions, SpreadsheetEditOptions> metaFile = new()
        {
            DocumentCode = documentCode,
            DocumentInfo = new StorageDocumentInfo
            {
                Format = SpreadsheetFormats.Xlsx,
                FamilyFormat = "Spreadsheet",
                IsEncrypted = false,
                PageCount = 1,
                Size = 1
            },
            OriginalFile = new StorageFile
            {
                DocumentCode = documentCode,
                FileName = TestFile.Spreadsheet.Name,
                ResourceType = ResourceType.OriginalDocument
            },
        };
        SpreadsheetEditOptions editOptions = new() { WorksheetIndex = 0 };
        ILoadOptions loadOptions = new SpreadsheetLoadOptions();
        StorageFile storageFile = new()
        {
            DocumentCode = documentCode,
            FileName = TestFile.Spreadsheet.ChangeExtension("html"),
            ResourceType = ResourceType.HtmlContent
        };


        StorageResponse<StorageFile> storageResponse = StorageResponse<StorageFile>.CreateSuccess(storageFile);
        _mockStorage.Setup(a => a.RemoveFolder(It.IsAny<PathBuilder>()))
            .ReturnsAsync(StorageResponse.CreateSuccess());
        _mockStorage.Setup(a => a.DownloadFile(It.IsAny<PathBuilder>()))
            .ReturnsAsync(StorageDisposableResponse<Stream>.CreateSuccess(stream));

        _mockStorage.Setup(a =>
                a.SaveFile(
                    It.Is<IEnumerable<FileContent>>(contents => contents.Any(ca =>
                        ca.ResourceType == ResourceType.HtmlContent && ca.FileName.Equals(TestFile.Spreadsheet.ChangeExtension("html")))),
                    It.IsAny<PathBuilder>()))
            .ReturnsAsync(new List<StorageResponse<StorageFile>> { storageResponse });
        _mockMetaFileStorageCache.Setup(a =>
                a.UpdateFiles(It.IsAny<StorageMetaFile<SpreadsheetLoadOptions, SpreadsheetEditOptions>>()))
            .ReturnsAsync(new StorageMetaFile<SpreadsheetLoadOptions, SpreadsheetEditOptions>());
        // Act
        var result = await service.ConvertToHtml(metaFile, editOptions, loadOptions);

        // Assert
        var content = metaFile.StorageSubFiles["0"];
        content.Should().NotBeNull();
        content.Resources[storageFile.FileName].Should().Be(storageFile);
        result.Should().NotBeNull();
        result?.Length.Should().BeGreaterThan(0);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task ConvertPreviews()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return;
        }
        // Arrange
        var service = CreateService();
        Guid documentCode = Guid.NewGuid();
        await using Stream stream = TestFile.Spreadsheet.OpenFile();
        StorageMetaFile<SpreadsheetLoadOptions, SpreadsheetEditOptions> metaFile = new()
        {
            DocumentCode = documentCode,
            DocumentInfo = new StorageDocumentInfo
            {
                Format = SpreadsheetFormats.Xlsx,
                FamilyFormat = "Spreadsheet",
                IsEncrypted = false,
                PageCount = 1,
                Size = 1
            },
            OriginalFile = new StorageFile
            {
                DocumentCode = documentCode,
                FileName = TestFile.Spreadsheet.Name,
                ResourceType = ResourceType.OriginalDocument
            },
        };
        StorageFile storageFile = new()
        {
            DocumentCode = documentCode,
            FileName = "0.svg",
            ResourceType = ResourceType.Preview
        };
        StorageResponse<StorageFile> storageResponse = StorageResponse<StorageFile>.CreateSuccess(storageFile);
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(documentCode)).ReturnsAsync(metaFile);
        _mockStorage.Setup(a =>
                a.SaveFile(It.IsAny<IEnumerable<FileContent>>(),
                    It.IsAny<PathBuilder>()))
            .ReturnsAsync(new List<StorageResponse<StorageFile>> { storageResponse });
        _mockStorage.Setup(a => a.DownloadFile(It.IsAny<PathBuilder>()))
            .ReturnsAsync(StorageDisposableResponse<Stream>.CreateSuccess(stream));
        _mockMetaFileStorageCache.Setup(a =>
                a.UpdateFiles(It.IsAny<StorageMetaFile<SpreadsheetLoadOptions, SpreadsheetEditOptions>>()))
            .ReturnsAsync(new StorageMetaFile<SpreadsheetLoadOptions, SpreadsheetEditOptions>());

        // Act
        var result = await service.ConvertPreviews(documentCode);

        // Assert
        result.Should().NotBeNull();
        result?.PreviewImages.Should().HaveCount(1);
        result?.PreviewImages["0"].Should().Be(storageFile);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task SaveToPdf()
    {
        // Arrange
        var service = CreateService();
        Guid documentCode = Guid.NewGuid();

        PdfSaveOptions saveOptions = new();
        DownloadPdfRequest request = new() { DocumentCode = documentCode, SaveOptions = saveOptions };
        await using Stream stream = TestFile.Spreadsheet.OpenFile();
        StorageMetaFile<SpreadsheetLoadOptions, SpreadsheetEditOptions> metaFile = new()
        {
            DocumentCode = documentCode,
            DocumentInfo = new StorageDocumentInfo
            {
                Format = SpreadsheetFormats.Xlsx,
                FamilyFormat = "Spreadsheet",
                IsEncrypted = false,
                PageCount = 1,
                Size = 1
            },
            OriginalFile = new StorageFile
            {
                DocumentCode = documentCode,
                FileName = TestFile.Spreadsheet.Name,
                ResourceType = ResourceType.OriginalDocument
            },
        };
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(documentCode)).ReturnsAsync(metaFile);
        _mockStorage.Setup(a => a.DownloadFile(It.IsAny<PathBuilder>()))
            .ReturnsAsync(StorageDisposableResponse<Stream>.CreateSuccess(stream));

        // Act
        using var result = await service.SaveToPdf(request);

        // Assert
        result.Should().NotBeNull();
        result?.FileName.Should().Be(TestFile.Spreadsheet.ChangeExtension("pdf"));
        result?.ResourceStream.Length.Should().BeGreaterThan(0);
        result?.ResourceStream.CanRead.Should().BeTrue();
        result?.ResourceStream.Position.Should().Be(0);
        result?.ResourceType.Should().Be(ResourceType.ConvertedDocument);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task ConvertToDocument()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return;
        }
        // Arrange
        var service = CreateService();
        Guid documentCode = Guid.NewGuid();
        SpreadsheetSaveOptions saveOptions = new(SpreadsheetFormats.Xls);
        DownloadDocumentRequest request = new() { DocumentCode = documentCode, Format = "Xls", SaveOptions = saveOptions };
        await using Stream stream = TestFile.Spreadsheet.OpenFile();
        StorageMetaFile<SpreadsheetLoadOptions, SpreadsheetEditOptions> metaFile = new()
        {
            DocumentCode = documentCode,
            DocumentInfo = new StorageDocumentInfo
            {
                Format = SpreadsheetFormats.Xlsx,
                FamilyFormat = "Spreadsheet",
                IsEncrypted = false,
                PageCount = 1,
                Size = 1
            },
            OriginalFile = new StorageFile
            {
                DocumentCode = documentCode,
                FileName = TestFile.Spreadsheet.Name,
                ResourceType = ResourceType.OriginalDocument
            },
        };
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(documentCode)).ReturnsAsync(metaFile);
        _mockStorage.Setup(a => a.DownloadFile(It.IsAny<PathBuilder>()))
            .ReturnsAsync(StorageDisposableResponse<Stream>.CreateSuccess(stream));

        // Act
        using var result = await service.ConvertToDocument(request);

        // Assert
        result.Should().NotBeNull();
        result?.FileName.Should().Be(TestFile.Spreadsheet.ChangeExtension("Xls"));
        result?.ResourceStream.Length.Should().BeGreaterThan(0);
        result?.ResourceStream.CanRead.Should().BeTrue();
        result?.ResourceStream.Position.Should().Be(0);
        result?.ResourceType.Should().Be(ResourceType.ConvertedDocument);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public void GetSupportedFormats()
    {
        // Arrange
        var service = CreateService();
        var expected = SpreadsheetFormats.All.GroupBy(a => a.Extension).Select(a => a.First()).ToList();
        // Act
        var result = service.GetSupportedFormats<SpreadsheetFormats>();

        // Assert
        var spreadsheetFormats = result.ToList();
        spreadsheetFormats.Should().NotBeNull();
        spreadsheetFormats.Should().BeEquivalentTo(expected);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task UpdateHtmlContent()
    {
        // Arrange
        var service = CreateService();
        Guid documentCode = Guid.NewGuid();
        StorageSubFile<SpreadsheetEditOptions> currentContent = new(TestFile.Spreadsheet.Name, "0") { DocumentCode = documentCode };
        const string htmlContents = "new content";
        StorageFile storageFile = new()
        {
            DocumentCode = documentCode,
            FileName = TestFile.Spreadsheet.ChangeExtension("html"),
            ResourceType = ResourceType.HtmlContent
        };
        StorageResponse<StorageFile> storageResponse = StorageResponse<StorageFile>.CreateSuccess(storageFile);
        _mockStorage
            .Setup(a => a.RemoveFile(It.IsAny<PathBuilder>()))
            .ReturnsAsync(StorageResponse.CreateSuccess());
        _mockStorage.Setup(a =>
                a.SaveFile(It.IsAny<IEnumerable<FileContent>>(),
                    It.IsAny<PathBuilder>()))
            .ReturnsAsync(new List<StorageResponse<StorageFile>> { storageResponse });
        // Act
        var result = await service.UpdateHtmlContent(currentContent, htmlContents);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(StorageActionStatus.Success);
        result.Response.Should().NotBeNull();
        result.Response?.IsEdited.Should().BeTrue();
        result.Response?.Resources[storageFile.FileName].Should().Be(storageFile);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task UpdateResource()
    {
        // Arrange
        var service = CreateService();
        Guid documentCode = Guid.NewGuid();
        const string fileName = "new.css";
        await using var stream = new MemoryStream();

        IFormFile formFile = new FormFile(stream, 0, stream.Length, "id_from_form", fileName);
        StorageSubFile<SpreadsheetEditOptions> currentContent = new(TestFile.Spreadsheet.Name, "0") { DocumentCode = documentCode };
        UploadResourceRequest resource = new() { DocumentCode = documentCode, ResourceType = ResourceType.Stylesheet, File = formFile };
        StorageFile storageFile = new()
        {
            DocumentCode = documentCode,
            FileName = "new.css",
            ResourceType = ResourceType.HtmlContent
        };
        StorageResponse<StorageFile> storageResponse = StorageResponse<StorageFile>.CreateSuccess(storageFile);
        _mockStorage.Setup(a =>
                a.SaveFile(It.IsAny<IEnumerable<FileContent>>(),
                    It.IsAny<PathBuilder>()))
            .ReturnsAsync(new List<StorageResponse<StorageFile>> { storageResponse });
        // Act
        var result = await service.UpdateResource(currentContent, resource);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(StorageActionStatus.Success);
        result.Response.Should().NotBeNull();
        result.Response?.Resources[storageFile.FileName].Should().Be(storageFile);
        _mockRepository.VerifyAll();
    }
}