using AutoMapper;
using FluentAssertions;
using GroupDocs.Editor.Formats;
using GroupDocs.Editor.Options;
using GroupDocs.Editor.UI.Api.Controllers;
using GroupDocs.Editor.UI.Api.Controllers.RequestModels;
using GroupDocs.Editor.UI.Api.Controllers.RequestModels.Email;
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

public class EmailControllerTests
{
    private readonly MockRepository mockRepository;

    private readonly Mock<IEmailEditorService> _mockEditorService;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IStorage> _mockStorage;
    private readonly Mock<IEmailStorageCache> _mockMetaFileStorageCache;

    public EmailControllerTests()
    {
        mockRepository = new(MockBehavior.Strict);

        _mockEditorService = mockRepository.Create<IEmailEditorService>();
        _mockMapper = mockRepository.Create<IMapper>();
        _mockStorage = mockRepository.Create<IStorage>();
        _mockMetaFileStorageCache = mockRepository.Create<IEmailStorageCache>();
    }

    private EmailController CreateEmailController()
    {
        return new(
            new NullLogger<EmailController>(),
            _mockEditorService.Object,
            _mockMapper.Object,
            _mockStorage.Object,
            _mockMetaFileStorageCache.Object);
    }

    [Fact]
    public async Task UploadDocument()
    {
        // Arrange
        var controller = CreateEmailController();
        const string fileName = "document.eml";
        Guid documentCode = Guid.NewGuid();
        await using var stream = new MemoryStream();
        IFormFile formFile = new FormFile(stream, 0, stream.Length, "id_from_form", fileName);
        EmailUploadRequest file = new()
        {
            File = formFile
        };
        UploadDocumentRequest uploadDocumentRequest = new() { FileName = "document.eml", Stream = stream };
        EmailUploadResponse documentUploadResponse = new()
        {
            DocumentCode = documentCode,
            DocumentInfo = new()
            {
                Format = EmailFormats.Eml,
                FamilyFormat = "Email",
                IsEncrypted = false,
                PageCount = 10,
                Size = 258
            },
            OriginalFile = new() { DocumentCode = documentCode }
        };
        StorageMetaFile<ILoadOptions, EmailEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new()
                {
                    Format = EmailFormats.Eml,
                    FamilyFormat = "Email",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new() { DocumentCode = documentCode }
            };
        _mockMapper.Setup(a => a.Map<UploadDocumentRequest>(file)).Returns(uploadDocumentRequest);
        _mockMapper.Setup(a => a.Map<EmailUploadResponse>(storageMetaFile)).Returns(documentUploadResponse);
        _mockEditorService.Setup(a => a.UploadDocument(uploadDocumentRequest)).ReturnsAsync(storageMetaFile);
        // Act
        var result = await controller.Upload(
            file);

        // Assert
        result.Should().NotBeNull();
        var okObjectResult = result as OkObjectResult;
        okObjectResult.Should().NotBeNull();
        var responseDocument = okObjectResult?.Value as EmailUploadResponse;
        responseDocument.Should().NotBeNull();
        responseDocument.Should().Be(documentUploadResponse);
        mockRepository.VerifyAll();
    }

    [Fact]
    public async Task NewDocument()
    {
        // Arrange
        var controller = CreateEmailController();
        Guid documentCode = Guid.NewGuid();
        EmailNewDocumentRequest file = new()
        {
            FileName = "document.Eml",
            Format = EmailFormats.Eml.Extension
        };
        CreateDocumentRequest createDocumentRequest = new() { FileName = "document.Eml", Format = EmailFormats.Eml };
        StorageMetaFile<ILoadOptions, EmailEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new()
                {
                    Format = EmailFormats.Eml,
                    FamilyFormat = "Email",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new() { DocumentCode = documentCode }
            };
        EmailUploadResponse uploadResponse = new()
        {
            DocumentCode = documentCode,
            DocumentInfo = new()
            {
                Format = EmailFormats.Eml,
                FamilyFormat = "Email",
                IsEncrypted = false,
                PageCount = 10,
                Size = 258
            },
            OriginalFile = new() { DocumentCode = documentCode }
        };
        _mockMapper.Setup(a => a.Map<CreateDocumentRequest>(file)).Returns(createDocumentRequest);
        _mockMapper.Setup(a => a.Map<EmailUploadResponse>(storageMetaFile)).Returns(uploadResponse);
        _mockEditorService.Setup(a => a.CreateDocument(createDocumentRequest)).ReturnsAsync(storageMetaFile);
        // Act
        var result = await controller.NewDocument(file);

        // Assert
        result.Should().NotBeNull();
        var okObjectResult = result as OkObjectResult;
        okObjectResult.Should().NotBeNull();
        var responseDocument = okObjectResult?.Value as EmailUploadResponse;
        responseDocument.Should().NotBeNull();
        responseDocument.Should().Be(uploadResponse);
        mockRepository.VerifyAll();
    }

    [Fact]
    public async Task EditDocument_ConvertWithEditor()
    {
        // Arrange
        var controller = CreateEmailController();
        Guid documentCode = Guid.NewGuid();
        EmailEditOptions editOptions = new();
        EmailEditRequest request = new() { DocumentCode = documentCode, EditOptions = editOptions };
        using var expectedHtml = new MemoryStream();

        StorageMetaFile<ILoadOptions, EmailEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new()
                {
                    Format = EmailFormats.Eml,
                    FamilyFormat = "Email",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new() { DocumentCode = documentCode }
            };
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(documentCode)).ReturnsAsync(storageMetaFile);
        _mockEditorService.Setup(a => a.ConvertToHtml(storageMetaFile, editOptions, null))
            .ReturnsAsync(expectedHtml);
        // Act
        var result = await controller.Edit(
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
        var controller = CreateEmailController();
        Guid documentCode = Guid.NewGuid();
        EmailEditOptions editOptions = new();
        EmailEditRequest request = new() { DocumentCode = documentCode, EditOptions = editOptions };
        using var expectedHtml = new MemoryStream();

        StorageMetaFile<ILoadOptions, EmailEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new()
                {
                    Format = EmailFormats.Eml,
                    FamilyFormat = "Email",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new() { DocumentCode = documentCode },
                StorageSubFiles = new Dictionary<string, StorageSubFile<EmailEditOptions>>
                {
                    {"0", new StorageSubFile<EmailEditOptions>("fixed.Eml", "0")
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
        var result = await controller.Edit(request);

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
        var controller = CreateEmailController();
        Guid documentCode = Guid.NewGuid();
        EmailSaveOptions saveOptions = new();
        EmailDownloadRequest request = new() { DocumentCode = documentCode, Format = "Eml", SaveOptions = saveOptions };
        DownloadDocumentRequest documentRequest = new()
        {
            DocumentCode = documentCode,
            Format = "Eml",
            SaveOptions = saveOptions
        };
        using MemoryStream stream = new();
        FileContent fileContent = new()
        {
            FileName = "fixed.Eml",
            ResourceStream = stream,
            ResourceType = ResourceType.OriginalDocument
        };
        _mockMapper.Setup(a => a.Map<DownloadDocumentRequest>(request)).Returns(documentRequest);
        _mockEditorService.Setup(a => a.ConvertToDocument(documentRequest)).ReturnsAsync(fileContent);
        // Act
        var result = await controller.Download(
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
        var controller = CreateEmailController();
        Guid documentCode = Guid.NewGuid();
        const string editedHtml = "newContent";
        UpdateContentRequest request = new() { DocumentCode = documentCode, HtmlContents = editedHtml };
        EmailEditOptions editOptions = new();
        StorageMetaFile<ILoadOptions, EmailEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new()
                {
                    Format = EmailFormats.Eml,
                    FamilyFormat = "Email",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new() { DocumentCode = documentCode },
                StorageSubFiles = new Dictionary<string, StorageSubFile<EmailEditOptions>>
                {
                    {"0", new StorageSubFile<EmailEditOptions>("fixed.eml", "0")
                    {
                        EditOptions = editOptions,
                        DocumentCode = documentCode
                    }}
                }
            };
        StorageSubFile<EmailEditOptions> storageSubFile =
            new("fixed.eml", "0")
            {
                EditOptions = editOptions,
                DocumentCode = documentCode
            };
        StorageResponse<StorageSubFile<EmailEditOptions>> storageResponse =
            StorageResponse<StorageSubFile<EmailEditOptions>>.CreateSuccess(storageSubFile);
        StorageMetaFile<ILoadOptions, EmailEditOptions> newMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new()
                {
                    Format = EmailFormats.Eml,
                    FamilyFormat = "Email",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new() { DocumentCode = documentCode },
                StorageSubFiles = new Dictionary<string, StorageSubFile<EmailEditOptions>>
                {
                    {"0", storageSubFile}
                }
            };
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(documentCode)).ReturnsAsync(storageMetaFile);
        _mockMetaFileStorageCache.Setup(a => a.UpdateFiles(It.IsAny<StorageMetaFile<ILoadOptions, EmailEditOptions>>())).ReturnsAsync(newMetaFile);
        _mockEditorService
            .Setup(a => a.UpdateHtmlContent(storageMetaFile.StorageSubFiles["0"],
                editedHtml)).ReturnsAsync(storageResponse);
        // Act
        var result = await controller.Update(
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
        var controller = CreateEmailController();
        Guid documentCode = Guid.NewGuid();
        const string fileName = "new.css";
        await using var stream = new MemoryStream();
        IFormFile formFile = new FormFile(stream, 0, stream.Length, "id_from_form", fileName);
        UploadResourceRequest resource = new() { DocumentCode = documentCode, File = formFile, OldResourceName = "test.css", ResourceType = ResourceType.Stylesheet };
        EmailEditOptions editOptions = new();
        StorageMetaFile<ILoadOptions, EmailEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new()
                {
                    Format = EmailFormats.Eml,
                    FamilyFormat = "Email",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new() { DocumentCode = documentCode },
                StorageSubFiles = new Dictionary<string, StorageSubFile<EmailEditOptions>>
                {
                    {"0", new StorageSubFile<EmailEditOptions>("fixed.eml", "0")
                    {
                        EditOptions = editOptions,
                        DocumentCode = documentCode
                    }}
                }
            };
        StorageFile storageFile = new() { DocumentCode = documentCode, FileName = fileName, ResourceType = ResourceType.Stylesheet };
        StorageSubFile<EmailEditOptions> storageSub = new(documentCode.ToString(), "0");
        StorageUpdateResourceResponse<StorageSubFile<EmailEditOptions>, StorageFile>
            storageUpdateResourceResponse =
                StorageUpdateResourceResponse<StorageSubFile<EmailEditOptions>, StorageFile>.CreateSuccess(
                    storageSub, storageFile);
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(documentCode)).ReturnsAsync(storageMetaFile);
        _mockEditorService.Setup(a => a.UpdateResource(storageMetaFile.StorageSubFiles["0"], resource))
            .ReturnsAsync(storageUpdateResourceResponse);
        _mockMetaFileStorageCache.Setup(a => a.UpdateFiles(It.IsAny<StorageMetaFile<ILoadOptions, EmailEditOptions>>())).ReturnsAsync(storageMetaFile);

        // Act
        var result = await controller.UploadResource(resource);

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
        var controller = CreateEmailController();
        Guid documentCode = Guid.NewGuid();
        StorageMetaFile<ILoadOptions, EmailEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new()
                {
                    Format = EmailFormats.Eml,
                    FamilyFormat = "fixed",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new() { DocumentCode = documentCode },
                StorageSubFiles = new Dictionary<string, StorageSubFile<EmailEditOptions>>
                {
                    {"0", new StorageSubFile<EmailEditOptions>("fixed.eml", "0")
                    {
                        Resources = new()
                        {
                            {"style.css", new() {FileName = "style.css", ResourceType = ResourceType.Stylesheet}}
                        }
                    }}
                }
            };
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(documentCode)).ReturnsAsync(storageMetaFile);

        // Act
        var result = await controller.Stylesheets(
            documentCode);

        // Assert
        result.Should().NotBeNull();
        var okObjectResult = result as OkObjectResult;
        okObjectResult.Should().NotBeNull();
        var responseDocument = okObjectResult?.Value as IEnumerable<StorageFile>;
        IEnumerable<StorageFile> storageFiles = responseDocument as StorageFile[] ?? responseDocument?.ToArray() ?? [];
        storageFiles.Should().NotBeNull();
        storageFiles.Should().BeEquivalentTo(new List<StorageFile> { new() { FileName = "style.css", ResourceType = ResourceType.Stylesheet } });
        mockRepository.VerifyAll();
    }

    [Fact]
    public async Task MetaInfo()
    {
        // Arrange
        var controller = CreateEmailController();
        Guid documentCode = Guid.NewGuid();
        StorageMetaFile<ILoadOptions, EmailEditOptions> storageMetaFile =
            new()
            {
                DocumentCode = documentCode,
                DocumentInfo = new()
                {
                    Format = EmailFormats.Eml,
                    FamilyFormat = "Email",
                    IsEncrypted = false,
                    PageCount = 10,
                    Size = 258
                },
                OriginalFile = new() { DocumentCode = documentCode },
                StorageSubFiles = new Dictionary<string, StorageSubFile<EmailEditOptions>>
                {
                    {"0", new StorageSubFile<EmailEditOptions>("fixed.eml", "0")
                    {
                        Resources = new()
                        {
                            {"style.css", new() {FileName = "style.css", ResourceType = ResourceType.Stylesheet}}
                        }
                    }}
                }
            };
        EmailStorageInfo EmailStorageInfo = new() { DocumentCode = documentCode };
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(documentCode)).ReturnsAsync(storageMetaFile);
        _mockMapper.Setup(a => a.Map<EmailStorageInfo>(storageMetaFile)).Returns(EmailStorageInfo);
        // Act
        var result = await controller.MetaInfo(
            documentCode);

        // Assert
        var okObjectResult = result as OkObjectResult;
        okObjectResult.Should().NotBeNull();
        var responseDocument = okObjectResult?.Value as EmailStorageInfo;
        responseDocument.Should().NotBeNull();
        responseDocument.Should().Be(EmailStorageInfo);
        mockRepository.VerifyAll();
    }

    [Fact]
    public void SupportedFormats()
    {
        // Arrange
        var controller = CreateEmailController();
        var expected = EmailFormats.All.GroupBy(a => a.Extension).Select(a => a.First()).ToList();
        _mockEditorService.Setup(a => a.GetSupportedFormats<EmailFormats>()).Returns(expected);
        // Act
        var result = controller.SupportedFormats();

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