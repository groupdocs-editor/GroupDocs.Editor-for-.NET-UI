using GroupDocs.Editor.UI.Api.Services.Implementation;

namespace GroupDocs.Editor.UI.Api.Models.Storage.Responses;

public class StorageUpdateResourceResponse<T, T2> : StorageResponse
{
    private StorageUpdateResourceResponse(StorageActionStatus status, T? response, T2? additionalData) : base(status)
    {
        Response = response;
        AdditionalData = additionalData;
    }

    public T? Response { get; }
    public T2? AdditionalData { get; }

    public static StorageUpdateResourceResponse<T, T2> CreateSuccess(T? response, T2? additionalData)
    {
        return new StorageUpdateResourceResponse<T, T2>(StorageActionStatus.Success, response, additionalData);
    }

    public static StorageUpdateResourceResponse<T, T2> CreateFailed(T? response, T2? additionalData)
    {
        return new StorageUpdateResourceResponse<T, T2>(StorageActionStatus.Failed, response, additionalData);
    }

    public static StorageUpdateResourceResponse<T, T2> CreateNotExist(T? response, T2? additionalData)
    {
        return new StorageUpdateResourceResponse<T, T2>(StorageActionStatus.NotExist, response, additionalData);
    }
}