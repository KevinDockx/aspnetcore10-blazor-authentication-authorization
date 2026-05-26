using System.Collections.Concurrent;
using System.Security.Claims;

namespace BlazorAuthNZDemo.TokenStores;

public class ServerSideTokenStore
{
    private readonly ConcurrentDictionary<string, TokenInfo> _tokens = new();

    public void StoreTokens(ClaimsPrincipal user, string? accessToken, string? refreshToken,
        DateTimeOffset expiration)
    {
        var key = GetKey(user);
        _tokens[key] = new TokenInfo(accessToken, refreshToken, expiration);
    }

    public TokenInfo? GetTokens(ClaimsPrincipal user)
    {
        var key = GetKey(user);
        _tokens.TryGetValue(key, out var token);
        return token;
    }

    public void ClearTokens(ClaimsPrincipal user)
    {
        var key = GetKey(user);
        _tokens.TryRemove(key, out _);
    }

    private static string GetKey(ClaimsPrincipal user)
    {
        return user.FindFirstValue("sub")
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? "anonymous";
    }
}

public record TokenInfo(string? AccessToken, string? RefreshToken, DateTimeOffset Expiration);
