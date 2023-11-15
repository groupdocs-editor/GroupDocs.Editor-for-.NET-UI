using AutoMapper;
using GroupDocs.Editor.Options;
using GroupDocs.Editor.UI.Api.Controllers.RequestModels;
using GroupDocs.Editor.UI.Api.Controllers.RequestModels.Pdf;
using GroupDocs.Editor.UI.Api.Controllers.RequestModels.WordProcessing;
using GroupDocs.Editor.UI.Api.Models.Editor;

namespace GroupDocs.Editor.UI.Api.AutoMapperProfiles;

public class UploadProfile : Profile
{
    public UploadProfile()
    {
        WordProcessing();
        Pdf();
    }

    public void WordProcessing()
    {
        CreateMap<WordProcessingUploadRequest, SaveDocumentRequest>()
            .ForMember(dest => dest.Stream,
                opt => opt.MapFrom(src => src.File.OpenReadStream()))
            .ForMember(dest => dest.FileName,
                opt => opt.MapFrom(src => src.File.FileName));
        CreateMap<WordProcessingNewDocumentRequest, SaveDocumentRequest>()
            .ForMember(dest => dest.Stream,
                opt => opt.MapFrom(src => WordProcessingNewDocument.Stream))
            .ForMember(dest => dest.FileName,
                opt => opt.MapFrom(src => "NewDocument.docx"))
            .ForMember(dest => dest.LoadOptions,
                opt => opt.MapFrom(src => new WordProcessingLoadOptions()));
        CreateMap<WordProcessingDownloadRequest, DownloadDocumentRequest>()
            .ForMember(dest => dest.LoadOptions,
                opt => opt.MapFrom(src => src.LoadOptions));
        CreateMap<WordToPdfDownloadRequest, DownloadPdfRequest>()
            .ForMember(dest => dest.LoadOptions,
                opt => opt.MapFrom(src => src.LoadOptions));
    }

    public void Pdf()
    {
        CreateMap<PdfUploadRequest, SaveDocumentRequest>()
            .ForMember(dest => dest.Stream,
                opt => opt.MapFrom(src => src.File.OpenReadStream()))
            .ForMember(dest => dest.FileName,
                opt => opt.MapFrom(src => src.File.FileName));
        CreateMap<PdfNewDocumentRequest, SaveDocumentRequest>()
            .ForMember(dest => dest.Stream,
                opt => opt.MapFrom(src => PdfNewDocument.Stream))
            .ForMember(dest => dest.FileName,
                opt => opt.MapFrom(src => "NewDocument.pdf"))
            .ForMember(dest => dest.LoadOptions,
                opt => opt.MapFrom(src => new PdfLoadOptions()));
        CreateMap<PdfDownloadRequest, DownloadDocumentRequest>()
            .ForMember(dest => dest.LoadOptions,
                opt => opt.MapFrom(src => src.LoadOptions));
    }
}

public static class WordProcessingNewDocument
{
    private const string _sampleFile = "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";

    public static Stream Stream
    {
        get
        {
            byte[] bytes = Convert.FromBase64String(_sampleFile);
            return new MemoryStream(bytes);
        }
    }
}

public static class PdfNewDocument
{
    private const string _sampleFile = "JVBERi0xLjcNCjUgMCBvYmoNCjw8L1R5cGUgL1BhZ2UvUGFyZW50IDMgMCBSL0NvbnRlbnRzIDYgMCBSL01lZGlhQm94IFswIDAgNjEyIDc5Ml0vUmVzb3VyY2VzPDwvRm9udDw8L0ZBQUFBSSA4IDAgUj4+Pj4vR3JvdXAgPDwvVHlwZS9Hcm91cC9TL1RyYW5zcGFyZW5jeS9DUy9EZXZpY2VSR0I+Pj4+DQplbmRvYmoNCjYgMCBvYmoNCjw8L0xlbmd0aCAxMCAwIFI+PnN0cmVhbQ0KMSAwIDAgLTEgMCA3OTIgY20gcSAxIDAgMCAxIDcwLjg0OTk5ODQ3IDcwLjg0OTk5ODQ3IGNtIEJUIC9GQUFBQUkgMTEgVGYgMSAwIDAgLTEgMCAxMC4zMTc5OTk4NCBUbSAwIGcgWyhOZXcgRG8pLTEoY3VtZW50IHdpdGggR3JvdXBEbyktMShjcy4pMShFZCktMShpdG9yICkxKEFwKS0xKHApXSBUSiBFVCBRIDEgMCAwIC0xIDAgNzkyIGNtIA0KZW5kc3RyZWFtDQplbmRvYmoNCjEwIDAgb2JqDQoxOTMgDQplbmRvYmoNCjEgMCBvYmoNCjw8L1Byb2R1Y2VyKP7/AEEAcwBwAG8AcwBlAC4AVwBvAHIAZABzACAAZgBvAHIAIAAuAE4ARQBUACAAMgAzAC4ANQAuADApPj4NCmVuZG9iag0KMiAwIG9iag0KPDwvVHlwZSAvQ2F0YWxvZy9QYWdlcyAzIDAgUi9MYW5nKGVuLVVTKS9NZXRhZGF0YSA0IDAgUj4+DQplbmRvYmoNCjMgMCBvYmoNCjw8L1R5cGUgL1BhZ2VzL0NvdW50IDEvS2lkc1s1IDAgUiBdPj4NCmVuZG9iag0KNCAwIG9iag0KPDwvVHlwZSAvTWV0YWRhdGEvU3VidHlwZSAvWE1ML0xlbmd0aCAxMSAwIFI+PnN0cmVhbQ0KPD94cGFja2V0IGJlZ2luPSLvu78iIGlkPSJXNU0wTXBDZWhpSHpyZVN6TlRjemtjOWQiPz4KPHg6eG1wbWV0YSB4bWxuczp4PSJhZG9iZTpuczptZXRhLyIgeDp4bXB0az0iUERGTmV0Ij4KPHJkZjpSREYgeG1sbnM6cmRmPSJodHRwOi8vd3d3LnczLm9yZy8xOTk5LzAyLzIyLXJkZi1zeW50YXgtbnMjIj4KPHJkZjpEZXNjcmlwdGlvbiByZGY6YWJvdXQ9IiIgeG1sbnM6ZGM9Imh0dHA6Ly9wdXJsLm9yZy9kYy9lbGVtZW50cy8xLjEvIj4KPGRjOmZvcm1hdD5hcHBsaWNhdGlvbi9wZGY8L2RjOmZvcm1hdD4KPC9yZGY6RGVzY3JpcHRpb24+CjxyZGY6RGVzY3JpcHRpb24gcmRmOmFib3V0PSIiIHhtbG5zOnBkZj0iaHR0cDovL25zLmFkb2JlLmNvbS9wZGYvMS4zLyI+CjxwZGY6UHJvZHVjZXI+QXNwb3NlLldvcmRzIGZvciAuTkVUIDIzLjUuMDwvcGRmOlByb2R1Y2VyPgo8L3JkZjpEZXNjcmlwdGlvbj4KPC9yZGY6UkRGPgo8L3g6eG1wbWV0YT4KPD94cGFja2V0IGVuZD0idyI/PgoNCmVuZHN0cmVhbQ0KZW5kb2JqDQoxMSAwIG9iag0KNTAwIA0KZW5kb2JqDQo4IDAgb2JqDQo8PC9UeXBlIC9Gb250L1N1YnR5cGUgL1RydWVUeXBlL0Jhc2VGb250IC9GQUFBQUkrTGliZXJhdGlvblNhbnMvRW5jb2RpbmcgL1dpbkFuc2lFbmNvZGluZy9GaXJzdENoYXIgMzIvTGFzdENoYXIgMTE5L1dpZHRocyBbMjc4IDAgMCAwIDAgMCAwIDAgMCAwIDAgMCAwIDAgMjc4IDAgMCAwIDAgMCAwIDAgMCAwIDAgMCAwIDAgMCAwIDAgMCAwIDY2NyAwIDAgNzIyIDY2NyAwIDc3OCAwIDAgMCAwIDAgMCA3MjIgMCAwIDAgMCAwIDAgMCAwIDAgMCAwIDAgMCAwIDAgMCAwIDAgMCAwIDUwMCA1NTYgNTU2IDAgMCA1NTYgMjIyIDAgMCAwIDgzMyA1NTYgNTU2IDU1NiAwIDMzMyA1MDAgMjc4IDU1NiAwIDcyMiBdL0ZvbnREZXNjcmlwdG9yIDkgMCBSPj4NCmVuZG9iag0KOSAwIG9iag0KPDwvVHlwZSAvRm9udERlc2NyaXB0b3IvRm9udE5hbWUgL0ZBQUFBSStMaWJlcmF0aW9uU2Fucy9TdGVtViA4MC9EZXNjZW50IC0yMTIvQXNjZW50IDkwNS9DYXBIZWlnaHQgNjg4L0ZsYWdzIDMyL0l0YWxpY0Fu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";

    public static Stream Stream
    {
        get
        {
            byte[] bytes = Convert.FromBase64String(_sampleFile);
            return new MemoryStream(bytes);
        }
    }
}