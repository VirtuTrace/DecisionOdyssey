using System.Text.Json;
using Microsoft.JSInterop;

namespace Client.JSWrappers;

public class LocalStorageAccessor(IJSRuntime jsRuntime) : JsWrapper(jsRuntime)
{
    protected override string JsFileName => "/js/LocalStorageAccessor.js";

    private async Task<T?> GetValueAsync<T>(string key)
    {
        await WaitForReference();
        var result = await AccessorJsRef.Value.InvokeAsync<string?>("get", key);
        return result is null ? default : JsonSerializer.Deserialize<T>(result);
    }
    
    public async Task<T> GetValueOrDefaultAsync<T>(string key, T defaultValue)
    {
        var result = await GetValueAsync<T>(key);

        return result ?? defaultValue;
    }

    public async Task<bool> CheckValueExistsAsync(string key)
    {
        await WaitForReference();
        var result = await AccessorJsRef.Value.InvokeAsync<bool>("exists", key);

        return result;
    }

    public async Task SetValueAsync<T>(string key, T value)
    {
        await WaitForReference();
        await AccessorJsRef.Value.InvokeVoidAsync("set", key, value);
    }

    public async Task Clear()
    {
        await WaitForReference();
        await AccessorJsRef.Value.InvokeVoidAsync("clear");
    }

    public async Task RemoveAsync(string key)
    {
        await WaitForReference();
        await AccessorJsRef.Value.InvokeVoidAsync("remove", key);
    }
}