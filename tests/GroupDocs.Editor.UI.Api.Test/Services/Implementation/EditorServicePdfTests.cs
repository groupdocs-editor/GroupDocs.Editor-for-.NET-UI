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

public class EditorServicePdfTests
{
    private readonly MockRepository _mockRepository;

    private readonly Mock<IStorage> _mockStorage;
    private readonly Mock<IMetaFileStorageCache<PdfLoadOptions, PdfEditOptions>> _mockMetaFileStorageCache;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IIdGeneratorService> _mockIdGeneratorService;

    public EditorServicePdfTests()
    {
        _mockRepository = new MockRepository(MockBehavior.Strict);

        _mockStorage = _mockRepository.Create<IStorage>();
        _mockMetaFileStorageCache = _mockRepository.Create<IMetaFileStorageCache<PdfLoadOptions, PdfEditOptions>>();
        _mockMapper = _mockRepository.Create<IMapper>();
        _mockIdGeneratorService = _mockRepository.Create<IIdGeneratorService>();
    }

    private EditorService<PdfLoadOptions, PdfEditOptions> CreateService()
    {
        return new EditorService<PdfLoadOptions, PdfEditOptions>(
            _mockStorage.Object,
            new NullLogger<EditorService<PdfLoadOptions, PdfEditOptions>>(),
            _mockMetaFileStorageCache.Object,
            _mockMapper.Object,
            _mockIdGeneratorService.Object);
    }

    [Fact]
    public async Task UploadDocument()
    {
        // Arrange
        var service = CreateService();
        Guid documentCode = Guid.NewGuid();
        await using Stream stream = TestFile.Pdf.OpenFile();
        StorageFile storageFile = new()
        { DocumentCode = documentCode, FileName = TestFile.Pdf.Name, ResourceType = ResourceType.OriginalDocument };
        var docInfo = new StorageDocumentInfo
        {
            Format = FixedLayoutFormats.Pdf,
            FamilyFormat = "FixedLayout",
            IsEncrypted = false,
            PageCount = 1,
            Size = 1
        };
        StorageResponse<StorageFile> storageResponse = StorageResponse<StorageFile>.CreateSuccess(storageFile);
        UploadDocumentRequest request = new() { FileName = TestFile.Pdf.Name, Stream = stream };
        _mockMapper.Setup(a => a.Map<StorageDocumentInfo>(It.IsAny<FixedLayoutDocumentInfo>())).Returns(docInfo);
        _mockIdGeneratorService.Setup(a => a.GenerateDocumentCode()).Returns(documentCode);
        _mockStorage.Setup(a => a.SaveFile(It.IsAny<List<FileContent>>(), It.IsAny<PathBuilder>()))
            .ReturnsAsync(new List<StorageResponse<StorageFile>> { storageResponse });
        _mockMetaFileStorageCache.Setup(a =>
                a.UpdateFiles(It.IsAny<StorageMetaFile<PdfLoadOptions, PdfEditOptions>>()))
            .ReturnsAsync(new StorageMetaFile<PdfLoadOptions, PdfEditOptions>());
        // Act
        var result = await service.UploadDocument(request);

        // Assert
        result.Should().NotBeNull();
        result?.DocumentCode.Should().Be(documentCode);
        result?.OriginalLoadOptions.Should().BeNull();
        result?.OriginalFile.Should().BeEquivalentTo(new StorageFile
        {
            DocumentCode = documentCode,
            FileName = TestFile.Pdf.Name,
            ResourceType = ResourceType.OriginalDocument
        });
        result?.DocumentInfo.Should().NotBeNull();
        result?.DocumentInfo.Should().BeEquivalentTo(docInfo);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public void GetDocumentInfo()
    {
        // Arrange
        var service = CreateService();
        using Stream stream = TestFile.Pdf.OpenFile();
        PdfLoadOptions loadOptions = new();

        // Act
        var result = service.GetDocumentInfo(stream, loadOptions);

        // Assert
        result.PageCount.Should().Be(1);
        result.Size.Should().Be(10406L);
        result.Format.Should().Be(FixedLayoutFormats.Pdf);
        result.IsEncrypted.Should().BeFalse();
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task ConvertToHtml()
    {
        // Arrange
        var service = CreateService();
        Guid documentCode = Guid.NewGuid();
        await using Stream stream = TestFile.Pdf.OpenFile();
        StorageMetaFile<PdfLoadOptions, PdfEditOptions> metaFile = new()
        {
            DocumentCode = documentCode,
            DocumentInfo = new StorageDocumentInfo
            {
                Format = FixedLayoutFormats.Pdf,
                FamilyFormat = "FixedLayout",
                IsEncrypted = false,
                PageCount = 1,
                Size = 1
            },
            OriginalFile = new StorageFile
            {
                DocumentCode = documentCode,
                FileName = TestFile.Pdf.Name,
                ResourceType = ResourceType.OriginalDocument
            },
        };
        PdfEditOptions editOptions = new() { EnablePagination = true };
        ILoadOptions loadOptions = new PdfLoadOptions();
        StorageFile storageFile = new()
        {
            DocumentCode = documentCode,
            FileName = TestFile.Pdf.ChangeExtension("html"),
            ResourceType = ResourceType.HtmlContent
        };
        StorageFile storageStyle = new()
        {
            DocumentCode = documentCode,
            FileName = "style.css",
            ResourceType = ResourceType.HtmlContent
        };
        StorageFile storageStylePaginal = new()
        {
            DocumentCode = documentCode,
            FileName = "PaginalStyles.css",
            ResourceType = ResourceType.HtmlContent
        };
        StorageResponse<StorageFile> storageResponse = StorageResponse<StorageFile>.CreateSuccess(storageFile);
        StorageResponse<StorageFile> styleStorageResponse = StorageResponse<StorageFile>.CreateSuccess(storageStyle);
        StorageResponse<StorageFile> stylePaginalStorageResponse = StorageResponse<StorageFile>.CreateSuccess(storageStylePaginal);
        _mockStorage.Setup(a => a.RemoveFolder(It.IsAny<PathBuilder>()))
            .ReturnsAsync(StorageResponse.CreateSuccess());
        _mockStorage.Setup(a => a.DownloadFile(It.IsAny<PathBuilder>()))
            .ReturnsAsync(StorageDisposableResponse<Stream>.CreateSuccess(stream));
        _mockStorage.Setup(a =>
                a.SaveFile(
                    It.Is<IEnumerable<FileContent>>(contents => contents.Any(ca =>
                        ca.ResourceType == ResourceType.Stylesheet && ca.FileName.Equals("style.css"))),
                    It.IsAny<PathBuilder>()))
            .ReturnsAsync(new List<StorageResponse<StorageFile>> { styleStorageResponse });
        _mockStorage.Setup(a =>
                a.SaveFile(
                    It.Is<IEnumerable<FileContent>>(contents => contents.Any(ca =>
                        ca.ResourceType == ResourceType.Stylesheet && ca.FileName.Equals("PaginalStyles.css"))),
                    It.IsAny<PathBuilder>()))
            .ReturnsAsync(new List<StorageResponse<StorageFile>> { stylePaginalStorageResponse });
        _mockStorage.Setup(a =>
                a.SaveFile(
                    It.Is<IEnumerable<FileContent>>(contents => contents.Any(ca =>
                        ca.ResourceType == ResourceType.HtmlContent && ca.FileName.Equals(TestFile.Pdf.ChangeExtension("html")))),
                    It.IsAny<PathBuilder>()))
            .ReturnsAsync(new List<StorageResponse<StorageFile>> { storageResponse });
        _mockMetaFileStorageCache.Setup(a =>
                a.UpdateFiles(It.IsAny<StorageMetaFile<PdfLoadOptions, PdfEditOptions>>()))
            .ReturnsAsync(new StorageMetaFile<PdfLoadOptions, PdfEditOptions>());
        // Act
        var result = await service.ConvertToHtml(metaFile, editOptions, loadOptions);

        // Assert
        var content = metaFile.StorageSubFiles["0"];
        content.Should().NotBeNull();
        content.Resources[storageFile.FileName].Should().Be(storageFile);
        content.Resources[storageStyle.FileName].Should().Be(storageStyle);
        content.Resources[storageStylePaginal.FileName].Should().Be(storageStylePaginal);
        result.Should().NotBeNull();
        result?.Length.Should().BeGreaterThan(0);
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
        PdfSaveOptions saveOptions = new();
        DownloadDocumentRequest request = new() { DocumentCode = documentCode, Format = "rtf", SaveOptions = saveOptions };
        await using Stream stream = TestFile.Pdf.OpenFile();
        StorageMetaFile<PdfLoadOptions, PdfEditOptions> metaFile = new()
        {
            DocumentCode = documentCode,
            DocumentInfo = new StorageDocumentInfo
            {
                Format = FixedLayoutFormats.Pdf,
                FamilyFormat = "FixedLayout",
                IsEncrypted = false,
                PageCount = 1,
                Size = 1
            },
            OriginalFile = new StorageFile
            {
                DocumentCode = documentCode,
                FileName = TestFile.Pdf.Name,
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
        result?.FileName.Should().Be(TestFile.Pdf.ChangeExtension("rtf"));
        result?.ResourceStream.Length.Should().BeGreaterThan(0);
        result?.ResourceStream.CanRead.Should().BeTrue();
        result?.ResourceStream.Position.Should().Be(0);
        result?.ResourceType.Should().Be(ResourceType.ConvertedDocument);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task UpdateHtmlContent()
    {
        // Arrange
        var service = CreateService();
        Guid documentCode = Guid.NewGuid();
        StorageSubFile<PdfEditOptions> currentContent = new(TestFile.Pdf.Name, "0") { DocumentCode = documentCode };
        const string htmlContents = "new content";
        StorageFile storageFile = new()
        {
            DocumentCode = documentCode,
            FileName = TestFile.Pdf.ChangeExtension("html"),
            ResourceType = ResourceType.HtmlContent
        };
        StorageResponse<StorageFile> storageResponse = StorageResponse<StorageFile>.CreateSuccess(storageFile);
        _mockStorage.Setup(a => a.RemoveFile(It.IsAny<PathBuilder>())).ReturnsAsync(StorageResponse.CreateSuccess());
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
        StorageSubFile<PdfEditOptions> currentContent = new(TestFile.Pdf.Name, "0") { DocumentCode = documentCode };
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