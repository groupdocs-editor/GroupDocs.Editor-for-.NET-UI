using AutoMapper;
using FluentAssertions;
using GroupDocs.Editor.Formats;
using GroupDocs.Editor.Options;
using GroupDocs.Editor.UI.Api.Controllers;
using GroupDocs.Editor.UI.Api.Controllers.RequestModels;
using GroupDocs.Editor.UI.Api.Controllers.RequestModels.WordProcessing;
using GroupDocs.Editor.UI.Api.Controllers.ResponseModels;
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

public class WordProcessingControllerTests
{
    private readonly MockRepository mockRepository;

    private readonly Mock<IWordProcessingEditorService> _mockEditorService;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IStorage> _mockStorage;
    private readonly Mock<IWordProcessingStorageCache> _mockMetaFileStorageCache;

    public WordProcessingControllerTests()
    {
        mockRepository = new MockRepository(MockBehavior.Strict);

        _mockEditorService = mockRepository.Create<IWordProcessingEditorService>();
        _mockMapper = mockRepository.Create<IMapper>();
        _mockStorage = mockRepository.Create<IStorage>();
        _mockMetaFileStorageCache = mockRepository.Create<IWordProcessingStorageCache>();
    }

    private WordProcessingController CreateWordProcessingController()
    {
        return new WordProcessingController(
            new NullLogger<WordProcessingController>(),
            _mockEditorService.Object,
            _mockMapper.Object,
            _mockStorage.Object,
            _mockMetaFileStorageCache.Object);
    }

    [Fact]
    public async Task UploadDocument()
    {
        // Arrange
        var wordProcessingController = CreateWordProcessingController();
        const string fileName = "document.docx";
        Guid documentCode = Guid.NewGuid();
        await using var stream = new MemoryStream();
        IFormFile formFile = new FormFile(stream, 0, stream.Length, "id_from_form", fileName);
        WordProcessingUploadRequest file = new()
        {
            File = formFile
        };
        UploadDocumentRequest uploadDocumentRequest = new() { FileName = "document.docx", Stream = stream };
        WordProcessingUploadResponse documentUploadResponse = new()
        {
            DocumentCode = documentCode,
            DocumentInfo = new StorageDocumentInfo
            {
                Format = WordProcessingFormats.Docx,
                FamilyFormat = "wordProcessing",
                IsEncrypted = false,
                PageCount = 10,
                Size = 258
            },
            OriginalFile = new StorageFile { DocumentCode = documentCode }
        };
        StorageMetaFile<WordProcessingLoadOptions, WordProcessingEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo
                {
                    Format = WordProcessingFormats.Docx,
                    FamilyFormat = "wordProcessing",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile { DocumentCode = documentCode }
            };
        _mockMapper.Setup(a => a.Map<UploadDocumentRequest>(file)).Returns(uploadDocumentRequest);
        _mockMapper.Setup(a => a.Map<WordProcessingUploadResponse>(storageMetaFile)).Returns(documentUploadResponse);
        _mockEditorService.Setup(a => a.UploadDocument(uploadDocumentRequest)).ReturnsAsync(storageMetaFile);
        // Act
        var result = await wordProcessingController.Upload(
            file);

        // Assert
        result.Should().NotBeNull();
        var okObjectResult = result as OkObjectResult;
        okObjectResult.Should().NotBeNull();
        var responseDocument = okObjectResult?.Value as WordProcessingUploadResponse;
        responseDocument.Should().NotBeNull();
        responseDocument.Should().Be(documentUploadResponse);
        mockRepository.VerifyAll();
    }

    [Fact]
    public async Task NewDocument()
    {
        // Arrange
        var wordProcessingController = CreateWordProcessingController();
        Guid documentCode = Guid.NewGuid();
        WordProcessingNewDocumentRequest file = new()
        {
            FileName = "document.docx",
            Format = WordProcessingFormats.Docx.Extension
        };
        CreateDocumentRequest createDocumentRequest = new() { FileName = "document.docx", Format = WordProcessingFormats.Docx };
        StorageMetaFile<WordProcessingLoadOptions, WordProcessingEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo
                {
                    Format = WordProcessingFormats.Docx,
                    FamilyFormat = "wordProcessing",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile { DocumentCode = documentCode }
            };
        WordProcessingUploadResponse uploadResponse = new()
        {
            DocumentCode = documentCode,
            DocumentInfo = new StorageDocumentInfo
            {
                Format = WordProcessingFormats.Docx,
                FamilyFormat = "wordProcessing",
                IsEncrypted = false,
                PageCount = 10,
                Size = 258
            },
            OriginalFile = new StorageFile { DocumentCode = documentCode }
        };
        _mockMapper.Setup(a => a.Map<CreateDocumentRequest>(file)).Returns(createDocumentRequest);
        _mockMapper.Setup(a => a.Map<WordProcessingUploadResponse>(storageMetaFile)).Returns(uploadResponse);
        _mockEditorService.Setup(a => a.CreateDocument(createDocumentRequest)).ReturnsAsync(storageMetaFile);
        // Act
        var result = await wordProcessingController.NewDocument(file);

        // Assert
        result.Should().NotBeNull();
        var okObjectResult = result as OkObjectResult;
        okObjectResult.Should().NotBeNull();
        var responseDocument = okObjectResult?.Value as WordProcessingUploadResponse;
        responseDocument.Should().NotBeNull();
        responseDocument.Should().Be(uploadResponse);
        mockRepository.VerifyAll();
    }

    [Fact]
    public async Task EditDocument_ConvertWithEditor()
    {
        // Arrange
        var wordProcessingController = CreateWordProcessingController();
        Guid documentCode = Guid.NewGuid();
        WordProcessingEditOptions editOptions = new(true);
        WordProcessingEditRequest request = new() { DocumentCode = documentCode, EditOptions = editOptions };
        using var expectedHtml = new MemoryStream();

        StorageMetaFile<WordProcessingLoadOptions, WordProcessingEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo
                {
                    Format = WordProcessingFormats.Docx,
                    FamilyFormat = "wordProcessing",
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
        var result = await wordProcessingController.Edit(
            request);

        // Assert
        result.Should().NotBeNull();
        var okObjectResult = result as FileStreamResult;
        okObjectResult.Should().NotBeNull();
        var responseDocument = okObjectResult?.FileStream;
        responseDocument.Should().NotBeNull();
        responseDocument.Should().BeSameAs(expectedHtml);
        mockRepository.VerifyAll();
    }

    [Fact]
    public async Task EditDocument_WasConverted()
    {
        // Arrange
        var wordProcessingController = CreateWordProcessingController();
        Guid documentCode = Guid.NewGuid();
        WordProcessingEditOptions editOptions = new(true);
        WordProcessingEditRequest request = new() { DocumentCode = documentCode, EditOptions = editOptions };
        using var expectedHtml = new MemoryStream();

        StorageMetaFile<WordProcessingLoadOptions, WordProcessingEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo
                {
                    Format = WordProcessingFormats.Docx,
                    FamilyFormat = "wordProcessing",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile { DocumentCode = documentCode },
                StorageSubFiles = new Dictionary<string, StorageSubFile<WordProcessingEditOptions>>
                {
                    {"0", new StorageSubFile<WordProcessingEditOptions>("fixed.docx", "0")
                    {
                        EditOptions = editOptions,
                        DocumentCode = documentCode
                    }}
                }
            };
        StorageDisposableResponse<Stream> expectedFile = StorageDisposableResponse<Stream>.CreateSuccess(expectedHtml);
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(documentCode)).ReturnsAsync(storageMetaFile);
        _mockStorage.Setup(a => a.DownloadFile(It.IsAny<PathBuilder>()))
            .ReturnsAsync(expectedFile);
        // Act
        var result = await wordProcessingController.Edit(request);

        // Assert
        result.Should().NotBeNull();
        var okObjectResult = result as FileStreamResult;
        okObjectResult.Should().NotBeNull();
        var responseDocument = okObjectResult?.FileStream;
        responseDocument.Should().NotBeNull();
        responseDocument.Should().BeSameAs(expectedHtml);
        mockRepository.VerifyAll();
    }

    [Fact]
    public async Task DownloadDocument()
    {
        // Arrange
        var wordProcessingController = CreateWordProcessingController();
        Guid documentCode = Guid.NewGuid();
        WordProcessingSaveOptions saveOptions = new();
        WordProcessingDownloadRequest request = new() { DocumentCode = documentCode, Format = "docx", SaveOptions = saveOptions };
        DownloadDocumentRequest documentRequest = new()
        {
            DocumentCode = documentCode,
            Format = "docx",
            SaveOptions = saveOptions
        };
        using MemoryStream stream = new();
        FileContent fileContent = new()
        {
            FileName = "fixed.docx",
            ResourceStream = stream,
            ResourceType = ResourceType.OriginalDocument
        };
        _mockMapper.Setup(a => a.Map<DownloadDocumentRequest>(request)).Returns(documentRequest);
        _mockEditorService.Setup(a => a.ConvertToDocument(documentRequest)).ReturnsAsync(fileContent);
        // Act
        var result = await wordProcessingController.Download(
            request);

        // Assert
        result.Should().NotBeNull();
        var fileStream = result as FileStreamResult;
        fileStream.Should().NotBeNull();
        fileStream?.FileStream.Should().BeSameAs(stream);
        mockRepository.VerifyAll();
    }

    [Fact]
    public async Task DownloadPdf_StateUnderTest_ExpectedBehavior()
    {
        // Arrange
        var wordProcessingController = CreateWordProcessingController();
        Guid documentCode = Guid.NewGuid();
        PdfSaveOptions saveOptions = new();
        WordProcessingToPdfDownloadRequest request = new() { DocumentCode = documentCode, SaveOptions = saveOptions };
        DownloadPdfRequest downloadPdfRequest = new() { DocumentCode = documentCode, SaveOptions = saveOptions };
        _mockMapper.Setup(a => a.Map<DownloadPdfRequest>(request)).Returns(downloadPdfRequest);
        using MemoryStream stream = new();
        FileContent fileContent = new()
        {
            FileName = "fixed.docx",
            ResourceStream = stream,
            ResourceType = ResourceType.OriginalDocument
        };
        _mockEditorService.Setup(a => a.SaveToPdf(downloadPdfRequest)).ReturnsAsync(fileContent);
        // Act
        var result = await wordProcessingController.DownloadPdf(request);

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
        var wordProcessingController = CreateWordProcessingController();
        Guid documentCode = Guid.NewGuid();
        const string editedHtml = "newContent";
        UpdateContentRequest request = new() { DocumentCode = documentCode, HtmlContents = editedHtml };
        WordProcessingEditOptions editOptions = new()
        {
            EnablePagination = true
        };
        StorageMetaFile<WordProcessingLoadOptions, WordProcessingEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo
                {
                    Format = WordProcessingFormats.Docx,
                    FamilyFormat = "WordProcessing",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile { DocumentCode = documentCode },
                StorageSubFiles = new Dictionary<string, StorageSubFile<WordProcessingEditOptions>>
                {
                    {"0", new StorageSubFile<WordProcessingEditOptions>("fixed.docx", "0")
                    {
                        EditOptions = editOptions,
                        DocumentCode = documentCode
                    }}
                }
            };
        StorageSubFile<WordProcessingEditOptions> storageSubFile =
            new("fixed.docx", "0")
            {
                EditOptions = editOptions,
                DocumentCode = documentCode
            };
        StorageResponse<StorageSubFile<WordProcessingEditOptions>> storageResponse =
            StorageResponse<StorageSubFile<WordProcessingEditOptions>>.CreateSuccess(storageSubFile);
        StorageMetaFile<WordProcessingLoadOptions, WordProcessingEditOptions> newMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo
                {
                    Format = WordProcessingFormats.Docx,
                    FamilyFormat = "WordProcessing",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile { DocumentCode = documentCode },
                StorageSubFiles = new Dictionary<string, StorageSubFile<WordProcessingEditOptions>>
                {
                    {"0", storageSubFile}
                }
            };
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(documentCode)).ReturnsAsync(storageMetaFile);
        _mockMetaFileStorageCache.Setup(a => a.UpdateFiles(It.IsAny<StorageMetaFile<WordProcessingLoadOptions, WordProcessingEditOptions>>())).ReturnsAsync(newMetaFile);
        _mockEditorService
            .Setup(a => a.UpdateHtmlContent(storageMetaFile.StorageSubFiles["0"],
                editedHtml)).ReturnsAsync(storageResponse);
        // Act
        var result = await wordProcessingController.Update(
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
        var wordProcessingController = CreateWordProcessingController();
        Guid documentCode = Guid.NewGuid();
        const string fileName = "new.css";
        await using var stream = new MemoryStream();
        IFormFile formFile = new FormFile(stream, 0, stream.Length, "id_from_form", fileName);
        UploadResourceRequest resource = new() { DocumentCode = documentCode, File = formFile, OldResourceName = "test.css", ResourceType = ResourceType.Stylesheet };
        WordProcessingEditOptions editOptions = new()
        {
            EnablePagination = true
        };
        StorageMetaFile<WordProcessingLoadOptions, WordProcessingEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo
                {
                    Format = WordProcessingFormats.Docx,
                    FamilyFormat = "WordProcessing",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile { DocumentCode = documentCode },
                StorageSubFiles = new Dictionary<string, StorageSubFile<WordProcessingEditOptions>>
                {
                    {"0", new StorageSubFile<WordProcessingEditOptions>("fixed.docx", "0")
                    {
                        EditOptions = editOptions,
                        DocumentCode = documentCode
                    }}
                }
            };
        StorageFile storageFile = new() { DocumentCode = documentCode, FileName = fileName, ResourceType = ResourceType.Stylesheet };
        StorageSubFile<WordProcessingEditOptions> storageSub = new(documentCode.ToString(), "0");
        StorageUpdateResourceResponse<StorageSubFile<WordProcessingEditOptions>, StorageFile>
            storageUpdateResourceResponse =
                StorageUpdateResourceResponse<StorageSubFile<WordProcessingEditOptions>, StorageFile>.CreateSuccess(
                    storageSub, storageFile);
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(documentCode)).ReturnsAsync(storageMetaFile);
        _mockEditorService.Setup(a => a.UpdateResource(storageMetaFile.StorageSubFiles["0"], resource))
            .ReturnsAsync(storageUpdateResourceResponse);
        _mockMetaFileStorageCache.Setup(a => a.UpdateFiles(It.IsAny<StorageMetaFile<WordProcessingLoadOptions, WordProcessingEditOptions>>())).ReturnsAsync(storageMetaFile);

        // Act
        var result = await wordProcessingController.UploadResource(resource);

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
    public async Task Previews()
    {
        // Arrange
        var wordProcessingController = CreateWordProcessingController();
        Guid documentCode = Guid.NewGuid();
        StorageFile storageFile = new() { DocumentCode = documentCode, FileName = "preview", ResourceType = ResourceType.Preview };
        StorageMetaFile<WordProcessingLoadOptions, WordProcessingEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo
                {
                    Format = WordProcessingFormats.Docx,
                    FamilyFormat = "WordProcessing",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile { DocumentCode = documentCode }
            };
        StorageMetaFile<WordProcessingLoadOptions, WordProcessingEditOptions> storageMetaFileEditor =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo
                {
                    Format = WordProcessingFormats.Docx,
                    FamilyFormat = "WordProcessing",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile { DocumentCode = documentCode },
                PreviewImages = new Dictionary<string, StorageFile>
                {
                    {"0", storageFile}
                }
            };
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(documentCode)).ReturnsAsync(storageMetaFile);
        _mockEditorService.Setup(a => a.ConvertPreviews(documentCode)).ReturnsAsync(storageMetaFileEditor);

        // Act
        var result = await wordProcessingController.Previews(documentCode);

        // Assert
        result.Should().NotBeNull();
        var okObjectResult = result as OkObjectResult;
        okObjectResult.Should().NotBeNull();
        var responseDocument = okObjectResult?.Value as Dictionary<string, StorageFile>;
        responseDocument.Should().NotBeNull();
        responseDocument.Should().BeEquivalentTo(storageMetaFileEditor.PreviewImages);
        mockRepository.VerifyAll();
    }

    [Fact]
    public async Task Stylesheets()
    {
        // Arrange
        var wordProcessingController = CreateWordProcessingController();
        Guid documentCode = Guid.NewGuid();
        StorageMetaFile<WordProcessingLoadOptions, WordProcessingEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo
                {
                    Format = WordProcessingFormats.Docx,
                    FamilyFormat = "fixed",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile { DocumentCode = documentCode },
                StorageSubFiles = new Dictionary<string, StorageSubFile<WordProcessingEditOptions>>
                {
                    {"0", new StorageSubFile<WordProcessingEditOptions>("fixed.docx", "0")
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
        var result = await wordProcessingController.Stylesheets(
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
        var wordProcessingController = CreateWordProcessingController();
        Guid documentCode = Guid.NewGuid();
        StorageMetaFile<WordProcessingLoadOptions, WordProcessingEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo
                {
                    Format = WordProcessingFormats.Docx,
                    FamilyFormat = "WordProcessing",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile { DocumentCode = documentCode },
                StorageSubFiles = new Dictionary<string, StorageSubFile<WordProcessingEditOptions>>
                {
                    {"0", new StorageSubFile<WordProcessingEditOptions>("fixed.docx", "0")
                    {
                        Resources = new Dictionary<string, StorageFile>
                        {
                            {"style.css", new StorageFile {FileName = "style.css", ResourceType = ResourceType.Stylesheet}}
                        }
                    }}
                }
            };
        WordProcessingStorageInfo wordProcessingStorageInfo = new()
        { DocumentCode = documentCode };
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(documentCode)).ReturnsAsync(storageMetaFile);
        _mockMapper.Setup(a => a.Map<WordProcessingStorageInfo>(storageMetaFile)).Returns(wordProcessingStorageInfo);
        // Act
        var result = await wordProcessingController.MetaInfo(
            documentCode);

        // Assert
        var okObjectResult = result as OkObjectResult;
        okObjectResult.Should().NotBeNull();
        var responseDocument = okObjectResult?.Value as WordProcessingStorageInfo;
        responseDocument.Should().NotBeNull();
        responseDocument.Should().Be(wordProcessingStorageInfo);
        mockRepository.VerifyAll();
    }

    [Fact]
    public void SupportedFormats()
    {
        // Arrange
        var wordProcessingController = CreateWordProcessingController();
        var expected = WordProcessingFormats.All.GroupBy(a => a.Extension).Select(a => a.First()).ToList();
        _mockEditorService.Setup(a => a.GetSupportedFormats<WordProcessingFormats>()).Returns(expected);
        // Act
        var result = wordProcessingController.SupportedFormats();

        // Assert
        result.Should().NotBeNull();
        var okObjectResult = result as OkObjectResult;
        okObjectResult.Should().NotBeNull();
        var data = okObjectResult?.Value as Dictionary<string, string>;
        data.Should().NotBeNull();
        data.Should().BeEquivalentTo(expected.ToDictionary(format => format.Extension, format => format.Name));
        mockRepository.VerifyAll();
    }
}