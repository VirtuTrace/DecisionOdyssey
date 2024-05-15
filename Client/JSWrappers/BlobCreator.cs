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
}