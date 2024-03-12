using GroupDocs.Editor.Options;
using GroupDocs.Editor.UI.Api.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace GroupDocs.Editor.UI.Api.Services.Implementation;

public class EmailStorageCache : MetaFileStorageCache<ILoadOptions, EmailEditOptions>, IEmailStorageCache
{
    public EmailStorageCache(IMemoryCache memoryCache, IStorage storage) : base(memoryCache, storage)
    {
    }
}