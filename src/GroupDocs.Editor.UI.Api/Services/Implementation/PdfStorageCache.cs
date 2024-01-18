using GroupDocs.Editor.Options;
using GroupDocs.Editor.UI.Api.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace GroupDocs.Editor.UI.Api.Services.Implementation;

public class PdfStorageCache : MetaFileStorageCache<PdfLoadOptions, PdfEditOptions>, IPdfStorageCache
{
    public PdfStorageCache(IMemoryCache memoryCache, IStorage storage) : base(memoryCache, storage)
    {
    }
}