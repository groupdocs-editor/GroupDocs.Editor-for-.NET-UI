using GroupDocs.Editor.UI.Api.Services.Implementation;

namespace GroupDocs.Editor.UI.Api.Models.Storage.Responses;

public class StorageDisposableResponse<T> : StorageResponse, IDisposable where T : IDisposable
{
    private StorageDisposableResponse(StorageActionStatus status, T? response) : base(status)
    {
        Response = response;
    }

    public T? Response { get; }

    public static StorageDisposableResponse<T> CreateSuccess(T? response)
    {
        return new StorageDisposableResponse<T>(StorageActionStatus.Success, response);
    }

    public static StorageDisposableResponse<T> CreateFailed(T? response)
    {
        return new StorageDisposableResponse<T>(StorageActionStatus.Failed, response);
    }

    public static StorageDisposableResponse<T> CreateNotExist(T? response)
    {
        return new StorageDisposableResponse<T>(StorageActionStatus.NotExist, response);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Response?.Dispose();
        }
    }
}