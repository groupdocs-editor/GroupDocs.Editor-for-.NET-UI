using AutoMapper;
using GroupDocs.Editor.Options;
using GroupDocs.Editor.UI.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace GroupDocs.Editor.UI.Api.Services.Implementation;

public class PresentationEditorService : EditorService<PresentationLoadOptions, PresentationEditOptions>, IPresentationEditorService
{
    public PresentationEditorService(
        IStorage storage,
        ILogger<PresentationEditorService> logger,
        IPresentationStorageCache metaFileStorageCache,
        IMapper mapper,
        IIdGeneratorService idGenerator) : base(storage, logger, metaFileStorageCache, mapper, idGenerator)
    {
    }
}