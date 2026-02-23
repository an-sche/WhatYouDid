namespace WhatYouDid.Client.Services;

using Microsoft.JSInterop;
using WhatYouDid.Shared;

public class ClientBrowserStorage(IJSRuntime js) : IBrowserStorage
{
    public async Task DeleteAsync(string key)
    {
        await js.InvokeVoidAsync("localStorage.removeItem", key);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var json = await js.InvokeAsync<string?>("localStorage.getItem", key);
        return json is null ? default : System.Text.Json.JsonSerializer.Deserialize<T>(json);
    }

    public async Task SetAsync(string key, object value)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(value);
        await js.InvokeVoidAsync("localStorage.setItem", key, json);
    }
}