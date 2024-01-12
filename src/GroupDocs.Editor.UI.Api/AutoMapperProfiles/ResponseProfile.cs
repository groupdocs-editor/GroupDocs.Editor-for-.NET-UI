using AutoMapper;
using GroupDocs.Editor.Options;
using GroupDocs.Editor.UI.Api.Controllers.ResponseModels;
using GroupDocs.Editor.UI.Api.Models.Storage;

namespace GroupDocs.Editor.UI.Api.AutoMapperProfiles;

public class ResponseProfile : Profile
{
    public ResponseProfile()
    {
        Presentation();
        WordProcessing();
    }

    public void Presentation()
    {
        CreateMap<StorageMetaFile<PresentationLoadOptions, PresentationEditOptions>, DocumentUploadResponse<PresentationLoadOptions>>()
            .ForMember(dest => dest.OriginalLoadOptions,
                opt => opt.MapFrom(src => new PresentationLoadOptions
                {
                    Password = new string('*',
                        src.OriginalLoadOptions == null ? 0 : src.OriginalLoadOptions.Password.Length)
                }));
    }

    public void WordProcessing()
    {
        CreateMap<StorageMetaFile<WordProcessingLoadOptions, WordProcessingEditOptions>, DocumentUploadResponse<WordProcessingLoadOptions>>()
            .ForMember(dest => dest.OriginalLoadOptions,
                opt => opt.MapFrom(src => new WordProcessingLoadOptions
                {
                    Password = new string('*',
                        src.OriginalLoadOptions == null ? 0 : src.OriginalLoadOptions.Password.Length)
                }));
    }



    public void Pdf()
    {
        CreateMap<StorageMetaFile<PdfLoadOptions, PdfEditOptions>, DocumentUploadResponse<PdfLoadOptions>>()
            .ForMember(dest => dest.OriginalLoadOptions,
                opt => opt.MapFrom(src => new PdfLoadOptions
                {
                    Password = new string('*',
                        src.OriginalLoadOptions == null ? 0 : src.OriginalLoadOptions.Password.Length)
                }));
    }
}
