using BlazorAuthNZDemo.Client.Models;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using System.Text.Json;

namespace BlazorAuthNZDemo.Client.Services;

public class WasmBandsClient(HttpClient http) : IBandsClient
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<List<Band>> GetBandsAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, 
            "localapi/bands");
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
        var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await JsonSerializer.DeserializeAsync<List<Band>>(
            await response.Content.ReadAsStreamAsync(), _jsonOptions) ?? [];

    }
}
