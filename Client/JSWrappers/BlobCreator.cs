using Client.Models.DecisionElements;
using Client.Utility;
using Microsoft.JSInterop;

namespace Client.JSWrappers;

public class BlobCreator(IJSRuntime jsRuntime) : JsWrapper(jsRuntime)
{
    protected override string JsFileName => "/js/BlobCreator.js";
    
    public async Task<string> CreateBlobUrl(byte[] data, string contentType)
    {
        await WaitForReference();
        return await AccessorJsRef.Value.InvokeAsync<string>("createBlobUrl", data, contentType);
    }
    
    public async Task RevokeBlobUrl(string blobUrl)
    {
        await WaitForReference();
        await AccessorJsRef.Value.InvokeVoidAsync("revokeBlobUrl", blobUrl);
    }

    public Task<string> CreateMediaBlobUrl(MediaData media, string contentHeader)
    {
        return CreateBlobUrl(media.Data!, $"{contentHeader}/{media.Extension.RemoveChar('.')}");
    }
}