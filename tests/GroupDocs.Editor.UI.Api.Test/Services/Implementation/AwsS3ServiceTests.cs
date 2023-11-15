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
    public class AwsS3ServiceTests : IDisposable
    {
        private readonly AwsS3Storage _storage;
        private readonly AwsOptions _options;
        private readonly MockRepository _mockRepository;
        private readonly Mock<IIdGeneratorService> _mockIdGeneratorService;

        public AwsS3ServiceTests()
        {
            _mockRepository = new MockRepository(MockBehavior.Strict);
            _mockIdGeneratorService = _mockRepository.Create<IIdGeneratorService>();
            var awsConfiguration = TestConfigHelper.IConfiguration().BuildAwsTestOption();
            _options = new()
            {
                Bucket = (string.IsNullOrWhiteSpace(awsConfiguration.Bucket) ? Environment.GetEnvironmentVariable("EDITOR_AWS_BUCKET") : awsConfiguration.Bucket) ?? "",
                AccessKey = (string.IsNullOrWhiteSpace(awsConfiguration.AccessKey) ? Environment.GetEnvironmentVariable("EDITOR_AWS_KEY") : awsConfiguration.AccessKey) ?? "",
                LinkExpiresDays = 1,
                Profile = "",
                Region = (string.IsNullOrWhiteSpace(awsConfiguration.Region) ? Environment.GetEnvironmentVariable("EDITOR_AWS_REGION") : awsConfiguration.Region) ?? "",
                RootFolderName = "groupdocseditorui",
                SecretKey = (string.IsNullOrWhiteSpace(awsConfiguration.SecretKey) ? Environment.GetEnvironmentVariable("EDITOR_AWS_SECRETKEY") : awsConfiguration.SecretKey) ?? ""
            };
            _storage = new AwsS3Storage(
                new NullLogger<AwsS3Storage>(),
                 _mockIdGeneratorService.Object,
                 Microsoft.Extensions.Options.Options.Create(_options)
                );
        }

        [Fact]
        public async Task UploadDownloadRemoveOneFile()
        {
            const string filename = "WordProcessing.docx";
            string filePath = Path.Combine("TestFiles", filename);
            File.Exists(filePath).Should().BeTrue();
            Guid code = Guid.NewGuid();
            _mockIdGeneratorService.Setup(a => a.GenerateDocumentCode()).Returns(code);
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
            List<UploadOriginalRequest> inputWrapperList = new(1) { inputWrapper };

            IEnumerable<StorageResponse<StorageMetaFile>> result = await _storage.UploadFiles(inputWrapperList);
            result.Should().NotBeNullOrEmpty().And.HaveCount(1);
            _mockRepository.VerifyAll();

            using StorageDisposableResponse<Stream> downloaded = await _storage.DownloadFile($"{_options.RootFolderName}/{code}/{filename}");
            downloaded.Should().NotBeNull();
            downloaded.IsSuccess.Should().Be(true);
            downloaded.Status.Should().Be(StorageActionStatus.Success);
            downloaded.Response.Should().NotBeNull().And.BeAssignableTo<MemoryStream>();
            downloaded.Response?.CanRead.Should().BeTrue();
            downloaded.Response?.CanSeek.Should().BeTrue();
            downloaded.Response?.Position.Should().BeGreaterThan(0);

            StorageResponse deletedStatus = await _storage.RemoveFile($"{_options.RootFolderName}/{code}/{filename}");
            deletedStatus.Should().NotBeNull();
            deletedStatus.IsSuccess.Should().BeTrue();
            deletedStatus.Status.Should().Be(StorageActionStatus.Success);
        }

        [Fact]
        public async Task CreateAndRemoveFolder()
        {
            const string folderName = "Folder";
            const string folderPrefix = folderName + "/";

            const string wordFilename = "WordProcessing.docx";
            string wordPath = Path.Combine("TestFiles", wordFilename);
            File.Exists(wordPath).Should().BeTrue();

            const string excelFilename = "Spreadsheet.xlsx";
            string excelPath = Path.Combine("TestFiles", excelFilename);
            File.Exists(excelPath).Should().BeTrue();
            Guid code = Guid.NewGuid();
            _mockIdGeneratorService.Setup(a => a.GenerateDocumentCode()).Returns(code);
            await using (FileStream wordStream = File.OpenRead(wordPath))
            await using (FileStream excelStream = File.OpenRead(excelPath))
            {
                UploadOriginalRequest wordUploadWrapper = new()
                {
                    DocumentInfo = new()
                    {
                        Format = Formats.WordProcessingFormats.FromExtension(wordFilename),
                        IsEncrypted = false,
                        PageCount = 1,
                        Size = 100500
                    },
                    FileContent = new()
                    {
                        FileName = folderPrefix + wordFilename,
                        ResourceStream = wordStream
                    }
                };
                UploadOriginalRequest excelUploadWrapper = new()
                {
                    DocumentInfo = new()
                    {
                        Format = Formats.SpreadsheetFormats.FromExtension(excelFilename),
                        IsEncrypted = false,
                        PageCount = 1,
                        Size = 100500
                    },
                    FileContent = new()
                    {
                        FileName = folderPrefix + excelFilename,
                        ResourceStream = excelStream
                    }
                };

                List<UploadOriginalRequest> inputWrapperList = new(2) { wordUploadWrapper, excelUploadWrapper };
                IEnumerable<StorageResponse<StorageMetaFile>> result = await _storage.UploadFiles(inputWrapperList);
                result.Should().NotBeNullOrEmpty().And.HaveCount(2);

            }

            StorageResponse deletionResult = await _storage.RemoveFolder($"{_options.RootFolderName}/{code}/{folderName}");
            deletionResult.Should().NotBeNull();
            deletionResult.IsSuccess.Should().BeTrue();
            deletionResult.Status.Should().Be(StorageActionStatus.Success);


        }

        [Fact]
        public async Task RemoveNotExisting()
        {
            StorageResponse deletionNotExistantFolderResult = await _storage.RemoveFolder("Abcde_notExists");
            deletionNotExistantFolderResult.Should().NotBeNull();
            deletionNotExistantFolderResult.IsSuccess.Should().BeFalse();
            deletionNotExistantFolderResult.Status.Should().Be(StorageActionStatus.NotExist);

            StorageResponse detetionNotExistantFileResult = await _storage.RemoveFile("Abcd_NotExistantFile");
            detetionNotExistantFileResult.Should().NotBeNull();
            detetionNotExistantFileResult.IsSuccess.Should().BeFalse();
            detetionNotExistantFileResult.Status.Should().Be(StorageActionStatus.NotExist);
        }

        [Fact]
        public async void DownloadNotExistantFile()
        {
            const string filename = "Abcdef_NotExistantFilename";
            using StorageDisposableResponse<Stream> downloaded = await _storage.DownloadFile(filename);
            downloaded.Should().NotBeNull();
            downloaded.IsSuccess.Should().BeFalse();
            downloaded.Status.Should().Be(StorageActionStatus.NotExist);
            downloaded.Response.Should().NotBeNull();
            downloaded.Response.Should().BeSameAs(Stream.Null);
        }

        [Fact]
        public async void UploadSameFileTwice()
        {
            const string wordFilename = "WordProcessing.docx";
            string wordPath = Path.Combine("TestFiles", wordFilename);
            File.Exists(wordPath).Should().BeTrue();

            await using FileStream wordStream = File.OpenRead(wordPath);
            wordStream.Position.Should().Be(0);
            wordStream.CanSeek.Should().BeTrue();
            wordStream.CanRead.Should().BeTrue();

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
                    FileName = wordFilename,
                    ResourceStream = wordStream
                }
            };
            Guid code = Guid.NewGuid();
            _mockIdGeneratorService.Setup(a => a.GenerateDocumentCode()).Returns(code);
            List<UploadOriginalRequest> inputWrapperList = new(1) { wordUploadWrapper };
            IEnumerable<StorageResponse<StorageMetaFile>> result = await _storage.UploadFiles(inputWrapperList);
            var storageResponses = result.ToList();
            storageResponses.Should().NotBeNullOrEmpty().And.HaveCount(1);

            wordStream.CanSeek.Should().BeTrue();
            wordStream.CanRead.Should().BeTrue();

            StorageResponse<StorageMetaFile> single = storageResponses.Single();
            single.Should().NotBeNull();
            single.Status.Should().Be(StorageActionStatus.Success);
            single.IsSuccess.Should().BeTrue();

            single.Response.Should().NotBeNull();
            single.Response?.OriginalFile.Should().NotBeNull();

            //2nd attempt to upload previously uploaded file
            await using FileStream wordStream2 = File.OpenRead(wordPath);
            UploadOriginalRequest wordUploadWrapper2 = new()
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
                    FileName = wordFilename,
                    ResourceStream = wordStream2
                }
            };

            List<UploadOriginalRequest> inputWrapperList2 = new(1) { wordUploadWrapper2 };
            IEnumerable<StorageResponse<StorageMetaFile>> result2 = await _storage.UploadFiles(inputWrapperList2);
            var storageResponses2 = result2.ToList();
            storageResponses2.Should().NotBeNullOrEmpty().And.HaveCount(1);
            storageResponses2.Single().Should().NotBeNull();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            _storage.Dispose();
        }
    }
}
