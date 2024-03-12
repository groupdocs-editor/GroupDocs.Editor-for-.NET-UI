using AutoMapper;
using FluentAssertions;
using GroupDocs.Editor.Formats;
using GroupDocs.Editor.Options;
using GroupDocs.Editor.UI.Api.Controllers;
using GroupDocs.Editor.UI.Api.Controllers.RequestModels;
using GroupDocs.Editor.UI.Api.Controllers.RequestModels.Spreadsheet;
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

public class SpreadsheetControllerTests
{
    private readonly MockRepository _mockRepository;

    private readonly Mock<ISpreadsheetEditorService> _mockEditorService;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IStorage> _mockStorage;
    private readonly Mock<ISpreadsheetStorageCache> _mockMetaFileStorageCache;

    public SpreadsheetControllerTests()
    {
        _mockRepository = new MockRepository(MockBehavior.Strict);

        _mockEditorService = _mockRepository.Create<ISpreadsheetEditorService>();
        _mockMapper = _mockRepository.Create<IMapper>();
        _mockStorage = _mockRepository.Create<IStorage>();
        _mockMetaFileStorageCache = _mockRepository.Create<ISpreadsheetStorageCache>();
    }

    private SpreadsheetController CreateController()
    {
        return new SpreadsheetController(
            new NullLogger<SpreadsheetController>(),
            _mockEditorService.Object,
            _mockMapper.Object,
            _mockStorage.Object,
            _mockMetaFileStorageCache.Object);
    }

    [Fact]
    public async Task CreateNewDocument()
    {
        // Arrange
        var controller = CreateController();
        Guid documentCode = Guid.NewGuid();
        SpreadsheetNewDocumentRequest file = new() { FileName = "Spreadsheet.xlsx", Format = SpreadsheetFormats.Xlsx.Extension };
        CreateDocumentRequest createDocumentRequest = new() { FileName = "Spreadsheet.xlsx", Format = SpreadsheetFormats.Xlsx };
        StorageMetaFile<SpreadsheetLoadOptions, SpreadsheetEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo()
                {
                    Format = SpreadsheetFormats.Xlsx,
                    FamilyFormat = "Spreadsheet",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile() { DocumentCode = documentCode }
            };
        SpreadsheetUploadResponse uploadResponse = new()
        {
            DocumentCode = documentCode,
            DocumentInfo = new StorageDocumentInfo()
            {
                Format = SpreadsheetFormats.Xlsx,
                FamilyFormat = "Spreadsheet",
                IsEncrypted = false,
                PageCount = 10,
                Size = 258
            },
            OriginalFile = new StorageFile() { DocumentCode = documentCode }
        };
        _mockMapper.Setup(a => a.Map<CreateDocumentRequest>(file)).Returns(createDocumentRequest);
        _mockMapper.Setup(a => a.Map<SpreadsheetUploadResponse>(storageMetaFile)).Returns(uploadResponse);
        _mockEditorService.Setup(a => a.CreateDocument(createDocumentRequest)).ReturnsAsync(storageMetaFile);
        // Act
        var result = await controller.CreateNewDocument(file);

        // Assert
        result.Should().NotBeNull();
        var okObjectResult = result as OkObjectResult;
        okObjectResult.Should().NotBeNull();
        var responseDocument = okObjectResult?.Value as SpreadsheetUploadResponse;
        responseDocument.Should().NotBeNull();
        responseDocument.Should().Be(uploadResponse);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task UploadDocument()
    {
        // Arrange
        var controller = CreateController();
        const string fileName = "Spreadsheet.xlsx";
        Guid documentCode = Guid.NewGuid();
        await using var stream = new MemoryStream();
        IFormFile formFile = new FormFile(stream, 0, stream.Length, "id_from_form", fileName);
        SpreadsheetUploadRequest file = new() { File = formFile };
        UploadDocumentRequest uploadDocumentRequest = new() { FileName = "Spreadsheet.xlsx", Stream = stream };
        SpreadsheetUploadResponse documentUploadResponse = new()
        {
            DocumentCode = documentCode,
            DocumentInfo = new StorageDocumentInfo()
            {
                Format = SpreadsheetFormats.Xlsx,
                FamilyFormat = "Spreadsheet",
                IsEncrypted = false,
                PageCount = 10,
                Size = 258
            },
            OriginalFile = new StorageFile() { DocumentCode = documentCode }
        };
        StorageMetaFile<SpreadsheetLoadOptions, SpreadsheetEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo()
                {
                    Format = SpreadsheetFormats.Xlsx,
                    FamilyFormat = "Spreadsheet",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile() { DocumentCode = documentCode }
            };
        _mockMapper.Setup(a => a.Map<UploadDocumentRequest>(file)).Returns(uploadDocumentRequest);
        _mockMapper.Setup(a => a.Map<SpreadsheetUploadResponse>(storageMetaFile)).Returns(documentUploadResponse);
        _mockEditorService.Setup(a => a.UploadDocument(uploadDocumentRequest)).ReturnsAsync(storageMetaFile);
        // Act
        var result = await controller.Upload(file);

        // Assert
        result.Should().NotBeNull();
        var okObjectResult = result as OkObjectResult;
        okObjectResult.Should().NotBeNull();
        var responseDocument = okObjectResult?.Value as SpreadsheetUploadResponse;
        responseDocument.Should().NotBeNull();
        responseDocument.Should().Be(documentUploadResponse);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task EditDocument_ConvertWithEditor()
    {
        // Arrange
        var controller = CreateController();
        Guid documentCode = Guid.NewGuid();
        SpreadsheetEditOptions editOptions = new()
        {
            WorksheetIndex = 0
        };
        SpreadsheetEditRequest request = new() { DocumentCode = documentCode, EditOptions = editOptions };
        using var expectedHtml = new MemoryStream();

        StorageMetaFile<SpreadsheetLoadOptions, SpreadsheetEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo()
                {
                    Format = SpreadsheetFormats.Xlsx,
                    FamilyFormat = "Spreadsheet",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile() { DocumentCode = documentCode }
            };
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(documentCode)).ReturnsAsync(storageMetaFile);
        _mockEditorService.Setup(a => a.ConvertToHtml(storageMetaFile, editOptions, null))
            .ReturnsAsync(expectedHtml);
        // Act
        var result = await controller.Edit(request);

        // Assert
        result.Should().NotBeNull();
        var okObjectResult = result as FileStreamResult;
        okObjectResult.Should().NotBeNull();
        var responseDocument = okObjectResult?.FileStream;
        responseDocument.Should().NotBeNull();
        responseDocument.Should().BeSameAs(expectedHtml);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task EditDocument_WasConverted()
    {
        // Arrange
        var controller = CreateController();
        Guid documentCode = Guid.NewGuid();
        SpreadsheetEditOptions editOptions = new()
        {
            WorksheetIndex = 0
        };
        SpreadsheetEditRequest request = new() { DocumentCode = documentCode, EditOptions = editOptions };
        using var expectedHtml = new MemoryStream();

        StorageMetaFile<SpreadsheetLoadOptions, SpreadsheetEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo()
                {
                    Format = SpreadsheetFormats.Xlsx,
                    FamilyFormat = "Spreadsheet",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile() { DocumentCode = documentCode },
                StorageSubFiles = new Dictionary<string, StorageSubFile<SpreadsheetEditOptions>>
                {
                        {"0", new StorageSubFile<SpreadsheetEditOptions>("Spreadsheet.xlsx", "0")
                        {
                            EditOptions = editOptions,
                            DocumentCode = documentCode
                        }}
                }
            };
        StorageDisposableResponse<Stream> expectedFile = StorageDisposableResponse<Stream>.CreateSuccess(expectedHtml);
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(documentCode)).ReturnsAsync(storageMetaFile);
        _mockStorage.Setup(a => a.DownloadFile(Path.Combine(documentCode.ToString(), "0", "Spreadsheet.html")))
            .ReturnsAsync(expectedFile);
        // Act
        var result = await controller.Edit(request);

        // Assert
        result.Should().NotBeNull();
        var okObjectResult = result as FileStreamResult;
        okObjectResult.Should().NotBeNull();
        var responseDocument = okObjectResult?.FileStream;
        responseDocument.Should().NotBeNull();
        responseDocument.Should().BeSameAs(expectedHtml);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task DownloadDocument()
    {
        // Arrange
        var controller = CreateController();
        Guid documentCode = Guid.NewGuid();
        SpreadsheetSaveOptions saveOptions = new()
        {
            WorksheetNumber = 0
        };
        SpreadsheetDownloadRequest request = new()
        {
            DocumentCode = documentCode,
            Format = "xlsx",
            SaveOptions = saveOptions
        };
        DownloadDocumentRequest documentRequest = new()
        {
            DocumentCode = documentCode,
            Format = "xlsx",
            SaveOptions = saveOptions
        };
        using MemoryStream stream = new();
        FileContent fileContent = new()
        {
            FileName = "Spreadsheet.xlsx",
            ResourceStream = stream,
            ResourceType = ResourceType.OriginalDocument
        };
        _mockMapper.Setup(a => a.Map<DownloadDocumentRequest>(request)).Returns(documentRequest);
        _mockEditorService.Setup(a => a.ConvertToDocument(documentRequest)).ReturnsAsync(fileContent);
        // Act
        var result = await controller.Download(request);

        // Assert
        result.Should().NotBeNull();
        var fileStream = result as FileStreamResult;
        fileStream.Should().NotBeNull();
        fileStream?.FileStream.Should().BeSameAs(stream);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task UpdateDocument()
    {
        // Arrange
        var controller = CreateController();
        Guid documentCode = Guid.NewGuid();
        const string editedHtml = "newContent";
        UpdateContentRequestPaged request = new() { DocumentCode = documentCode, HtmlContents = editedHtml, SubIndex = "0" };
        SpreadsheetEditOptions editOptions = new()
        {
            WorksheetIndex = 0
        };
        StorageMetaFile<SpreadsheetLoadOptions, SpreadsheetEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo()
                {
                    Format = SpreadsheetFormats.Xlsx,
                    FamilyFormat = "Spreadsheet",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile() { DocumentCode = documentCode },
                StorageSubFiles = new Dictionary<string, StorageSubFile<SpreadsheetEditOptions>>
                {
                        {"0", new StorageSubFile<SpreadsheetEditOptions>("Spreadsheet.xlsx", "0")
                        {
                            EditOptions = editOptions,
                            DocumentCode = documentCode
                        }}
                }
            };
        StorageSubFile<SpreadsheetEditOptions> storageSubFile =
            new("Spreadsheet.xlsx", "0")
            {
                EditOptions = editOptions,
                DocumentCode = documentCode
            };
        StorageResponse<StorageSubFile<SpreadsheetEditOptions>> storageResponse =
            StorageResponse<StorageSubFile<SpreadsheetEditOptions>>.CreateSuccess(storageSubFile);
        StorageMetaFile<SpreadsheetLoadOptions, SpreadsheetEditOptions> newMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo()
                {
                    Format = SpreadsheetFormats.Xlsx,
                    FamilyFormat = "Spreadsheet",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile() { DocumentCode = documentCode },
                StorageSubFiles = new Dictionary<string, StorageSubFile<SpreadsheetEditOptions>>
                {
                        {"0", storageSubFile}
                }
            };
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(documentCode)).ReturnsAsync(storageMetaFile);
        _mockMetaFileStorageCache.Setup(a => a.UpdateFiles(It.IsAny<StorageMetaFile<SpreadsheetLoadOptions, SpreadsheetEditOptions>>())).ReturnsAsync(newMetaFile);
        _mockEditorService
            .Setup(a => a.UpdateHtmlContent(storageMetaFile.StorageSubFiles[editOptions.WorksheetIndex.ToString()],
                editedHtml)).ReturnsAsync(storageResponse);
        // Act
        var result = await controller.Update(request);

        // Assert
        result.Should().NotBeNull();
        var fileStream = result as NoContentResult;
        fileStream.Should().NotBeNull();
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task UploadResource()
    {
        // Arrange
        var controller = CreateController();
        Guid documentCode = Guid.NewGuid();
        const string fileName = "new.css";
        await using var stream = new MemoryStream();
        IFormFile formFile = new FormFile(stream, 0, stream.Length, "id_from_form", fileName);
        UploadResourceRequestPaged resource = new()
        {
            DocumentCode = documentCode,
            File = formFile,
            OldResourceName = "test.css",
            SubIndex = "0",
            ResourceType = ResourceType.Stylesheet
        };
        SpreadsheetEditOptions editOptions = new()
        {
            WorksheetIndex = 0
        };
        StorageMetaFile<SpreadsheetLoadOptions, SpreadsheetEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo()
                {
                    Format = SpreadsheetFormats.Xlsx,
                    FamilyFormat = "Spreadsheet",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile() { DocumentCode = documentCode },
                StorageSubFiles = new Dictionary<string, StorageSubFile<SpreadsheetEditOptions>>
                {
                        {"0", new StorageSubFile<SpreadsheetEditOptions>("Spreadsheet.xlsx", "0")
                        {
                            EditOptions = editOptions,
                            DocumentCode = documentCode
                        }}
                }
            };
        StorageFile storageFile = new() { DocumentCode = documentCode, FileName = fileName, ResourceType = ResourceType.Stylesheet };
        StorageSubFile<SpreadsheetEditOptions> storageSub = new(documentCode.ToString(), "0");
        StorageUpdateResourceResponse<StorageSubFile<SpreadsheetEditOptions>, StorageFile>
            storageUpdateResourceResponse =
                StorageUpdateResourceResponse<StorageSubFile<SpreadsheetEditOptions>, StorageFile>.CreateSuccess(
                    storageSub, storageFile);
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(documentCode)).ReturnsAsync(storageMetaFile);
        _mockEditorService.Setup(a => a.UpdateResource(storageMetaFile.StorageSubFiles["0"], resource))
            .ReturnsAsync(storageUpdateResourceResponse);
        _mockMetaFileStorageCache.Setup(a => a.UpdateFiles(It.IsAny<StorageMetaFile<SpreadsheetLoadOptions, SpreadsheetEditOptions>>())).ReturnsAsync(storageMetaFile);

        // Act
        var result = await controller.UploadResource(resource);

        // Assert
        result.Should().NotBeNull();
        var okObjectResult = result as OkObjectResult;
        okObjectResult.Should().NotBeNull();
        var responseDocument = okObjectResult?.Value as StorageFile;
        responseDocument.Should().NotBeNull();
        responseDocument.Should().Be(storageFile);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task Previews()
    {
        // Arrange
        var controller = CreateController();
        Guid documentCode = Guid.NewGuid();
        StorageFile storageFile = new() { DocumentCode = documentCode, FileName = "preview", ResourceType = ResourceType.Preview };
        StorageMetaFile<SpreadsheetLoadOptions, SpreadsheetEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo()
                {
                    Format = SpreadsheetFormats.Xlsx,
                    FamilyFormat = "Spreadsheet",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile() { DocumentCode = documentCode }
            };
        StorageMetaFile<SpreadsheetLoadOptions, SpreadsheetEditOptions> storageMetaFileEditor =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo()
                {
                    Format = SpreadsheetFormats.Xlsx,
                    FamilyFormat = "Spreadsheet",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile() { DocumentCode = documentCode },
                PreviewImages = new Dictionary<string, StorageFile>
                {
                        {"0", storageFile}
                }
            };
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(documentCode)).ReturnsAsync(storageMetaFile);
        _mockEditorService.Setup(a => a.ConvertPreviews(documentCode)).ReturnsAsync(storageMetaFileEditor);
        // Act
        var result = await controller.Previews(documentCode);

        // Assert
        result.Should().NotBeNull();
        var okObjectResult = result as OkObjectResult;
        okObjectResult.Should().NotBeNull();
        var responseDocument = okObjectResult?.Value as Dictionary<string, StorageFile>;
        responseDocument.Should().NotBeNull();
        responseDocument.Should().BeEquivalentTo(storageMetaFileEditor.PreviewImages);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task Stylesheets()
    {
        // Arrange
        var controller = CreateController();
        Guid documentCode = Guid.NewGuid();
        const int slideNumber = 0;
        StorageMetaFile<SpreadsheetLoadOptions, SpreadsheetEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo()
                {
                    Format = SpreadsheetFormats.Xlsx,
                    FamilyFormat = "Spreadsheet",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile() { DocumentCode = documentCode },
                StorageSubFiles = new Dictionary<string, StorageSubFile<SpreadsheetEditOptions>>
                {
                        {"0", new StorageSubFile<SpreadsheetEditOptions>("Spreadsheet.xlsx", "0")
                        {
                            Resources = new Dictionary<string, StorageFile>()
                            {
                                {"style.css", new() {FileName = "style.css", ResourceType = ResourceType.Stylesheet}}
                            }
                        }}
                }
            };
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(documentCode)).ReturnsAsync(storageMetaFile);

        // Act
        var result = await controller.Stylesheets(documentCode, slideNumber);

        // Assert
        result.Should().NotBeNull();
        var okObjectResult = result as OkObjectResult;
        okObjectResult.Should().NotBeNull();
        var responseDocument = okObjectResult?.Value as IEnumerable<StorageFile>;
        IEnumerable<StorageFile> storageFiles = responseDocument as StorageFile[] ?? responseDocument?.ToArray() ?? Array.Empty<StorageFile>();
        storageFiles.Should().NotBeNull();
        storageFiles.Should().BeEquivalentTo(new List<StorageFile> { new() { FileName = "style.css", ResourceType = ResourceType.Stylesheet } });
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task MetaInfo()
    {
        // Arrange
        var controller = CreateController();
        Guid documentCode = Guid.NewGuid();
        StorageMetaFile<SpreadsheetLoadOptions, SpreadsheetEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo()
                {
                    Format = SpreadsheetFormats.Xlsx,
                    FamilyFormat = "Spreadsheet",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile() { DocumentCode = documentCode },
                StorageSubFiles = new Dictionary<string, StorageSubFile<SpreadsheetEditOptions>>
                {
                        {"0", new StorageSubFile<SpreadsheetEditOptions>("Spreadsheet.xlsx", "0")
                        {
                            Resources = new Dictionary<string, StorageFile>()
                            {
                                {"style.css", new() {FileName = "style.css", ResourceType = ResourceType.Stylesheet}}
                            }
                        }}
                }
            };
        SpreadsheetStorageInfo SpreadsheetStorageInfo = new() { DocumentCode = documentCode };
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(documentCode)).ReturnsAsync(storageMetaFile);
        _mockMapper.Setup(a => a.Map<SpreadsheetStorageInfo>(storageMetaFile)).Returns(SpreadsheetStorageInfo);
        // Act
        var result = await controller.MetaInfo(documentCode);

        // Assert
        result.Should().NotBeNull();
        var okObjectResult = result as OkObjectResult;
        okObjectResult.Should().NotBeNull();
        var responseDocument = okObjectResult?.Value as SpreadsheetStorageInfo;
        responseDocument.Should().NotBeNull();
        responseDocument.Should().Be(SpreadsheetStorageInfo);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public void SupportedFormats()
    {
        // Arrange
        var controller = CreateController();
        var expected = SpreadsheetFormats.All.GroupBy(a => a.Extension).Select(a => a.First()).ToList();
        _mockEditorService.Setup(a => a.GetSupportedFormats<SpreadsheetFormats>()).Returns(expected);
        // Act
        var result = controller.SupportedFormats();

        // Assert
        result.Should().NotBeNull();
        var okObjectResult = result as OkObjectResult;
        okObjectResult.Should().NotBeNull();
        var data = okObjectResult?.Value as Dictionary<string, string>;
        data.Should().NotBeNull();
        data.Should().BeEquivalentTo(expected.ToDictionary(format => format.Extension, format => format.Name));
        _mockRepository.VerifyAll();
    }
}