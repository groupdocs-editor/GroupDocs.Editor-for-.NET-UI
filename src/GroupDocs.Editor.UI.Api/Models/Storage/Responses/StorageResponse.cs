using GroupDocs.Editor.UI.Api.Services.Implementation;

namespace GroupDocs.Editor.UI.Api.Models.Storage.Responses;

public class StorageResponse
{
    protected StorageResponse(StorageActionStatus status)
    {
        Status = status;
    }

    public static StorageResponse CreateSuccess()
    {
        return new StorageResponse(StorageActionStatus.Success);
    }

    public static StorageResponse CreateFailed()
    {
        return new StorageResponse(StorageActionStatus.Failed);
    }

    public static StorageResponse CreateNotExist()
    {
        return new StorageResponse(StorageActionStatus.NotExist);
    }

    public StorageActionStatus Status { get; }

    public bool IsSuccess => Status == StorageActionStatus.Success;
}

public class StorageResponse<T> : StorageResponse
{
    private StorageResponse(StorageActionStatus status, T? response) : base(status)
    {
        Response = response;
    }

    public T? Response { get; }

    public static StorageResponse<T> CreateSuccess(T? response)
    {
        return new StorageResponse<T>(StorageActionStatus.Success, response);
    }

    public static StorageResponse<T> CreateFailed(T? response)
    {
        return new StorageResponse<T>(StorageActionStatus.Failed, response);
    }

    public static StorageResponse<T> CreateNotExist(T? response)
    {
        return new StorageResponse<T>(StorageActionStatus.NotExist, response);
    }
}