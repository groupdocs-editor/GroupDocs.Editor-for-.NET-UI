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
        CreateMap<StorageMetaFile<PresentationLoadOptions, PresentationEditOptions>, PresentationUploadResponse>()
            .ForMember(dest => dest.OriginalLoadOptions,
                opt => opt.MapFrom(src => new PresentationLoadOptions
                {
                    Password = new string('*',
                        src.OriginalLoadOptions == null ? 0 : src.OriginalLoadOptions.Password.Length)
                }));
        CreateMap<StorageMetaFile<PresentationLoadOptions, PresentationEditOptions>, PresentationStorageInfo>()
            .ForMember(dest => dest.OriginalLoadOptions,
                opt => opt.MapFrom(src => new PresentationLoadOptions
                {
                    Password = new string('*',
                        src.OriginalLoadOptions == null ? 0 : src.OriginalLoadOptions.Password.Length)
                }));
    }

    public void WordProcessing()
    {
        CreateMap<StorageMetaFile<WordProcessingLoadOptions, WordProcessingEditOptions>, WordProcessingUploadResponse>()
            .ForMember(dest => dest.OriginalLoadOptions,
                opt => opt.MapFrom(src => new WordProcessingLoadOptions
                {
                    Password = new string('*',
                        src.OriginalLoadOptions == null ? 0 : src.OriginalLoadOptions.Password.Length)
                }));
        CreateMap<StorageMetaFile<WordProcessingLoadOptions, WordProcessingEditOptions>, WordProcessingStorageInfo>()
            .ForMember(dest => dest.OriginalLoadOptions,
                opt => opt.MapFrom(src => new WordProcessingLoadOptions
                {
                    Password = new string('*',
                        src.OriginalLoadOptions == null ? 0 : src.OriginalLoadOptions.Password.Length)
                }));
    }



    public void Pdf()
    {
        CreateMap<StorageMetaFile<PdfLoadOptions, PdfEditOptions>, PdfUploadResponse>()
            .ForMember(dest => dest.DocumentCode,
                opt => opt.MapFrom(src => src.DocumentCode))
            .ForMember(dest => dest.OriginalLoadOptions,
                opt => opt.MapFrom(src => new PdfLoadOptions
                {
                    Password = src == null || src.OriginalLoadOptions == null ||
                               string.IsNullOrWhiteSpace(src.OriginalLoadOptions.Password)
                        ? string.Empty
                        : new string('*', src.OriginalLoadOptions.Password.Length)
                }));
        CreateMap<StorageMetaFile<PdfLoadOptions, PdfEditOptions>, PdfStorageInfo>()
            .ForMember(dest => dest.OriginalLoadOptions,
                opt => opt.MapFrom(src => new PdfLoadOptions
                {
                    Password = new string('*',
                        src.OriginalLoadOptions == null || string.IsNullOrWhiteSpace(src.OriginalLoadOptions.Password) ? 0 : src.OriginalLoadOptions.Password.Length)
                }));
    }
}
