using Lab.Api;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("Payments")
    ?? throw new InvalidOperationException("Missing ConnectionStrings__Payments");

builder.Services.AddNpgsqlDataSource(connectionString);
builder.Services.AddHttpClient("oidc", c => c.Timeout = TimeSpan.FromSeconds(3));
builder
    .Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"])
    .AddCheck<OidcDiscoveryHealthCheck>("issuer", tags: ["ready"]);

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Oidc:RequireAtStartup"))
{
    var report = await app.Services.GetRequiredService<HealthCheckService>().CheckHealthAsync(r => r.Name == "issuer");
    if (report.Status != HealthStatus.Healthy)
        throw new InvalidOperationException("Issuer discovery failed at startup (Oidc:RequireAtStartup=true).");
}

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") });

app.MapGet(
    "/api/info",
    (IConfiguration config) => new { version = config["APP_VERSION"] ?? "unknown", pod = Environment.MachineName }
);

app.MapPaymentEndpoints();

app.Run();
