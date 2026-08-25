using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ConferenceRooms.Api.Auth;

public static class ApiKeyDefaults
{
    public const string AuthenticationScheme = "ApiKey";
    public const string HeaderName = "X-API-Key";
}

public static class ApiRoles
{
    public const string Admin = "Admin";
    public const string Customer = "Customer";
}

public static class AuthorizationPolicies
{
    public const string Admin = "AdminOnly";
    public const string CustomerOrAdmin = "CustomerOrAdmin";
}

public static class RateLimitPolicies
{
    public const string Public = "Public";
    public const string Protected = "Protected";
}

public sealed class ApiKeyOptions
{
    public const string SectionName = "ApiKeys";

    public List<ApiKeyClient> Clients { get; init; } = [];
}

public sealed class ApiKeyClient
{
    public string Name { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public string Key { get; init; } = string.Empty;
}

public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> schemeOptions,
    IOptionsMonitor<ApiKeyOptions> apiKeyOptions,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(schemeOptions, loggerFactory, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyDefaults.HeaderName, out var suppliedValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (suppliedValues.Count != 1 || string.IsNullOrWhiteSpace(suppliedValues[0]))
        {
            return Task.FromResult(AuthenticateResult.Fail("A single API key is required."));
        }

        var suppliedKeyHash = SHA256.HashData(Encoding.UTF8.GetBytes(suppliedValues[0]!));
        var client = apiKeyOptions.CurrentValue.Clients.FirstOrDefault(candidate =>
            !string.IsNullOrWhiteSpace(candidate.Key)
            && CryptographicOperations.FixedTimeEquals(
                suppliedKeyHash,
                SHA256.HashData(Encoding.UTF8.GetBytes(candidate.Key))));

        if (client is null)
        {
            return Task.FromResult(AuthenticateResult.Fail("The supplied API key is invalid."));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, client.Name),
            new Claim(ClaimTypes.Name, client.Name),
            new Claim(ClaimTypes.Role, client.Role)
        };
        var identity = new ClaimsIdentity(claims, ApiKeyDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, ApiKeyDefaults.AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

