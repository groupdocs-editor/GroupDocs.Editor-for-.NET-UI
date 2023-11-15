using FluentAssertions;
using GroupDocs.Editor.UI.Api.Models.DocumentConvertor;
using GroupDocs.Editor.UI.Api.Models.Storage;
using GroupDocs.Editor.UI.Api.Models.Storage.Requests;
using GroupDocs.Editor.UI.Api.Models.Storage.Responses;
using GroupDocs.Editor.UI.Api.Services.Implementation;
using GroupDocs.Editor.UI.Api.Services.Interfaces;
using GroupDocs.Editor.UI.Api.Services.Options;
using GroupDocs.Editor.UI.Api.Test.SetupApp;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GroupDocs.Editor.UI.Api.Test.Services.Implementation
{
    public class AzureBlobServiceTests
    {
        private readonly AzureBlobStorage _storage;
        private readonly MockRepository _mockRepository;
        private readonly Mock<IIdGeneratorService> _mockIdGeneratorService;

        private readonly AzureBlobOptions _azureBlobOptions;
        public AzureBlobServiceTests()
        {
            _mockRepository = new MockRepository(MockBehavior.Strict);
            _mockIdGeneratorService = _mockRepository.Create<IIdGeneratorService>();
            var azureConfiguration = TestConfigHelper.IConfiguration().BuildAzureTestOption();
            _azureBlobOptions = new()
            {
                AccountKey = (string.IsNullOrWhiteSpace(azureConfiguration.AccountKey)
                    ? Environment.GetEnvironmentVariable("EDITOR_AZURE_KEY")
                    : azureConfiguration.AccountKey) ?? "",
                AccountName = (string.IsNullOrWhiteSpace(azureConfiguration.AccountName)
                    ? Environment.GetEnvironmentVariable("EDITOR_AZURE_NAME")
                    : azureConfiguration.AccountName) ?? "",
                ContainerName = (string.IsNullOrWhiteSpace(azureConfiguration.ContainerName)
                    ? Environment.GetEnvironmentVariable("EDITOR_AZURE_CONTAINER")
                    : azureConfiguration.ContainerName) ?? "",
                LinkExpiresDays = 360
            };
            _storage = new AzureBlobStorage(
                Microsoft.Extensions.Options.Options.Create(_azureBlobOptions),
                _mockIdGeneratorService.Object,
                new NullLogger<AzureBlobStorage>());
        }

        [Fact]
        public async Task UploadDownloadRemoveOneFile()
        {
            const string filename = "WordProcessing.docx";
            string filePath = Path.Combine("TestFiles", filename);
            File.Exists(filePath).Should().BeTrue();

            await using FileStream file = File.OpenRead(filePath);
            UploadOriginalRequest inputWrapper = new()
            {
                DocumentInfo = new StorageDocumentInfo
                {
                    Format = Formats.WordProcessingFormats.FromExtension(filename),
                    IsEncrypted = false,
                    PageCount = 1,
                    Size = 100500
                },
                FileContent = new FileContent { FileName = filename, ResourceStream = file }
            };
            Guid code = Guid.NewGuid();
            _mockIdGeneratorService.Setup(a => a.GenerateDocumentCode()).Returns(code);
            List<UploadOriginalRequest> inputWrapperList = new(1) { inputWrapper };
            IEnumerable<StorageResponse<StorageMetaFile>> result = await _storage.UploadFiles(inputWrapperList);
            var storageResponses = result.ToList();
            storageResponses.Should().NotBeNullOrEmpty().And.HaveCount(1);
            storageResponses.First().IsSuccess.Should().BeTrue();
            storageResponses.First().Status.Should().Be(StorageActionStatus.Success);

            using (StorageDisposableResponse<Stream> downloaded = await _storage.DownloadFile($"{code}/{filename}"))
            {
                downloaded.Should().NotBeNull();
                downloaded.IsSuccess.Should().Be(true);
                downloaded.Status.Should().Be(StorageActionStatus.Success);
                downloaded.Response.Should().NotBeNull().And.BeOfType<MemoryStream>();
                downloaded.Response?.CanRead.Should().BeTrue();
                downloaded.Response?.CanSeek.Should().BeTrue();
                downloaded.Response?.Position.Should().BeGreaterOrEqualTo(0);
            }

            StorageResponse deletedStatus = await _storage.RemoveFile($"{code}/{filename}");
            deletedStatus.Should().NotBeNull();
            deletedStatus.IsSuccess.Should().BeTrue();
            deletedStatus.Status.Should().Be(StorageActionStatus.Success);
            _mockRepository.VerifyAll();
        }

        [Fact]
        public async void OnlyDownloadExistingFile()
        {
            const string filename = "WordProcessing.docx";
            using StorageDisposableResponse<Stream> downloaded = await _storage.DownloadFile(filename);
            downloaded.Should().NotBeNull();
            downloaded.Status.Should().BeOneOf(StorageActionStatus.Success, StorageActionStatus.NotExist);
            if (downloaded.Status != StorageActionStatus.Success) return;
            downloaded.Response.Should().NotBeNull().And.BeOfType<MemoryStream>();
            downloaded.Response?.CanRead.Should().BeTrue();
            downloaded.Response?.CanSeek.Should().BeTrue();
            downloaded.Response?.Length.Should().BeGreaterThan(0);
            downloaded.Response?.Position.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task OnlyDeleteExistingFile()
        {
            const string filename = "WordProcessing.docx";
            StorageResponse deletedStatus = await _storage.RemoveFile(filename);
            deletedStatus.Should().NotBeNull();
            deletedStatus.Status.Should().BeOneOf(StorageActionStatus.Success, StorageActionStatus.NotExist);
        }

        [Fact]
        public async Task CreateAndRemoveFolder()
        {
            const string folderName = "Folder";
            string folderPrefix = folderName + "/";

            const string wordFilename = "WordProcessing.docx";
            string wordPath = Path.Combine("TestFiles", wordFilename);
            File.Exists(wordPath).Should().BeTrue();

            const string excelFilename = "Spreadsheet.xlsx";
            string excelPath = Path.Combine("TestFiles", excelFilename);
            File.Exists(excelPath).Should().BeTrue();

            await using FileStream wordStream = File.OpenRead(wordPath);
            await using FileStream excelStream = File.OpenRead(excelPath);
            UploadOriginalRequest wordUploadWrapper = new()
            {
                DocumentInfo = new StorageDocumentInfo
                {
                    Format = Formats.WordProcessingFormats.FromExtension(wordFilename),
                    IsEncrypted = false,
                    PageCount = 1,
                    Size = 100500
                },
                FileContent = new FileContent
                {
                    FileName = folderPrefix + wordFilename,
                    ResourceStream = wordStream
                }
            };
            UploadOriginalRequest excelUploadWrapper = new()
            {
                DocumentInfo = new StorageDocumentInfo
                {
                    Format = Formats.SpreadsheetFormats.FromExtension(excelFilename),
                    IsEncrypted = false,
                    PageCount = 1,
                    Size = 100500
                },
                FileContent = new FileContent
                {
                    FileName = folderPrefix + excelFilename,
                    ResourceStream = excelStream
                }
            };
            Guid code = Guid.NewGuid();
            _mockIdGeneratorService.Setup(a => a.GenerateDocumentCode()).Returns(code);
            List<UploadOriginalRequest> inputWrapperList = new List<UploadOriginalRequest>(2) { wordUploadWrapper, excelUploadWrapper };
            IEnumerable<StorageResponse<StorageMetaFile>> result = await _storage.UploadFiles(inputWrapperList);
            var storageResponses = result.ToList();
            storageResponses.Should().NotBeNullOrEmpty().And.HaveCount(2);
            storageResponses.First().Should().NotBeNull();
            storageResponses.Last().Should().NotBeNull();
            storageResponses.First().Status.Should().Be(StorageActionStatus.Success);
            storageResponses.Last().Status.Should().Be(StorageActionStatus.Success);

            StorageResponse deletionResult = await _storage.RemoveFolder($"{code}/{folderName}");
            deletionResult.Should().NotBeNull();
            deletionResult.IsSuccess.Should().BeTrue();
            deletionResult.Status.Should().Be(StorageActionStatus.Success);
            _mockRepository.VerifyAll();
        }
    }
}
