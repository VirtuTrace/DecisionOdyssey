using System.Text;
using Microsoft.JSInterop;

namespace Client.JSWrappers;

public class FileHandler(IJSRuntime jsRuntime) : JsWrapper(jsRuntime)
{
    protected override string JsFileName => "/js/FileHandler.js";
    
    private async Task DownloadFileFromByteArrayAsync(byte[] data, string fileName, string contentType)
    {
        await WaitForReference();
        await AccessorJsRef.Value.InvokeVoidAsync("downloadFileFromByteArray", data, fileName, contentType);
    }

    public async void DownloadJsonAsync(string jsonData, string fileName)
    {
        var data = Encoding.UTF8.GetBytes(jsonData);
        await DownloadFileFromByteArrayAsync(data, fileName, "application/json");
    }
    
    public async void DownloadZipAsync(byte[] data, string fileName)
    {
        await DownloadFileFromByteArrayAsync(data, fileName, "application/zip");
    }
}