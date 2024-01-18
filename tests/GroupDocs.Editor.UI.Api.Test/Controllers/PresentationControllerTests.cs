using AutoMapper;
using FluentAssertions;
using GroupDocs.Editor.Formats;
using GroupDocs.Editor.Options;
using GroupDocs.Editor.UI.Api.Controllers;
using GroupDocs.Editor.UI.Api.Controllers.RequestModels;
using GroupDocs.Editor.UI.Api.Controllers.RequestModels.Presentation;
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

public class PresentationControllerTests
{
    private readonly MockRepository _mockRepository;

    private readonly Mock<IPresentationEditorService> _mockEditorService;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IStorage> _mockStorage;
    private readonly Mock<IPresentationStorageCache> _mockMetaFileStorageCache;

    public PresentationControllerTests()
    {
        _mockRepository = new MockRepository(MockBehavior.Strict);

        _mockEditorService = _mockRepository.Create<IPresentationEditorService>();
        _mockMapper = _mockRepository.Create<IMapper>();
        _mockStorage = _mockRepository.Create<IStorage>();
        _mockMetaFileStorageCache = _mockRepository.Create<IPresentationStorageCache>();
    }

    private PresentationController CreatePresentationController()
    {
        return new PresentationController(
            new NullLogger<PresentationController>(),
            _mockEditorService.Object,
            _mockMapper.Object,
            _mockStorage.Object,
            _mockMetaFileStorageCache.Object);
    }

    [Fact]
    public async Task CreateNewDocument()
    {
        // Arrange
        var presentationController = CreatePresentationController();
        Guid documentCode = Guid.NewGuid();
        PresentationNewDocumentRequest file = new() { FileName = "presentation.pptx", Format = PresentationFormats.Pptx.Extension };
        CreateDocumentRequest createDocumentRequest = new() { FileName = "presentation.pptx", Format = PresentationFormats.Pptx };
        StorageMetaFile<PresentationLoadOptions, PresentationEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo
                {
                    Format = PresentationFormats.Pptx,
                    FamilyFormat = "presentation",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile { DocumentCode = documentCode }
            };
        PresentationUploadResponse uploadResponse = new()
        {
            DocumentCode = documentCode,
            DocumentInfo = new StorageDocumentInfo
            {
                Format = PresentationFormats.Pptx,
                FamilyFormat = "presentation",
                IsEncrypted = false,
                PageCount = 10,
                Size = 258
            },
            OriginalFile = new StorageFile { DocumentCode = documentCode }
        };
        _mockMapper.Setup(a => a.Map<CreateDocumentRequest>(file)).Returns(createDocumentRequest);
        _mockMapper.Setup(a => a.Map<PresentationUploadResponse>(storageMetaFile)).Returns(uploadResponse);
        _mockEditorService.Setup(a => a.CreateDocument(createDocumentRequest)).ReturnsAsync(storageMetaFile);
        // Act
        var result = await presentationController.CreateNewDocument(file);

        // Assert
        result.Should().NotBeNull();
        var okObjectResult = result as OkObjectResult;
        okObjectResult.Should().NotBeNull();
        var responseDocument = okObjectResult?.Value as PresentationUploadResponse;
        responseDocument.Should().NotBeNull();
        responseDocument.Should().Be(uploadResponse);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task UploadDocument()
    {
        // Arrange
        var presentationController = CreatePresentationController();
        const string fileName = "presentation.docx";
        Guid documentCode = Guid.NewGuid();
        await using var stream = new MemoryStream();
        IFormFile formFile = new FormFile(stream, 0, stream.Length, "id_from_form", fileName);
        PresentationUploadRequest file = new() { File = formFile };
        UploadDocumentRequest uploadDocumentRequest = new() { FileName = "presentation.docx", Stream = stream };
        PresentationUploadResponse documentUploadResponse = new()
        {
            DocumentCode = documentCode,
            DocumentInfo = new StorageDocumentInfo
            {
                Format = PresentationFormats.Pptx,
                FamilyFormat = "presentation",
                IsEncrypted = false,
                PageCount = 10,
                Size = 258
            },
            OriginalFile = new StorageFile { DocumentCode = documentCode }
        };
        StorageMetaFile<PresentationLoadOptions, PresentationEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo
                {
                    Format = PresentationFormats.Pptx,
                    FamilyFormat = "presentation",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile { DocumentCode = documentCode }
            };
        _mockMapper.Setup(a => a.Map<UploadDocumentRequest>(file)).Returns(uploadDocumentRequest);
        _mockMapper.Setup(a => a.Map<PresentationUploadResponse>(storageMetaFile)).Returns(documentUploadResponse);
        _mockEditorService.Setup(a => a.UploadDocument(uploadDocumentRequest)).ReturnsAsync(storageMetaFile);
        // Act
        var result = await presentationController.Upload(file);

        // Assert
        result.Should().NotBeNull();
        var okObjectResult = result as OkObjectResult;
        okObjectResult.Should().NotBeNull();
        var responseDocument = okObjectResult?.Value as PresentationUploadResponse;
        responseDocument.Should().NotBeNull();
        responseDocument.Should().Be(documentUploadResponse);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task EditDocument_ConvertWithEditor()
    {
        // Arrange
        var presentationController = CreatePresentationController();
        Guid documentCode = Guid.NewGuid();
        PresentationEditOptions editOptions = new()
        {
            SlideNumber = 0
        };
        PresentationEditRequest request = new() { DocumentCode = documentCode, EditOptions = editOptions };
        string expectedHtml = "test html";

        StorageMetaFile<PresentationLoadOptions, PresentationEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo
                {
                    Format = PresentationFormats.Pptx,
                    FamilyFormat = "presentation",
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
        var result = await presentationController.Edit(request);

        // Assert
        result.Should().NotBeNull();
        var okObjectResult = result as OkObjectResult;
        okObjectResult.Should().NotBeNull();
        var responseDocument = okObjectResult?.Value as string;
        responseDocument.Should().NotBeNull();
        responseDocument.Should().Be(expectedHtml);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task EditDocument_WasConverted()
    {
        // Arrange
        var presentationController = CreatePresentationController();
        Guid documentCode = Guid.NewGuid();
        PresentationEditOptions editOptions = new()
        {
            SlideNumber = 0
        };
        PresentationEditRequest request = new() { DocumentCode = documentCode, EditOptions = editOptions };
        const string expectedHtml = "test html";

        StorageMetaFile<PresentationLoadOptions, PresentationEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo
                {
                    Format = PresentationFormats.Pptx,
                    FamilyFormat = "presentation",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile { DocumentCode = documentCode },
                StorageSubFiles = new Dictionary<string, StorageSubFile<PresentationEditOptions>>
                {
                        {"0", new StorageSubFile<PresentationEditOptions>("presentation.pptx", "0")
                        {
                            EditOptions = editOptions,
                            DocumentCode = documentCode
                        }}
                }
            };
        StorageResponse<string> expectedFile = StorageResponse<string>.CreateSuccess(expectedHtml);
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(documentCode)).ReturnsAsync(storageMetaFile);
        _mockStorage.Setup(a => a.GetFileText(Path.Combine(documentCode.ToString(), "0", "presentation.html")))
            .ReturnsAsync(expectedFile);
        // Act
        var result = await presentationController.Edit(request);

        // Assert
        result.Should().NotBeNull();
        var okObjectResult = result as OkObjectResult;
        okObjectResult.Should().NotBeNull();
        var responseDocument = okObjectResult?.Value as string;
        responseDocument.Should().NotBeNull();
        responseDocument.Should().Be(expectedHtml);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task DownloadDocument()
    {
        // Arrange
        var presentationController = CreatePresentationController();
        Guid documentCode = Guid.NewGuid();
        PresentationSaveOptions saveOptions = new()
        {
            SlideNumber = 0
        };
        PresentationDownloadRequest request = new()
        {
            DocumentCode = documentCode,
            Format = "pptx",
            SaveOptions = saveOptions
        };
        DownloadDocumentRequest documentRequest = new()
        {
            DocumentCode = documentCode,
            Format = "pptx",
            SaveOptions = saveOptions
        };
        using MemoryStream stream = new();
        FileContent fileContent = new()
        {
            FileName = "presentation.pptx",
            ResourceStream = stream,
            ResourceType = ResourceType.OriginalDocument
        };
        _mockMapper.Setup(a => a.Map<DownloadDocumentRequest>(request)).Returns(documentRequest);
        _mockEditorService.Setup(a => a.ConvertToDocument(documentRequest)).ReturnsAsync(fileContent);
        // Act
        var result = await presentationController.Download(request);

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
        var presentationController = CreatePresentationController();
        Guid documentCode = Guid.NewGuid();
        const string editedHtml = "newContent";
        UpdateContentRequestPaged request = new() { DocumentCode = documentCode, HtmlContents = editedHtml, SubIndex = "0" };
        PresentationEditOptions editOptions = new()
        {
            SlideNumber = 0
        };
        StorageMetaFile<PresentationLoadOptions, PresentationEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo
                {
                    Format = PresentationFormats.Pptx,
                    FamilyFormat = "presentation",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile { DocumentCode = documentCode },
                StorageSubFiles = new Dictionary<string, StorageSubFile<PresentationEditOptions>>
                {
                        {"0", new StorageSubFile<PresentationEditOptions>("presentation.pptx", "0")
                        {
                            EditOptions = editOptions,
                            DocumentCode = documentCode
                        }}
                }
            };
        StorageSubFile<PresentationEditOptions> storageSubFile =
            new("presentation.pptx", "0")
            {
                EditOptions = editOptions,
                DocumentCode = documentCode
            };
        StorageResponse<StorageSubFile<PresentationEditOptions>> storageResponse =
            StorageResponse<StorageSubFile<PresentationEditOptions>>.CreateSuccess(storageSubFile);
        StorageMetaFile<PresentationLoadOptions, PresentationEditOptions> newMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo
                {
                    Format = PresentationFormats.Pptx,
                    FamilyFormat = "presentation",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile { DocumentCode = documentCode },
                StorageSubFiles = new Dictionary<string, StorageSubFile<PresentationEditOptions>>
                {
                        {"0", storageSubFile}
                }
            };
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(documentCode)).ReturnsAsync(storageMetaFile);
        _mockMetaFileStorageCache.Setup(a => a.UpdateFiles(It.IsAny<StorageMetaFile<PresentationLoadOptions, PresentationEditOptions>>())).ReturnsAsync(newMetaFile);
        _mockEditorService
            .Setup(a => a.UpdateHtmlContent(storageMetaFile.StorageSubFiles[editOptions.SlideNumber.ToString()],
                editedHtml)).ReturnsAsync(storageResponse);
        // Act
        var result = await presentationController.Update(request);

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
        var presentationController = CreatePresentationController();
        Guid documentCode = Guid.NewGuid();
        const string fileName = "new.css";
        await using var stream = new MemoryStream();
        IFormFile formFile = new FormFile(stream, 0, stream.Length, "id_from_form", fileName);
        UploadResourceRequestPaged resource = new()
        {
            DocumentCode = documentCode,
            File = formFile,
            OldResorceName = "test.css",
            SubIndex = "0",
            ResourceType = ResourceType.Stylesheet
        };
        PresentationEditOptions editOptions = new()
        {
            SlideNumber = 0
        };
        StorageMetaFile<PresentationLoadOptions, PresentationEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo
                {
                    Format = PresentationFormats.Pptx,
                    FamilyFormat = "presentation",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile { DocumentCode = documentCode },
                StorageSubFiles = new Dictionary<string, StorageSubFile<PresentationEditOptions>>
                {
                        {"0", new StorageSubFile<PresentationEditOptions>("presentation.pptx", "0")
                        {
                            EditOptions = editOptions,
                            DocumentCode = documentCode
                        }}
                }
            };
        StorageFile storageFile = new() { DocumentCode = documentCode, FileName = fileName, ResourceType = ResourceType.Stylesheet };
        StorageSubFile<PresentationEditOptions> storageSub =
            new StorageSubFile<PresentationEditOptions>(documentCode.ToString(), "0");
        StorageUpdateResourceResponse<StorageSubFile<PresentationEditOptions>, StorageFile>
            storageUpdateResourceResponse =
                StorageUpdateResourceResponse<StorageSubFile<PresentationEditOptions>, StorageFile>.CreateSuccess(
                    storageSub, storageFile);
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(documentCode)).ReturnsAsync(storageMetaFile);
        _mockEditorService.Setup(a => a.UpdateResource(storageMetaFile.StorageSubFiles["0"], resource))
            .ReturnsAsync(storageUpdateResourceResponse);
        _mockMetaFileStorageCache.Setup(a => a.UpdateFiles(It.IsAny<StorageMetaFile<PresentationLoadOptions, PresentationEditOptions>>())).ReturnsAsync(storageMetaFile);

        // Act
        var result = await presentationController.UploadResource(resource);

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
        var presentationController = CreatePresentationController();
        Guid documentCode = Guid.NewGuid();
        StorageFile storageFile = new() { DocumentCode = documentCode, FileName = "preview", ResourceType = ResourceType.Preview };
        StorageMetaFile<PresentationLoadOptions, PresentationEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo
                {
                    Format = PresentationFormats.Pptx,
                    FamilyFormat = "presentation",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile { DocumentCode = documentCode }
            };
        StorageMetaFile<PresentationLoadOptions, PresentationEditOptions> storageMetaFileEditor =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo
                {
                    Format = PresentationFormats.Pptx,
                    FamilyFormat = "presentation",
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
        var result = await presentationController.Previews(documentCode);

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
        var presentationController = CreatePresentationController();
        Guid documentCode = Guid.NewGuid();
        const int slideNumber = 0;
        StorageMetaFile<PresentationLoadOptions, PresentationEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo
                {
                    Format = PresentationFormats.Pptx,
                    FamilyFormat = "presentation",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile { DocumentCode = documentCode },
                StorageSubFiles = new Dictionary<string, StorageSubFile<PresentationEditOptions>>
                {
                        {"0", new StorageSubFile<PresentationEditOptions>("presentation,pptx", "0")
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
        var result = await presentationController.Stylesheets(documentCode, slideNumber);

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
        var presentationController = CreatePresentationController();
        Guid documentCode = Guid.NewGuid();
        StorageMetaFile<PresentationLoadOptions, PresentationEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new StorageDocumentInfo
                {
                    Format = PresentationFormats.Pptx,
                    FamilyFormat = "presentation",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new StorageFile { DocumentCode = documentCode },
                StorageSubFiles = new Dictionary<string, StorageSubFile<PresentationEditOptions>>
                {
                        {"0", new StorageSubFile<PresentationEditOptions>("presentation,pptx", "0")
                        {
                            Resources = new Dictionary<string, StorageFile>
                            {
                                {"style.css", new StorageFile {FileName = "style.css", ResourceType = ResourceType.Stylesheet}}
                            }
                        }}
                }
            };
        PresentationStorageInfo presentationStorageInfo = new PresentationStorageInfo() { DocumentCode = documentCode };
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(documentCode)).ReturnsAsync(storageMetaFile);
        _mockMapper.Setup(a => a.Map<PresentationStorageInfo>(storageMetaFile)).Returns(presentationStorageInfo);
        // Act
        var result = await presentationController.MetaInfo(documentCode);

        // Assert
        result.Should().NotBeNull();
        var okObjectResult = result as OkObjectResult;
        okObjectResult.Should().NotBeNull();
        var responseDocument = okObjectResult?.Value as PresentationStorageInfo;
        responseDocument.Should().NotBeNull();
        responseDocument.Should().Be(presentationStorageInfo);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public void SupportedFormats()
    {
        // Arrange
        var presentationController = CreatePresentationController();
        var expected = PresentationFormats.All.GroupBy(a => a.Extension).Select(a => a.First()).ToList();
        _mockEditorService.Setup(a => a.GetSupportedFormats<PresentationFormats>()).Returns(expected);
        // Act
        var result = presentationController.SupportedFormats();

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