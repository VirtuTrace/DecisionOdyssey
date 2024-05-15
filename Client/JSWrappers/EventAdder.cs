using Microsoft.JSInterop;

namespace Client.JSWrappers;

public class EventAdder(IJSRuntime jsRuntime) : JsWrapper(jsRuntime)
{
    protected override string JsFileName => "/js/EventAdder.js";
    
    public async Task AddAudioEvents<T>(DotNetObjectReference<T> dotNetObjectReference, string id, string? onPlay = null, string? onPause = null) where T : class
    {
        await WaitForReference();
        await AccessorJsRef.Value.InvokeVoidAsync("addMediaEventListeners", dotNetObjectReference, id, onPlay, onPause);
    }
}