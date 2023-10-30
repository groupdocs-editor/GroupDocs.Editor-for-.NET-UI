using AutoMapper;
using FluentAssertions;
using GroupDocs.Editor.Formats;
using GroupDocs.Editor.Options;
using GroupDocs.Editor.UI.Api.Controllers;
using GroupDocs.Editor.UI.Api.Controllers.RequestModels;
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
    private readonly MockRepository _mockRepository;

    private readonly Mock<IEditorService> _mockEditorService;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IStorage> _mockStorage;
    private readonly Mock<IMetaFileStorageCache> _mockMetaFileStorageCache;

    public WordProcessingControllerTests()
    {
        _mockRepository = new(MockBehavior.Strict);

        _mockEditorService = _mockRepository.Create<IEditorService>();
        _mockMapper = _mockRepository.Create<IMapper>();
        _mockStorage = _mockRepository.Create<IStorage>();
        _mockMetaFileStorageCache = _mockRepository.Create<IMetaFileStorageCache>();
    }

    private WordProcessingController CreateWordProcessingController()
    {
        return new(
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
        const string fileName = "WordProcessing.docx";
        await using var stream = new MemoryStream();
        IFormFile file = new FormFile(stream, 0, stream.Length, "id_from_form", fileName);

        UploadWordProcessingRequest fileRequest = new()
        {
            EditOptions = new(true),
            LoadOptions = new(),
            File = file
        };

        StorageMetaFile metaFileExpected = new();
        SaveDocumentRequest mockRequest = new();
        _mockMapper.Setup(a => a.Map<SaveDocumentRequest>(fileRequest)).Returns(mockRequest);
        _mockEditorService.Setup(a => a.SaveDocument(mockRequest)).ReturnsAsync(metaFileExpected);
        // Act
        var result = await wordProcessingController.Upload(fileRequest);

        // Assert
        result.Should().NotBeNull();
        var data = result as OkObjectResult;
        data.Should().NotBeNull();
        var response = data?.Value as StorageMetaFile;
        response.Should().NotBeNull();
        response.Should().BeEquivalentTo(metaFileExpected);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task DownloadDocument()
    {
        // Arrange
        var wordProcessingController = CreateWordProcessingController();
        WordProcessingDownloadRequest request = new()
        {
            DocumentCode = Guid.NewGuid(),
            Format = "doc",
            LoadOptions = new WordProcessingLoadOptions(),
            SaveOptions = new WordProcessingSaveOptions(WordProcessingFormats.Doc) { EnablePagination = true }
        };
        DownloadDocumentRequest mockRequest = new()
        {
            DocumentCode = Guid.NewGuid(),
            Format = "doc",
            LoadOptions = new WordProcessingLoadOptions(),
            SaveOptions = new WordProcessingSaveOptions(WordProcessingFormats.Doc) { EnablePagination = true }
        };
        _mockMapper.Setup(a => a.Map<DownloadDocumentRequest>(request)).Returns(mockRequest);
        await using var stream = new MemoryStream();
        FileContent expectedContent = new() { FileName = "WordProcessing.doc", ResourceStream = stream };

        _mockEditorService.Setup(a => a.ConvertToDocument(mockRequest)).ReturnsAsync(expectedContent);
        // Act
        var result = await wordProcessingController.Download(request);

        // Assert
        var file = result as FileStreamResult;
        file.Should().NotBeNull();
        file?.FileStream.Should().BeSameAs(stream);
        file?.FileDownloadName.Should().Be(expectedContent.FileName);
        file?.ContentType.Should().Be("application/msword");
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task UpdateUpdateHtmlContent()
    {
        // Arrange
        var wordProcessingController = CreateWordProcessingController();
        UpdateContentRequest request = new() { DocumentCode = Guid.NewGuid(), HtmlContents = "test", SubIndex = 0 };
        StorageSubFile subFile = new();
        StorageMetaFile meteFile = new()
        {
            DocumentCode = request.DocumentCode,
            StorageSubFiles = new Dictionary<int, StorageSubFile>
            {
                { request.SubIndex, subFile },
            }
        };
        _mockMetaFileStorageCache.Setup(a => a.UpdateFiles(meteFile)).ReturnsAsync(meteFile);
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(request.DocumentCode)).ReturnsAsync(meteFile);
        _mockStorage.Setup(a => a.UpdateHtmlContent(subFile, request.HtmlContents)).ReturnsAsync(StorageResponse<StorageSubFile>.CreateSuccess(subFile));
        // Act
        var result = await wordProcessingController.Update(request);

        // Assert
        var okObject = result as NoContentResult;
        okObject?.Should().NotBeNull();
        okObject?.StatusCode.Should().Be(204);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task UploadResource()
    {
        // Arrange
        var wordProcessingController = CreateWordProcessingController();
        using MemoryStream stream = new();
        IFormFile file = new FormFile(stream, 0, stream.Length, "id_from_form", "WordProcessing.doc");
        UploadResourceRequest request = new()
        {
            DocumentCode = Guid.NewGuid(),
            File = file,
            ResourceType = ResourceType.Image,
            SubIndex = 0
        };
        StorageFile storageFile = new();
        StorageSubFile subFile = new();
        StorageMetaFile meteFile = new()
        {
            DocumentCode = request.DocumentCode,
            StorageSubFiles = new Dictionary<int, StorageSubFile>
            {
                { request.SubIndex, subFile },
            }
        };
        _mockStorage.Setup(a => a.UpdateResource(subFile, request))
            .ReturnsAsync(StorageUpdateResourceResponse<StorageSubFile, StorageFile>.CreateSuccess(subFile, storageFile));
        _mockMetaFileStorageCache.Setup(a => a.UpdateFiles(meteFile)).ReturnsAsync(meteFile);
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(request.DocumentCode)).ReturnsAsync(meteFile);
        // Act
        var result = await wordProcessingController.UploadResource(request);

        // Assert
        result.Should().NotBeNull();
        var okObject = result as NoContentResult;
        okObject?.Should().NotBeNull();
        okObject?.StatusCode.Should().Be(204);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task PreviewImages_ImagesConverted()
    {
        // Arrange
        var wordProcessingController = CreateWordProcessingController();
        PreviewRequest request = new PreviewRequest()
        {
            DocumentCode = Guid.NewGuid(),
            LoadOptions = new WordProcessingLoadOptions()
        };
        StorageSubFile subFile = new();
        StorageFile previewFile = new()
        { DocumentCode = request.DocumentCode, FileName = "0.svg", FileLink = "http://s.com" };
        StorageMetaFile meteFile = new()
        {
            DocumentCode = request.DocumentCode,
            StorageSubFiles = new Dictionary<int, StorageSubFile>
            {
                { 0, subFile },
            },
            PreviewImages = new Dictionary<int, StorageFile>() { { 0, previewFile } }
        };
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(request.DocumentCode)).ReturnsAsync(meteFile);
        // Act
        var result = await wordProcessingController.PreviewImages(request);

        // Assert
        result.Should().NotBeNull();
        var data = result as OkObjectResult;
        data.Should().NotBeNull();
        var response = data?.Value as Dictionary<int, StorageFile>;
        response.Should().NotBeNull();
        response.Should().BeEquivalentTo(new Dictionary<int, StorageFile>() { { 0, previewFile } });
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task Stylesheets_StateUnderTest_ExpectedBehavior()
    {
        // Arrange
        var wordProcessingController = CreateWordProcessingController();
        StylesheetsRequest request = new()
        {
            DocumentCode = Guid.NewGuid(),
            SubIndex = 0
        };
        StorageSubFile subFile = new()
        {
            Stylesheets = new List<StorageFile>
                {new() {DocumentCode = request.DocumentCode, FileName = "style.css", FileLink = "http://s.com"}}
        };
        StorageMetaFile meteFile = new()
        {
            DocumentCode = request.DocumentCode,
            StorageSubFiles = new Dictionary<int, StorageSubFile>
            {
                { 0, subFile },
            },
        };
        _mockMetaFileStorageCache.Setup(a => a.DownloadFile(request.DocumentCode)).ReturnsAsync(meteFile);
        // Act
        var result = await wordProcessingController.Stylesheets(request);

        // Assert
        result.Should().NotBeNull();
        var data = result as OkObjectResult;
        data.Should().NotBeNull();
        var response = data?.Value as ICollection<StorageFile>;
        response.Should().NotBeNull();
        response.Should().BeEquivalentTo(subFile.Stylesheets);
        _mockRepository.VerifyAll();
    }
}