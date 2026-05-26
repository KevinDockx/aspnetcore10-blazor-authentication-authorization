using BlazorAuthNZDemo.TokenStores;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace BlazorAuthNZDemo.DelegatingHandlers;

public class AccessTokenHandler(
    IHttpContextAccessor httpContextAccessor,
    ServerSideTokenStore tokenStore,
    IConfiguration configuration) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user is not null)
        {
            var tokenInfo = tokenStore.GetTokens(user);
            if (tokenInfo is not null)
            {
                // If the access token is about to expire, refresh it
                if (tokenInfo.Expiration <= DateTimeOffset.UtcNow.AddMinutes(5)
                    && tokenInfo.RefreshToken is not null)
                {
                    tokenInfo = await RefreshAccessTokenAsync(user, tokenInfo);
                }
                if (tokenInfo?.AccessToken is not null)
                {
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", tokenInfo.AccessToken);
                }
            }

        }
        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<TokenInfo?> RefreshAccessTokenAsync(
        ClaimsPrincipal user, TokenInfo currentTokenInfo)
    {
        var schemeConfig = configuration.GetSection(
            "Authentication:Schemes:EntraIDOpenIDConnect");
        var authority = schemeConfig["Authority"]
                ?? throw new InvalidOperationException("Authority is not configured.");
        var clientId = schemeConfig["ClientId"]
            ?? throw new InvalidOperationException("ClientId is not configured.");
        var clientSecret = schemeConfig["ClientSecret"]
            ?? throw new InvalidOperationException("ClientSecret is not configured.");
        // Derive the token endpoint from the authority
        var tokenEndpoint = authority.Replace("/v2.0", "/oauth2/v2.0/token");
        using var refreshClient = new HttpClient();
        var parameters = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["refresh_token"] = currentTokenInfo.RefreshToken!
        };
        var response = await refreshClient.PostAsync(tokenEndpoint,
                new FormUrlEncodedContent(parameters));
        if (!response.IsSuccessStatusCode)
        {
            return currentTokenInfo;
        }
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var newAccessToken = payload.GetProperty("access_token").GetString();
        var newRefreshToken = payload.TryGetProperty("refresh_token", out var rt)
            ? rt.GetString() : currentTokenInfo.RefreshToken;
        var expiresIn = payload.GetProperty("expires_in").GetInt32();
        var expiration = DateTimeOffset.UtcNow.AddSeconds(expiresIn);
        tokenStore.StoreTokens(user, newAccessToken, newRefreshToken, expiration);
        return new TokenInfo(newAccessToken, newRefreshToken, expiration);
    }

}
