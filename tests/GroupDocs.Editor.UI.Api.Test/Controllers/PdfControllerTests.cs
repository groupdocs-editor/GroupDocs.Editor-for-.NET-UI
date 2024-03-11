using AutoMapper;
using FluentAssertions;
using GroupDocs.Editor.Formats;
using GroupDocs.Editor.Options;
using GroupDocs.Editor.UI.Api.Controllers;
using GroupDocs.Editor.UI.Api.Controllers.RequestModels;
using GroupDocs.Editor.UI.Api.Controllers.RequestModels.Pdf;
using GroupDocs.Editor.UI.Api.Models.DocumentConvertor;
using GroupDocs.Editor.UI.Api.Models.Editor;
using GroupDocs.Editor.UI.Api.Models.Storage;
using GroupDocs.Editor.UI.Api.Models.Storage.Responses;
using GroupDocs.Editor.UI.Api.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GroupDocs.Editor.UI.Api.Test.Controllers;

public class PdfControllerTests
{
    private readonly MockRepository mockRepository;

    private readonly Mock<IPdfEditorService> _mockEditorService;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IStorage> _mockStorage;
    private readonly Mock<IPdfStorageCache> _mockMetaFileStorageCache;

    public PdfControllerTests()
    {
        mockRepository = new MockRepository(MockBehavior.Strict);
        _mockEditorService = mockRepository.Create<IPdfEditorService>();
        _mockMapper = mockRepository.Create<IMapper>();
        _mockStorage = mockRepository.Create<IStorage>();
        _mockMetaFileStorageCache = mockRepository.Create<IPdfStorageCache>();
    }

    private PdfController CreatePdfController()
    {
        return new PdfController(
            new NullLogger<PdfController>(),
            _mockEditorService.Object,
            _mockMapper.Object,
            _mockStorage.Object,
            _mockMetaFileStorageCache.Object);
    }

    [Fact]
    public async Task UploadDocument()
    {
        // Arrange
        var pdfController = CreatePdfController();
        const string fileName = "pdf.pdf";
        Guid documentCode = Guid.NewGuid();
        await using var stream = new MemoryStream();
        IFormFile formFile = new FormFile(stream, 0, stream.Length, "id_from_form", fileName);
        PdfUploadRequest file = new()
        {
            File = formFile
        };
        UploadDocumentRequest uploadDocumentRequest = new() { FileName = "fixed.pdf", Stream = stream };

        StorageMetaFile<PdfLoadOptions, PdfEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo
                {
                    Format = FixedLayoutFormats.Pdf,
                    FamilyFormat = "fixed",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile { DocumentCode = documentCode }
            };
        _mockMapper.Setup(a => a.Map<UploadDocumentRequest>(file)).Returns(uploadDocumentRequest);
        _mockEditorService.Setup(a => a.UploadDocument(uploadDocumentRequest)).ReturnsAsync(storageMetaFile);
        // Act
        var result = await pdfController.Upload(file);

        // Assert
        result.Should().NotBeNull();
        var okObjectResult = result as OkObjectResult;
        okObjectResult.Should().NotBeNull();
        var responseDocument = okObjectResult?.Value as StorageMetaFile<PdfLoadOptions, PdfEditOptions>;
        responseDocument.Should().NotBeNull();
        responseDocument.Should().Be(storageMetaFile);
        mockRepository.VerifyAll();
    }

    [Fact]
    public async Task EditDocument_ConvertWithEditor()
    {
        // Arrange
        var pdfController = CreatePdfController();
        Guid documentCode = Guid.NewGuid();
        PdfEditOptions editOptions = new()
        {
            EnablePagination = true
        };
        PdfEditRequest request = new() { DocumentCode = documentCode, EditOptions = editOptions };
        string expectedHtml = "test html";

        StorageMetaFile<PdfLoadOptions, PdfEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo
                {
                    Format = FixedLayoutFormats.Pdf,
                    FamilyFormat = "fixed",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile { DocumentCode = documentCode }
            };
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(documentCode)).ReturnsAsync(storageMetaFile);
        _mockEditorService.Setup(a => a.ConvertToHtml(storageMetaFile, editOptions, null))
            .ReturnsAsync(expectedHtml);
        // Act
        var result = await pdfController.Edit(request);

        // Assert
        result.Should().NotBeNull();
        var okObjectResult = result as OkObjectResult;
        okObjectResult.Should().NotBeNull();
        var responseDocument = okObjectResult?.Value as string;
        responseDocument.Should().NotBeNull();
        responseDocument.Should().Be(expectedHtml);
        mockRepository.VerifyAll();
    }

    [Fact]
    public async Task EditDocument_WasConverted()
    {
        // Arrange
        var pdfController = CreatePdfController();
        Guid documentCode = Guid.NewGuid();
        PdfEditOptions editOptions = new()
        {
            EnablePagination = true
        };
        PdfEditRequest request = new() { DocumentCode = documentCode, EditOptions = editOptions };
        const string expectedHtml = "test html";

        StorageMetaFile<PdfLoadOptions, PdfEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo
                {
                    Format = FixedLayoutFormats.Pdf,
                    FamilyFormat = "fixed",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile { DocumentCode = documentCode },
                StorageSubFiles = new Dictionary<string, StorageSubFile<PdfEditOptions>>
                {
                    {"0", new StorageSubFile<PdfEditOptions>("fixed.pdf", "0")
                    {
                        EditOptions = editOptions,
                        DocumentCode = documentCode
                    }}
                }
            };
        StorageResponse<string> expectedFile = StorageResponse<string>.CreateSuccess(expectedHtml);
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(documentCode)).ReturnsAsync(storageMetaFile);
        _mockStorage.Setup(a => a.GetFileText(Path.Combine(documentCode.ToString(), "0", "fixed.html")))
            .ReturnsAsync(expectedFile);
        // Act
        var result = await pdfController.Edit(request);

        // Assert
        result.Should().NotBeNull();
        var okObjectResult = result as OkObjectResult;
        okObjectResult.Should().NotBeNull();
        var responseDocument = okObjectResult?.Value as string;
        responseDocument.Should().NotBeNull();
        responseDocument.Should().Be(expectedHtml);
        mockRepository.VerifyAll();
    }

    [Fact]
    public async Task DownloadDocument()
    {
        // Arrange
        var pdfController = CreatePdfController();
        Guid documentCode = Guid.NewGuid();
        PdfSaveOptions saveOptions = new();
        PdfDownloadRequest request = new() { DocumentCode = documentCode, Format = "pdf", SaveOptions = saveOptions };
        DownloadDocumentRequest documentRequest = new()
        {
            DocumentCode = documentCode,
            Format = "pdf",
            SaveOptions = saveOptions
        };
        using MemoryStream stream = new();
        FileContent fileContent = new()
        {
            FileName = "fixed.pdf",
            ResourceStream = stream,
            ResourceType = ResourceType.OriginalDocument
        };
        _mockMapper.Setup(a => a.Map<DownloadDocumentRequest>(request)).Returns(documentRequest);
        _mockEditorService.Setup(a => a.ConvertToDocument(documentRequest)).ReturnsAsync(fileContent);
        // Act
        var result = await pdfController.Download(
            request);

        // Assert
        result.Should().NotBeNull();
        var fileStream = result as FileStreamResult;
        fileStream.Should().NotBeNull();
        fileStream?.FileStream.Should().BeSameAs(stream);
        mockRepository.VerifyAll();
    }

    [Fact]
    public async Task UpdateDocument()
    {
        // Arrange
        var pdfController = CreatePdfController();
        Guid documentCode = Guid.NewGuid();
        const string editedHtml = "newContent";
        UpdateContentRequest request = new() { DocumentCode = documentCode, HtmlContents = editedHtml };
        PdfEditOptions editOptions = new()
        {
            EnablePagination = true
        };
        StorageMetaFile<PdfLoadOptions, PdfEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo
                {
                    Format = FixedLayoutFormats.Pdf,
                    FamilyFormat = "fixed",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile { DocumentCode = documentCode },
                StorageSubFiles = new Dictionary<string, StorageSubFile<PdfEditOptions>>
                {
                    {"0", new StorageSubFile<PdfEditOptions>("fixed.pdf", "0")
                    {
                        EditOptions = editOptions,
                        DocumentCode = documentCode
                    }}
                }
            };
        StorageSubFile<PdfEditOptions> storageSubFile =
            new("fixed.pdf", "0")
            {
                EditOptions = editOptions,
                DocumentCode = documentCode
            };
        StorageResponse<StorageSubFile<PdfEditOptions>> storageResponse =
            StorageResponse<StorageSubFile<PdfEditOptions>>.CreateSuccess(storageSubFile);
        StorageMetaFile<PdfLoadOptions, PdfEditOptions> newMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo
                {
                    Format = FixedLayoutFormats.Pdf,
                    FamilyFormat = "fixed",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile { DocumentCode = documentCode },
                StorageSubFiles = new Dictionary<string, StorageSubFile<PdfEditOptions>>
                {
                    {"0", storageSubFile}
                }
            };
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(documentCode)).ReturnsAsync(storageMetaFile);
        _mockMetaFileStorageCache.Setup(a => a.UpdateFiles(It.IsAny<StorageMetaFile<PdfLoadOptions, PdfEditOptions>>())).ReturnsAsync(newMetaFile);
        _mockEditorService
            .Setup(a => a.UpdateHtmlContent(storageMetaFile.StorageSubFiles["0"],
                editedHtml)).ReturnsAsync(storageResponse);
        // Act
        var result = await pdfController.Update(
            request);

        // Assert
        result.Should().NotBeNull();
        var fileStream = result as NoContentResult;
        fileStream.Should().NotBeNull();
        mockRepository.VerifyAll();
    }

    [Fact]
    public async Task UploadResource()
    {
        // Arrange
        var pdfController = CreatePdfController();
        Guid documentCode = Guid.NewGuid();
        const string fileName = "new.css";
        await using var stream = new MemoryStream();
        IFormFile formFile = new FormFile(stream, 0, stream.Length, "id_from_form", fileName);
        UploadResourceRequest resource = new() { DocumentCode = documentCode, File = formFile, OldResourceName = "test.css", ResourceType = ResourceType.Stylesheet };
        PdfEditOptions editOptions = new()
        {
            EnablePagination = true
        };
        StorageMetaFile<PdfLoadOptions, PdfEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo
                {
                    Format = FixedLayoutFormats.Pdf,
                    FamilyFormat = "fixed",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile { DocumentCode = documentCode },
                StorageSubFiles = new Dictionary<string, StorageSubFile<PdfEditOptions>>
                {
                    {"0", new StorageSubFile<PdfEditOptions>("fixed.pdf", "0")
                    {
                        EditOptions = editOptions,
                        DocumentCode = documentCode
                    }}
                }
            };
        StorageFile storageFile = new() { DocumentCode = documentCode, FileName = fileName, ResourceType = ResourceType.Stylesheet };
        StorageSubFile<PdfEditOptions> storageSub = new(documentCode.ToString(), "0");
        StorageUpdateResourceResponse<StorageSubFile<PdfEditOptions>, StorageFile>
            storageUpdateResourceResponse =
                StorageUpdateResourceResponse<StorageSubFile<PdfEditOptions>, StorageFile>.CreateSuccess(
                    storageSub, storageFile);
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(documentCode)).ReturnsAsync(storageMetaFile);
        _mockEditorService.Setup(a => a.UpdateResource(storageMetaFile.StorageSubFiles["0"], resource))
            .ReturnsAsync(storageUpdateResourceResponse);
        _mockMetaFileStorageCache.Setup(a => a.UpdateFiles(It.IsAny<StorageMetaFile<PdfLoadOptions, PdfEditOptions>>())).ReturnsAsync(storageMetaFile);

        // Act
        var result = await pdfController.UploadResource(
            resource);

        // Assert
        result.Should().NotBeNull();
        var okObjectResult = result as OkObjectResult;
        okObjectResult.Should().NotBeNull();
        var responseDocument = okObjectResult?.Value as StorageFile;
        responseDocument.Should().NotBeNull();
        responseDocument.Should().Be(storageFile);
        mockRepository.VerifyAll();
    }

    [Fact]
    public async Task Stylesheets()
    {
        // Arrange
        var pdfController = CreatePdfController();
        Guid documentCode = Guid.NewGuid();
        StorageMetaFile<PdfLoadOptions, PdfEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo
                {
                    Format = FixedLayoutFormats.Pdf,
                    FamilyFormat = "fixed",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile { DocumentCode = documentCode },
                StorageSubFiles = new Dictionary<string, StorageSubFile<PdfEditOptions>>
                {
                    {"0", new StorageSubFile<PdfEditOptions>("fixed.pdf", "0")
                    {
                        Resources = new Dictionary<string, StorageFile>
                        {
                            {"style.css", new StorageFile {FileName = "style.css", ResourceType = ResourceType.Stylesheet}}
                        }
                    }}
                }
            };
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(documentCode)).ReturnsAsync(storageMetaFile);

        // Act
        var result = await pdfController.Stylesheets(
            documentCode);

        // Assert
        result.Should().NotBeNull();
        var okObjectResult = result as OkObjectResult;
        okObjectResult.Should().NotBeNull();
        var responseDocument = okObjectResult?.Value as IEnumerable<StorageFile>;
        IEnumerable<StorageFile> storageFiles = responseDocument as StorageFile[] ?? responseDocument?.ToArray() ?? Array.Empty<StorageFile>();
        storageFiles.Should().NotBeNull();
        storageFiles.Should().BeEquivalentTo(new List<StorageFile> { new() { FileName = "style.css", ResourceType = ResourceType.Stylesheet } });
        mockRepository.VerifyAll();
    }

    [Fact]
    public async Task MetaInfo()
    {
        // Arrange
        var pdfController = CreatePdfController();
        Guid documentCode = Guid.NewGuid();
        StorageMetaFile<PdfLoadOptions, PdfEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo
                {
                    Format = FixedLayoutFormats.Pdf,
                    FamilyFormat = "fixed",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile { DocumentCode = documentCode },
                StorageSubFiles = new Dictionary<string, StorageSubFile<PdfEditOptions>>
                {
                    {"0", new StorageSubFile<PdfEditOptions>("fixed.pdf", "0")
                    {
                        Resources = new Dictionary<string, StorageFile>
                        {
                            {"style.css", new StorageFile {FileName = "style.css", ResourceType = ResourceType.Stylesheet}}
                        }
                    }}
                }
            };
        var storageMeta = new PdfStorageInfo() { DocumentCode = documentCode };
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(documentCode)).ReturnsAsync(storageMetaFile);
        _mockMapper.Setup(a => a.Map<PdfStorageInfo>(storageMetaFile))
            .Returns(storageMeta);
        // Act
        var result = await pdfController.MetaInfo(documentCode);

        // Assert
        result.Should().NotBeNull();
        var okObjectResult = result as OkObjectResult;
        okObjectResult.Should().NotBeNull();
        var responseDocument = okObjectResult?.Value as PdfStorageInfo;
        responseDocument.Should().NotBeNull();
        responseDocument.Should().Be(storageMeta);
        mockRepository.VerifyAll();
    }

    [Fact]
    public void SupportedFormats()
    {
        // Arrange
        var pdfController = CreatePdfController();
        Dictionary<string, string> expected = new() { { FixedLayoutFormats.Pdf.Extension, FixedLayoutFormats.Pdf.Name } };
        // Act
        var result = pdfController.SupportedFormats();

        // Assert
        result.Should().NotBeNull();
        var okObjectResult = result as OkObjectResult;
        okObjectResult.Should().NotBeNull();
        var data = okObjectResult?.Value as Dictionary<string, string>;
        data.Should().NotBeNull();
        data.Should().BeEquivalentTo(expected);
        mockRepository.VerifyAll();
    }
}