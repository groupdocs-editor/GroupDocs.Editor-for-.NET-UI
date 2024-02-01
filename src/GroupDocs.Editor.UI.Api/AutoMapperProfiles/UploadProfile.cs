using AutoMapper;
using GroupDocs.Editor.Formats;
using GroupDocs.Editor.UI.Api.Controllers.RequestModels;
using GroupDocs.Editor.UI.Api.Controllers.RequestModels.Pdf;
using GroupDocs.Editor.UI.Api.Controllers.RequestModels.Presentation;
using GroupDocs.Editor.UI.Api.Controllers.RequestModels.Spreadsheet;
using GroupDocs.Editor.UI.Api.Controllers.RequestModels.WordProcessing;
using GroupDocs.Editor.UI.Api.Models.Editor;

namespace GroupDocs.Editor.UI.Api.AutoMapperProfiles;

public class UploadProfile : Profile
{
    public UploadProfile()
    {
        WordProcessing();
        Pdf();
        Presentation();
        Spreadsheet();
    }

    public void WordProcessing()
    {
        CreateMap<WordProcessingUploadRequest, UploadDocumentRequest>()
            .ForMember(dest => dest.Stream,
                opt => opt.MapFrom(src => src.File.OpenReadStream()))
            .ForMember(dest => dest.FileName,
                opt => opt.MapFrom(src => src.File.FileName));
        CreateMap<WordProcessingNewDocumentRequest, CreateDocumentRequest>()
            .ForMember(dest => dest.Format,
                opt => opt.MapFrom(src =>
                    WordProcessingFormats.FromExtension(src.Format)))
            .ForMember(dest => dest.FileName,
                opt => opt.MapFrom(src =>
                    string.IsNullOrWhiteSpace(src.FileName) ? $"newDocxDocument.{src.Format}" : src.FileName));
        CreateMap<WordProcessingDownloadRequest, DownloadDocumentRequest>();
        CreateMap<WordToPdfDownloadRequest, DownloadPdfRequest>();
    }

    public void Pdf()
    {
        CreateMap<PdfUploadRequest, UploadDocumentRequest>()
            .ForMember(dest => dest.Stream,
                opt => opt.MapFrom(src => src.File.OpenReadStream()))
            .ForMember(dest => dest.FileName,
                opt => opt.MapFrom(src => src.File.FileName));
        CreateMap<PdfNewDocumentRequest, CreateDocumentRequest>()
            .ForMember(dest => dest.FileName,
                opt => opt.MapFrom(src =>
                    string.IsNullOrWhiteSpace(src.FileName) ? "newPdfDocument.pdf" : src.FileName));
        CreateMap<PdfDownloadRequest, DownloadDocumentRequest>()
            .ForMember(dest => dest.LoadOptions,
                opt => opt.MapFrom(src => src.LoadOptions));
    }

    public void Presentation()
    {
        CreateMap<PresentationUploadRequest, UploadDocumentRequest>()
            .ForMember(dest => dest.Stream,
                opt => opt.MapFrom(src => src.File.OpenReadStream()))
            .ForMember(dest => dest.FileName,
                opt => opt.MapFrom(src => src.File.FileName));
        CreateMap<PresentationNewDocumentRequest, CreateDocumentRequest>()
            .ForMember(dest => dest.Format,
                opt => opt.MapFrom(src =>
                    PresentationFormats.FromExtension(src.Format)))
            .ForMember(dest => dest.FileName,
                opt => opt.MapFrom(src =>
                    string.IsNullOrWhiteSpace(src.FileName) ? $"newPptxDocument.{src.Format}" : src.FileName));
        CreateMap<PresentationDownloadRequest, DownloadDocumentRequest>();
    }
    public void Spreadsheet()
    {
        CreateMap<SpreadsheetUploadRequest, UploadDocumentRequest>()
            .ForMember(dest => dest.Stream,
                opt => opt.MapFrom(src => src.File.OpenReadStream()))
            .ForMember(dest => dest.FileName,
                opt => opt.MapFrom(src => src.File.FileName));
        CreateMap<SpreadsheetNewDocumentRequest, CreateDocumentRequest>()
            .ForMember(dest => dest.Format,
                opt => opt.MapFrom(src =>
                    SpreadsheetFormats.FromExtension(src.Format)))
            .ForMember(dest => dest.FileName,
                opt => opt.MapFrom(src =>
                    string.IsNullOrWhiteSpace(src.FileName) ? $"newDocument.{src.Format}" : src.FileName));
        CreateMap<SpreadsheetDownloadRequest, DownloadDocumentRequest>();
    }
}
