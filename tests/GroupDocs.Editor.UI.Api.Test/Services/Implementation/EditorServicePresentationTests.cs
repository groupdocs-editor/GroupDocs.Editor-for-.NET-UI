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

namespace GroupDocs.Editor.UI.Api.Test.Services.Implementation;

public class EditorServicePresentationTests
{
    private readonly MockRepository _mockRepository;

    private readonly Mock<IStorage> _mockStorage;
    private readonly Mock<IMetaFileStorageCache<PresentationLoadOptions, PresentationEditOptions>> _mockMetaFileStorageCache;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IIdGeneratorService> _mockIdGeneratorService;

    public EditorServicePresentationTests()
    {
        _mockRepository = new MockRepository(MockBehavior.Strict);

        _mockStorage = _mockRepository.Create<IStorage>();
        _mockMetaFileStorageCache = _mockRepository.Create<IMetaFileStorageCache<PresentationLoadOptions, PresentationEditOptions>>();
        _mockMapper = _mockRepository.Create<IMapper>();
        _mockIdGeneratorService = _mockRepository.Create<IIdGeneratorService>();
    }

    private EditorService<PresentationLoadOptions, PresentationEditOptions> CreateService()
    {
        return new EditorService<PresentationLoadOptions, PresentationEditOptions>(
            _mockStorage.Object,
            new NullLogger<EditorService<PresentationLoadOptions, PresentationEditOptions>>(),
            _mockMetaFileStorageCache.Object,
            _mockMapper.Object,
            _mockIdGeneratorService.Object);
    }

    [Fact]
    public async Task CreateDocument()
    {
        // Arrange
        var service = CreateService();
        Guid documentCode = Guid.NewGuid();
        var docInfo = new StorageDocumentInfo
        {
            Format = PresentationFormats.Pptx,
            FamilyFormat = "Presentation",
            IsEncrypted = false,
            PageCount = 1,
            Size = 1
        };
        StorageFile storageFile = new()
        { DocumentCode = documentCode, FileName = "document.pptx", ResourceType = ResourceType.OriginalDocument };
        StorageResponse<StorageFile> storageResponse = StorageResponse<StorageFile>.CreateSuccess(storageFile);
        CreateDocumentRequest request = new() { FileName = "document.pptx", Format = PresentationFormats.Pptx };
        _mockStorage.Setup(a => a.SaveFile(It.IsAny<List<FileContent>>(), documentCode, ""))
            .ReturnsAsync(new List<StorageResponse<StorageFile>> { storageResponse });
        _mockMetaFileStorageCache.Setup(a =>
                a.UpdateFiles(It.IsAny<StorageMetaFile<PresentationLoadOptions, PresentationEditOptions>>()))
            .ReturnsAsync(new StorageMetaFile<PresentationLoadOptions, PresentationEditOptions>());
        _mockIdGeneratorService.Setup(a => a.GenerateDocumentCode()).Returns(documentCode);
        _mockMapper.Setup(a => a.Map<StorageDocumentInfo>(It.IsAny<PresentationDocumentInfo>())).Returns(docInfo);

        // Act
        var result = await service.CreateDocument(request);

        // Assert
        result.Should().NotBeNull();
        result?.DocumentCode.Should().Be(documentCode);
        result?.OriginalLoadOptions.Should().BeNull();
        result?.OriginalFile.Should().BeEquivalentTo(new StorageFile
        {
            DocumentCode = documentCode,
            FileName = "document.pptx",
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
        await using Stream stream = TestFile.Presentation.OpenFile();
        StorageFile storageFile = new()
        { DocumentCode = documentCode, FileName = TestFile.Presentation.Name, ResourceType = ResourceType.OriginalDocument };
        var docInfo = new StorageDocumentInfo
        {
            Format = PresentationFormats.Pptx,
            FamilyFormat = "Presentation",
            IsEncrypted = false,
            PageCount = 1,
            Size = 1
        };
        StorageResponse<StorageFile> storageResponse = StorageResponse<StorageFile>.CreateSuccess(storageFile);
        UploadDocumentRequest request = new() { FileName = TestFile.Presentation.Name, Stream = stream };
        _mockMapper.Setup(a => a.Map<StorageDocumentInfo>(It.IsAny<PresentationDocumentInfo>())).Returns(docInfo);
        _mockIdGeneratorService.Setup(a => a.GenerateDocumentCode()).Returns(documentCode);
        _mockStorage.Setup(a => a.SaveFile(It.IsAny<List<FileContent>>(), documentCode, ""))
            .ReturnsAsync(new List<StorageResponse<StorageFile>> { storageResponse });
        _mockMetaFileStorageCache.Setup(a =>
                a.UpdateFiles(It.IsAny<StorageMetaFile<PresentationLoadOptions, PresentationEditOptions>>()))
            .ReturnsAsync(new StorageMetaFile<PresentationLoadOptions, PresentationEditOptions>());
        // Act
        var result = await service.UploadDocument(request);

        // Assert
        result.Should().NotBeNull();
        result?.DocumentCode.Should().Be(documentCode);
        result?.OriginalLoadOptions.Should().BeNull();
        result?.OriginalFile.Should().BeEquivalentTo(new StorageFile
        {
            DocumentCode = documentCode,
            FileName = TestFile.Presentation.Name,
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
        using Stream stream = TestFile.Presentation.OpenFile();
        PresentationLoadOptions loadOptions = new PresentationLoadOptions();

        // Act
        var result = service.GetDocumentInfo(stream, loadOptions);

        // Assert
        result.PageCount.Should().Be(1);
        result.Size.Should().Be(35769);
        result.Format.Should().Be(PresentationFormats.Pptx);
        result.IsEncrypted.Should().BeFalse();
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task ConvertToHtml()
    {
        // Arrange
        var service = CreateService();
        Guid documentCode = Guid.NewGuid();
        await using Stream stream = TestFile.Presentation.OpenFile();
        StorageMetaFile<PresentationLoadOptions, PresentationEditOptions> metaFile = new()
        {
            DocumentCode = documentCode,
            DocumentInfo = new StorageDocumentInfo
            {
                Format = PresentationFormats.Pptx,
                FamilyFormat = "Presentation",
                IsEncrypted = false,
                PageCount = 1,
                Size = 1
            },
            OriginalFile = new StorageFile
            {
                DocumentCode = documentCode,
                FileName = TestFile.Presentation.Name,
                ResourceType = ResourceType.OriginalDocument
            },
        };
        PresentationEditOptions editOptions = new() { SlideNumber = 0 };
        ILoadOptions loadOptions = new PresentationLoadOptions();
        StorageFile storageFile = new()
        {
            DocumentCode = documentCode,
            FileName = TestFile.Presentation.ChangeExtension("html"),
            ResourceType = ResourceType.HtmlContent
        };
        StorageFile storageStyle = new()
        {
            DocumentCode = documentCode,
            FileName = "style.css",
            ResourceType = ResourceType.HtmlContent
        };

        StorageResponse<StorageFile> storageResponse = StorageResponse<StorageFile>.CreateSuccess(storageFile);
        StorageResponse<StorageFile> styleStorageResponse = StorageResponse<StorageFile>.CreateSuccess(storageStyle);
        _mockStorage.Setup(a => a.RemoveFolder(Path.Combine(documentCode.ToString(), "0")))
            .ReturnsAsync(StorageResponse.CreateSuccess());
        _mockStorage.Setup(a => a.DownloadFile(Path.Combine(documentCode.ToString(), TestFile.Presentation.Name)))
            .ReturnsAsync(StorageDisposableResponse<Stream>.CreateSuccess(stream));
        _mockStorage.Setup(a =>
                a.SaveFile(
                    It.Is<IEnumerable<FileContent>>(contents => contents.Any(ca =>
                        ca.ResourceType == ResourceType.Stylesheet && ca.FileName.Equals("style.css"))),
                    documentCode, "0"))
            .ReturnsAsync(new List<StorageResponse<StorageFile>> { styleStorageResponse });

        _mockStorage.Setup(a =>
                a.SaveFile(
                    It.Is<IEnumerable<FileContent>>(contents => contents.Any(ca =>
                        ca.ResourceType == ResourceType.HtmlContent && ca.FileName.Equals(TestFile.Presentation.ChangeExtension("html")))),
                    documentCode, "0"))
            .ReturnsAsync(new List<StorageResponse<StorageFile>> { storageResponse });
        _mockMetaFileStorageCache.Setup(a =>
                a.UpdateFiles(It.IsAny<StorageMetaFile<PresentationLoadOptions, PresentationEditOptions>>()))
            .ReturnsAsync(new StorageMetaFile<PresentationLoadOptions, PresentationEditOptions>());
        // Act
        var result = await service.ConvertToHtml(metaFile, editOptions, loadOptions);

        // Assert
        var content = metaFile.StorageSubFiles["0"];
        content.Should().NotBeNull();
        content.Resources[storageFile.FileName].Should().Be(storageFile);
        content.Resources[storageStyle.FileName].Should().Be(storageStyle);
        result.Should().NotBeNull();
        result?.Length.Should().BeGreaterThan(0);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task ConvertPreviews()
    {
        // Arrange
        var service = CreateService();
        Guid documentCode = Guid.NewGuid();
        await using Stream stream = TestFile.Presentation.OpenFile();
        StorageMetaFile<PresentationLoadOptions, PresentationEditOptions> metaFile = new()
        {
            DocumentCode = documentCode,
            DocumentInfo = new StorageDocumentInfo
            {
                Format = PresentationFormats.Pptx,
                FamilyFormat = "Presentation",
                IsEncrypted = false,
                PageCount = 1,
                Size = 1
            },
            OriginalFile = new StorageFile
            {
                DocumentCode = documentCode,
                FileName = TestFile.Presentation.Name,
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
                    documentCode, "preview"))
            .ReturnsAsync(new List<StorageResponse<StorageFile>> { storageResponse });
        _mockStorage.Setup(a => a.DownloadFile(Path.Combine(documentCode.ToString(), TestFile.Presentation.Name)))
            .ReturnsAsync(StorageDisposableResponse<Stream>.CreateSuccess(stream));
        _mockMetaFileStorageCache.Setup(a =>
                a.UpdateFiles(It.IsAny<StorageMetaFile<PresentationLoadOptions, PresentationEditOptions>>()))
            .ReturnsAsync(new StorageMetaFile<PresentationLoadOptions, PresentationEditOptions>());

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

        PdfSaveOptions saveOptions = new PdfSaveOptions();
        DownloadPdfRequest request = new() { DocumentCode = documentCode, SaveOptions = saveOptions };
        await using Stream stream = TestFile.Presentation.OpenFile();
        StorageMetaFile<PresentationLoadOptions, PresentationEditOptions> metaFile = new()
        {
            DocumentCode = documentCode,
            DocumentInfo = new StorageDocumentInfo
            {
                Format = PresentationFormats.Pptx,
                FamilyFormat = "Presentation",
                IsEncrypted = false,
                PageCount = 1,
                Size = 1
            },
            OriginalFile = new StorageFile
            {
                DocumentCode = documentCode,
                FileName = TestFile.Presentation.Name,
                ResourceType = ResourceType.OriginalDocument
            },
        };
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(documentCode)).ReturnsAsync(metaFile);
        _mockStorage.Setup(a => a.DownloadFile(Path.Combine(documentCode.ToString(), TestFile.Presentation.Name)))
            .ReturnsAsync(StorageDisposableResponse<Stream>.CreateSuccess(stream));

        // Act
        using var result = await service.SaveToPdf(request);

        // Assert
        result.Should().NotBeNull();
        result?.FileName.Should().Be(TestFile.Presentation.ChangeExtension("pdf"));
        result?.ResourceStream.Length.Should().BeGreaterThan(0);
        result?.ResourceStream.CanRead.Should().BeTrue();
        result?.ResourceStream.Position.Should().Be(0);
        result?.ResourceType.Should().Be(ResourceType.ConvertedDocument);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task ConvertToDocument()
    {
        // Arrange
        var service = CreateService();
        Guid documentCode = Guid.NewGuid();
        PresentationSaveOptions saveOptions = new(PresentationFormats.Ppt);
        DownloadDocumentRequest request = new() { DocumentCode = documentCode, Format = "rtf", SaveOptions = saveOptions };
        await using Stream stream = TestFile.Presentation.OpenFile();
        StorageMetaFile<PresentationLoadOptions, PresentationEditOptions> metaFile = new()
        {
            DocumentCode = documentCode,
            DocumentInfo = new StorageDocumentInfo
            {
                Format = PresentationFormats.Pptx,
                FamilyFormat = "Presentation",
                IsEncrypted = false,
                PageCount = 1,
                Size = 1
            },
            OriginalFile = new StorageFile
            {
                DocumentCode = documentCode,
                FileName = TestFile.Presentation.Name,
                ResourceType = ResourceType.OriginalDocument
            },
        };
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(documentCode)).ReturnsAsync(metaFile);
        _mockStorage.Setup(a => a.DownloadFile(Path.Combine(documentCode.ToString(), TestFile.Presentation.Name)))
            .ReturnsAsync(StorageDisposableResponse<Stream>.CreateSuccess(stream));

        // Act
        using var result = await service.ConvertToDocument(request);

        // Assert
        result.Should().NotBeNull();
        result?.FileName.Should().Be(TestFile.Presentation.ChangeExtension("rtf"));
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
        var expected = PresentationFormats.All.GroupBy(a => a.Extension).Select(a => a.First()).ToList();
        // Act
        var result = service.GetSupportedFormats<PresentationFormats>();

        // Assert
        var presentationFormats = result.ToList();
        presentationFormats.Should().NotBeNull();
        presentationFormats.Should().BeEquivalentTo(expected);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task UpdateHtmlContent()
    {
        // Arrange
        var service = CreateService();
        Guid documentCode = Guid.NewGuid();
        StorageSubFile<PresentationEditOptions> currentContent = new(TestFile.Presentation.Name, "0") { DocumentCode = documentCode };
        const string htmlContents = "new content";
        StorageFile storageFile = new()
        {
            DocumentCode = documentCode,
            FileName = TestFile.Presentation.ChangeExtension("html"),
            ResourceType = ResourceType.HtmlContent
        };
        StorageResponse<StorageFile> storageResponse = StorageResponse<StorageFile>.CreateSuccess(storageFile);
        var filePath = Path.Combine(currentContent.DocumentCode.ToString(),
            currentContent.SubCode, TestFile.Presentation.ChangeExtension("html"));
        _mockStorage.Setup(a => a.RemoveFile(filePath)).ReturnsAsync(StorageResponse.CreateSuccess());
        _mockStorage.Setup(a =>
                a.SaveFile(It.IsAny<IEnumerable<FileContent>>(),
                    documentCode, "0"))
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
        StorageSubFile<PresentationEditOptions> currentContent = new(TestFile.Presentation.Name, "0") { DocumentCode = documentCode };
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
                    documentCode, "0"))
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