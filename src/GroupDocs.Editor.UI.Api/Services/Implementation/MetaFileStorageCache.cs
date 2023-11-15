using GroupDocs.Editor.UI.Api.Models.DocumentConvertor;
using GroupDocs.Editor.UI.Api.Models.Storage;
using GroupDocs.Editor.UI.Api.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace GroupDocs.Editor.UI.Api.Services.Implementation;

public class MetaFileStorageCache : IMetaFileStorageCache
{
    private readonly IMemoryCache _memoryCache;
    private readonly IStorage _storage;
    private const string MetaFileName = $"{nameof(StorageMetaFile)}.json";

    public MetaFileStorageCache(IMemoryCache memoryCache, IStorage storage)
    {
        _memoryCache = memoryCache;
        _storage = storage;
    }

    public async Task<StorageMetaFile?> UpdateFiles(StorageMetaFile? metaFile)
    {
        if (metaFile == null) throw new ArgumentNullException(nameof(metaFile));
        _memoryCache.Remove(metaFile.DocumentCode);
        return await _memoryCache.GetOrCreateAsync(
            metaFile.DocumentCode,
            async cacheEntry =>
            {
                cacheEntry.SetAbsoluteExpiration(TimeSpan.FromHours(1));
                await using Stream metaStream = new MemoryStream();
                await JsonSerializer.SerializeAsync(metaStream, metaFile);
                var file = Path.Combine(metaFile.DocumentCode.ToString(), MetaFileName);
                _storage.RemoveFile(file);
                await _storage.SaveFile(new[] { new FileContent { FileName = MetaFileName, ResourceStream = metaStream } },
                    metaFile.DocumentCode);
                return metaFile;
            });
    }

    public async Task<StorageMetaFile?> DownloadFile(Guid documentFolderCode)
    {
        return await _memoryCache.GetOrCreateAsync(
            documentFolderCode,
            async cacheEntry =>
            {
                cacheEntry.SetAbsoluteExpiration(TimeSpan.FromHours(1));
                var file = Path.Combine(documentFolderCode.ToString(), MetaFileName);
                using var response = await _storage.DownloadFile(file);
                var data = response is not { IsSuccess: true } || response.Response == null
                    ? null
                    : JsonSerializer.Deserialize<StorageMetaFile>(response.Response);
                return data;
            });
    }
}