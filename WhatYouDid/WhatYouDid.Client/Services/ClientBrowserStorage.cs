namespace WhatYouDid.Client.Services;

using System.Text.Json;
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
        if (json is null)
        {
            return default;
        }

        try
        {
            var item = JsonSerializer.Deserialize<T>(json);
            return item;
        } 
        catch { }

        return default;
    }

    public async Task SetAsync(string key, object value)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(value);
        await js.InvokeVoidAsync("localStorage.setItem", key, json);
    }
}