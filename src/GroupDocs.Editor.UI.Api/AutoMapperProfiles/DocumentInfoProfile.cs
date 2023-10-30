using AutoMapper;
using GroupDocs.Editor.Metadata;
using GroupDocs.Editor.UI.Api.Models.Storage;

namespace GroupDocs.Editor.UI.Api.AutoMapperProfiles;

public class DocumentInfoProfile : Profile
{
    public DocumentInfoProfile()
    {
        CreateMap<WordProcessingDocumentInfo, StorageDocumentInfo>();

    }
}
