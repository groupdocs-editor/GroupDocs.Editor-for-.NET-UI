using GroupDocs.Editor.Options;
using GroupDocs.Editor.UI.Api.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace GroupDocs.Editor.UI.Api.Services.Implementation;

public class WordProcessingStorageCache : MetaFileStorageCache<WordProcessingLoadOptions, WordProcessingEditOptions>, IWordProcessingStorageCache
{
    public WordProcessingStorageCache(IMemoryCache memoryCache, IStorage storage) : base(memoryCache, storage)
    {
    }
}