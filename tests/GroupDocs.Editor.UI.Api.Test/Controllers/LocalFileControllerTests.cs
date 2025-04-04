﻿using FluentAssertions;
using GroupDocs.Editor.UI.Api.Controllers;
using GroupDocs.Editor.UI.Api.Models.Storage;
using GroupDocs.Editor.UI.Api.Models.Storage.Responses;
using GroupDocs.Editor.UI.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GroupDocs.Editor.UI.Api.Test.Controllers;

public class LocalFileControllerTests
{
    private readonly MockRepository _mockRepository;

    private readonly Mock<IStorage> _mockStorage;

    public LocalFileControllerTests()
    {
        _mockRepository = new MockRepository(MockBehavior.Strict);

        _mockStorage = _mockRepository.Create<IStorage>();
    }

    private LocalFileController CreateLocalFileController()
    {
        return new LocalFileController(
            new NullLogger<LocalFileController>(),
            _mockStorage.Object);
    }

    [Fact]
    public async Task DownloadFromSubDocument()
    {
        // Arrange
        LocalFileController localFileController = CreateLocalFileController();
        Guid documentCode = Guid.NewGuid();
        const int subDocumentIndex = 0;
        const string fileName = "WordProcessing.docx";
        await using MemoryStream stream = new();
        _mockStorage
            .Setup(a => a.DownloadFile(It.IsAny<PathBuilder>()))
            .ReturnsAsync(StorageDisposableResponse<Stream>.CreateSuccess(stream));
        // Act
        IActionResult result = await localFileController.DownloadFromSubDocument(
            documentCode,
            subDocumentIndex,
            fileName);

        // Assert
        result.Should().NotBeNull();
        FileStreamResult? file = result as FileStreamResult;
        file.Should().NotBeNull();
        file?.FileStream.Should().BeSameAs(stream);
        file?.FileDownloadName.Should().Be(fileName);
        file?.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task DownloadPresentationFromSubDocument()
    {
        // Arrange
        LocalFileController localFileController = CreateLocalFileController();
        Guid documentCode = Guid.NewGuid();
        const int subDocumentIndex = 0;
        const string fileName = "Presentation.pptx";
        await using MemoryStream stream = new();
        _mockStorage
            .Setup(a => a.DownloadFile(It.IsAny<PathBuilder>()))
            .ReturnsAsync(StorageDisposableResponse<Stream>.CreateSuccess(stream));
        // Act
        IActionResult result = await localFileController.DownloadFromSubDocument(
            documentCode,
            subDocumentIndex,
            fileName);

        // Assert
        result.Should().NotBeNull();
        FileStreamResult? file = result as FileStreamResult;
        file.Should().NotBeNull();
        file?.FileStream.Should().BeSameAs(stream);
        file?.FileDownloadName.Should().Be(fileName);
        file?.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.presentationml.presentation");
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task Download()
    {
        // Arrange
        LocalFileController localFileController = CreateLocalFileController();
        Guid documentCode = Guid.NewGuid();
        const string fileName = "WordProcessing.docx";
        await using MemoryStream stream = new();
        _mockStorage.Setup(a => a.DownloadFile(It.IsAny<PathBuilder>())).ReturnsAsync(StorageDisposableResponse<Stream>.CreateSuccess(stream));

        // Act
        IActionResult result = await localFileController.Download(
            documentCode,
            fileName);

        // Assert
        result.Should().NotBeNull();
        FileStreamResult? file = result as FileStreamResult;
        file.Should().NotBeNull();
        file?.FileStream.Should().BeSameAs(stream);
        file?.FileDownloadName.Should().Be(fileName);
        file?.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        _mockRepository.VerifyAll();
    }
}