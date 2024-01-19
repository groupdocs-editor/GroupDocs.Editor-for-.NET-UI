using AutoMapper;
using GroupDocs.Editor.Options;
using GroupDocs.Editor.UI.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace GroupDocs.Editor.UI.Api.Services.Implementation;

public class WordProcessingEditorService : EditorService<WordProcessingLoadOptions, WordProcessingEditOptions>, IWordProcessingEditorService
{
    public WordProcessingEditorService(
        IStorage storage,
        ILogger<WordProcessingEditorService> logger,
        IWordProcessingStorageCache metaFileStorageCache,
        IMapper mapper,
        IIdGeneratorService idGenerator) : base(storage, logger, metaFileStorageCache, mapper, idGenerator)
    {
    }
}