﻿using Microsoft.JSInterop;

namespace Client.JSWrappers;

public abstract class JsWrapper(IJSRuntime jsRuntime) : IAsyncDisposable
{
    protected abstract string JsFileName { get; }

    protected Lazy<IJSObjectReference> AccessorJsRef = new();

    protected async Task WaitForReference()
    {
        if (AccessorJsRef.IsValueCreated is false)
        {
            AccessorJsRef = new Lazy<IJSObjectReference>(await jsRuntime.InvokeAsync<IJSObjectReference>("import", JsFileName));
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (AccessorJsRef.IsValueCreated)
        {
            await AccessorJsRef.Value.DisposeAsync();
        }
    }
}