using AutoMapper;
using GroupDocs.Editor.Options;
using GroupDocs.Editor.UI.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace GroupDocs.Editor.UI.Api.Services.Implementation;

public class PdfEditorService : EditorService<PdfLoadOptions, PdfEditOptions>, IPdfEditorService
{
    public PdfEditorService(
        IStorage storage,
        ILogger<PdfEditorService> logger,
        IPdfStorageCache metaFileStorageCache,
        IMapper mapper,
        IIdGeneratorService idGenerator) : base(storage, logger, metaFileStorageCache, mapper, idGenerator)
    {
    }
}