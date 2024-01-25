using AutoMapper;
using GroupDocs.Editor.Options;
using GroupDocs.Editor.UI.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace GroupDocs.Editor.UI.Api.Services.Implementation;

public class SpreadsheetEditorService : EditorService<SpreadsheetLoadOptions, SpreadsheetEditOptions>, ISpreadsheetEditorService
{
    public SpreadsheetEditorService(
        IStorage storage,
        ILogger<SpreadsheetEditorService> logger,
        ISpreadsheetStorageCache metaFileStorageCache,
        IMapper mapper,
        IIdGeneratorService idGenerator) : base(storage, logger, metaFileStorageCache, mapper, idGenerator)
    {
    }
}