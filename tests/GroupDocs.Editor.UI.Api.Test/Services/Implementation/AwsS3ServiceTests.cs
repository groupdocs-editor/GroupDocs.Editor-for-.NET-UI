using Amazon.S3;
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

namespace GroupDocs.Editor.UI.Api.Test.Services.Implementation
{
    public class AwsS3ServiceTests : IDisposable
    {        
        private readonly AwsS3Storage _storage;
        private readonly AwsOptions _options;
        private readonly IdGeneratorService _idGeneratorService;

        public AwsS3ServiceTests()
        {
            var awsConfiguration = TestConfigHelper.IConfiguration().BuildAwsTestOption();

            AmazonS3Client amazonS3Client = new AmazonS3Client(
                awsConfiguration.AccessKey, 
                awsConfiguration.SecretKey,
                Amazon.RegionEndpoint.GetBySystemName(awsConfiguration.Region)
                );
            this._options = new AwsOptions { BucketName = awsConfiguration.Bucket };
            this._idGeneratorService = new IdGeneratorService();
            this._storage = new AwsS3Storage(
                amazonS3Client,
                new NullLogger<AwsS3Storage>(),
                 this._idGeneratorService,
                 Microsoft.Extensions.Options.Options.Create(this._options)
                );
        }

        [Fact]
        public async void UploadDownloadRemoveOneFile()
        {
            const string filename = "WordProcessing.docx";
            string filePath = Path.Combine("TestFiles", filename);
            File.Exists(filePath).Should().BeTrue();

            using (FileStream file = File.OpenRead(filePath))
            {
                UploadOriginalRequest inputWrapper = new UploadOriginalRequest()
                {
                    DocumentInfo = new StorageDocumentInfo() {
                        Format = GroupDocs.Editor.Formats.WordProcessingFormats.FromExtension(filename),
                        IsEncrypted = false,
                        PageCount = 1,
                        Size = 100500
                    },
                    FileContent = new FileContent() { FileName = filename, ResourceStream = file }
                };
                List<UploadOriginalRequest> inputWrapperList = new List<UploadOriginalRequest>(1) { inputWrapper };
                IEnumerable<StorageResponse<StorageMetaFile>> result = await this._storage.UploadFiles(inputWrapperList);
                result.Should().NotBeNullOrEmpty().And.HaveCount(1);
            }

            using (StorageDisposableResponse<Stream> downloaded = await this._storage.DownloadFile(filename))
            {
                downloaded.Should().NotBeNull();
                downloaded.IsSuccess.Should().Be(true);
                downloaded.Status.Should().Be(StorageActionStatus.Success);
                downloaded.Response.Should().NotBeNull().And.BeOfType<MemoryStream>();
                downloaded.Response.CanRead.Should().BeTrue();
                downloaded.Response.CanSeek.Should().BeTrue();
                downloaded.Response.Position.Should().BeGreaterThan(0);
            }

            StorageResponse deletedStatus = this._storage.RemoveFile(filename);
            deletedStatus.Should().NotBeNull();
            deletedStatus.IsSuccess.Should().BeTrue();
            deletedStatus.Status.Should().Be(StorageActionStatus.Success);
        }

        [Fact]
        public async void CreateAndRemoveFolder()
        {
            const string folderName = "Folder";
            string folderPrefix = folderName + "/";

            const string wordFilename = "WordProcessing.docx";
            string wordPath = Path.Combine("TestFiles", wordFilename);
            File.Exists(wordPath).Should().BeTrue();

            const string excelFilename = "Spreadsheet.xlsx";
            string excelPath = Path.Combine("TestFiles", excelFilename);
            File.Exists(excelPath).Should().BeTrue();

            using (FileStream wordStream = File.OpenRead(wordPath))
            using (FileStream excelStream = File.OpenRead(excelPath))
            {
                UploadOriginalRequest wordUploadWrapper = new UploadOriginalRequest()
                {
                    DocumentInfo = new StorageDocumentInfo()
                    {
                        Format = GroupDocs.Editor.Formats.WordProcessingFormats.FromExtension(wordFilename),
                        IsEncrypted = false,
                        PageCount = 1,
                        Size = 100500
                    },
                    FileContent = new FileContent() { 
                        FileName = folderPrefix + wordFilename, 
                        ResourceStream = wordStream }
                };
                UploadOriginalRequest excelUploadWrapper = new UploadOriginalRequest()
                {
                    DocumentInfo = new StorageDocumentInfo()
                    {
                        Format = GroupDocs.Editor.Formats.SpreadsheetFormats.FromExtension(excelFilename),
                        IsEncrypted = false,
                        PageCount = 1,
                        Size = 100500
                    },
                    FileContent = new FileContent()
                    {
                        FileName = folderPrefix + excelFilename,
                        ResourceStream = excelStream
                    }
                };
                List<UploadOriginalRequest> inputWrapperList = new List<UploadOriginalRequest>(2) { wordUploadWrapper, excelUploadWrapper };
                IEnumerable<StorageResponse<StorageMetaFile>> result = await this._storage.UploadFiles(inputWrapperList);
                result.Should().NotBeNullOrEmpty().And.HaveCount(2);

            }

            StorageResponse deletionResult = this._storage.RemoveFolder(folderName);
            deletionResult.Should().NotBeNull();
            deletionResult.IsSuccess.Should().BeTrue();
            deletionResult.Status.Should().Be(StorageActionStatus.Success);

            
        }

        [Fact]
        public void RemoveNotExisting()
        {
            StorageResponse deletionNotExistantFolderResult = this._storage.RemoveFolder("Abcde_notExists");
            deletionNotExistantFolderResult.Should().NotBeNull();
            deletionNotExistantFolderResult.IsSuccess.Should().BeFalse();
            deletionNotExistantFolderResult.Status.Should().Be(StorageActionStatus.NotExist);

            StorageResponse detetionNotExistantFileResult = this._storage.RemoveFile("Abcd_NotExistantFile");
            detetionNotExistantFileResult.Should().NotBeNull();
            detetionNotExistantFileResult.IsSuccess.Should().BeFalse();
            detetionNotExistantFileResult.Status.Should().Be(StorageActionStatus.NotExist);
        }

        [Fact]
        public async void DownloadNotExistantFile()
        {
            const string filename = "Abcdef_NotExistantFilename";
            using (StorageDisposableResponse<Stream> downloaded = await this._storage.DownloadFile(filename))
            {
                downloaded.Should().NotBeNull();
                downloaded.IsSuccess.Should().BeFalse();
                downloaded.Status.Should().Be(StorageActionStatus.NotExist);
                downloaded.Response.Should().NotBeNull();
                downloaded.Response.Should().BeSameAs(Stream.Null);
            }
        }

        [Fact]
        public async void UploadSameFileTwice()
        {
            const string wordFilename = "WordProcessing.docx";
            string wordPath = Path.Combine("TestFiles", wordFilename);
            File.Exists(wordPath).Should().BeTrue();

            using (FileStream wordStream = File.OpenRead(wordPath))
            {
                wordStream.Position.Should().Be(0);
                wordStream.CanSeek.Should().BeTrue();
                wordStream.CanRead.Should().BeTrue();

                UploadOriginalRequest wordUploadWrapper = new UploadOriginalRequest()
                {
                    DocumentInfo = new StorageDocumentInfo()
                    {
                        Format = GroupDocs.Editor.Formats.WordProcessingFormats.FromExtension(wordFilename),
                        IsEncrypted = false,
                        PageCount = 1,
                        Size = 100500
                    },
                    FileContent = new FileContent()
                    {
                        FileName = wordFilename,
                        ResourceStream = wordStream
                    }
                };

                List<UploadOriginalRequest> inputWrapperList = new List<UploadOriginalRequest>(1) { wordUploadWrapper };
                IEnumerable<StorageResponse<StorageMetaFile>> result = await this._storage.UploadFiles(inputWrapperList);
                result.Should().NotBeNullOrEmpty().And.HaveCount(1);

                wordStream.CanSeek.Should().BeFalse();
                wordStream.CanRead.Should().BeFalse();

                StorageResponse<StorageMetaFile> single = result.Single();
                single.Should().NotBeNull();
                single.Status.Should().Be(StorageActionStatus.Success);
                single.IsSuccess.Should().BeTrue();

                single.Response.Should().NotBeNull();
                single.Response.OriginalFile.Should().NotBeNull();
            }

            //2nd attempt to upload previously uploaded file
            using (FileStream wordStream = File.OpenRead(wordPath))
            {
                UploadOriginalRequest wordUploadWrapper = new UploadOriginalRequest()
                {
                    DocumentInfo = new StorageDocumentInfo()
                    {
                        Format = GroupDocs.Editor.Formats.WordProcessingFormats.FromExtension(wordFilename),
                        IsEncrypted = false,
                        PageCount = 1,
                        Size = 100500
                    },
                    FileContent = new FileContent()
                    {
                        FileName = wordFilename,
                        ResourceStream = wordStream
                    }
                };

                List<UploadOriginalRequest> inputWrapperList = new List<UploadOriginalRequest>(1) { wordUploadWrapper };
                IEnumerable<StorageResponse<StorageMetaFile>> result = await this._storage.UploadFiles(inputWrapperList);
                result.Should().NotBeNullOrEmpty().And.HaveCount(1);
                result.Single().Should().NotBeNull();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            this._storage.Dispose();
        }
    }
}
