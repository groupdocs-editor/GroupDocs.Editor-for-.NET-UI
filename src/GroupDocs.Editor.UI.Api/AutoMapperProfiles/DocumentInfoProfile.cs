using AutoMapper;
using GroupDocs.Editor.Formats;
using GroupDocs.Editor.Metadata;
using GroupDocs.Editor.UI.Api.Models.Storage;

namespace GroupDocs.Editor.UI.Api.AutoMapperProfiles;

public class DocumentInfoProfile : Profile
{
    public DocumentInfoProfile()
    {
        CreateMap<WordProcessingDocumentInfo, StorageDocumentInfo>()
            .ForMember(dest => dest.Format, opt => opt.MapFrom(src => src.Format))
            .ForMember(dest => dest.FamilyFormat,
                opt => opt.MapFrom(src => "WordProcessing"));
        CreateMap<FixedLayoutDocumentInfo, StorageDocumentInfo>()
            .ForMember(dest => dest.Format,
                opt => opt.MapFrom(src => FixedLayoutFormats.FromExtension(src.Format.Extension)))
            .ForMember(dest => dest.FamilyFormat,
                opt => opt.MapFrom(src => src.Format == FixedLayoutFormats.Pdf ? "Pdf" : "Xps"));
        CreateMap<PresentationDocumentInfo, StorageDocumentInfo>()
            .ForMember(dest => dest.Format, opt => opt.MapFrom(src => src.Format))
            .ForMember(dest => dest.FamilyFormat,
                opt => opt.MapFrom(src => "Presentation"));
        CreateMap<SpreadsheetDocumentInfo, StorageDocumentInfo>()
            .ForMember(dest => dest.Format, opt => opt.MapFrom(src => src.Format))
            .ForMember(dest => dest.FamilyFormat,
                opt => opt.MapFrom(src => "Spreadsheet"));
        CreateMap<EmailDocumentInfo, StorageDocumentInfo>()
            .ForMember(dest => dest.Format, opt => opt.MapFrom(src => src.Format))
            .ForMember(dest => dest.FamilyFormat,
                opt => opt.MapFrom(src => "Email"));
    }
}