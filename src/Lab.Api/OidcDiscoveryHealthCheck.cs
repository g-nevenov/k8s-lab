using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Lab.Api;

/// <summary>Check if OpenID connect is reachable and complete</summary>
public sealed class OidcDiscoveryHealthCheck(IHttpClientFactory http, IConfiguration config) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        var authority = config["Oidc:Authority"];
        if (string.IsNullOrEmpty(authority))
            return HealthCheckResult.Healthy("OIDC not configured.");

        try
        {
            using var client = http.CreateClient("oidc");
            var json = await client.GetStringAsync(
                $"{authority.TrimEnd('/')}/.well-known/openid-configuration",
                cancellationToken
            );
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("jwks_uri", out _)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Discovery document has no jwks_uri.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return HealthCheckResult.Unhealthy("Issuer is not reachable.", ex);
        }
    }
}
