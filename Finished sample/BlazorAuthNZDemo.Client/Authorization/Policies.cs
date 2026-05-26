using Microsoft.AspNetCore.Authorization;

namespace BlazorAuthNZDemo.Client.Authorization;

public static class Policies
{
    public const string IsFromBelgium = "IsFromBelgium";
    public static AuthorizationPolicy IsFromBelgiumPolicy()
        => new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireClaim("ctry", "BE")
            .Build();

    public const string RequiresAdminRole = "RequiresAdminRole";
    public static AuthorizationPolicy RequiresAdminRolePolicy()
        => new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireRole("Admin")
            .Build();


}
