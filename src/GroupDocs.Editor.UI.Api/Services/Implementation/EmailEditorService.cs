using AutoMapper;
using GroupDocs.Editor.Options;
using GroupDocs.Editor.UI.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace GroupDocs.Editor.UI.Api.Services.Implementation;

public class EmailEditorService : EditorService<ILoadOptions, EmailEditOptions>, IEmailEditorService
{
    public EmailEditorService(
        IStorage storage,
        ILogger<EmailEditorService> logger,
        IEmailStorageCache metaFileStorageCache,
        IMapper mapper,
        IIdGeneratorService idGenerator) : base(storage, logger, metaFileStorageCache, mapper, idGenerator)
    {
    }
}