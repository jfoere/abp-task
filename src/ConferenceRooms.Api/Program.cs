using System.Threading.RateLimiting;
using ConferenceRooms.Api.Auth;
using ConferenceRooms.Api.Errors;
using ConferenceRooms.Api.OpenApi;
using ConferenceRooms.Business.Services;
using ConferenceRooms.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
var dataProtection = builder.Services.AddDataProtection();
if (builder.Environment.IsEnvironment("Testing"))
{
    dataProtection.UseEphemeralDataProtectionProvider();
}
else
{
    dataProtection.PersistKeysToFileSystem(
        new DirectoryInfo(GetDataProtectionPath(builder.Environment.ContentRootPath)));
}

var businessTimeZoneId = builder.Configuration["Business:TimeZone"] ?? "Europe/Kyiv";
var businessTimeZone = ResolveTimeZone(businessTimeZoneId);

builder.Services.AddSingleton(new BookingTimePolicy(businessTimeZone, businessTimeZoneId));
builder.Services.AddSingleton<PricingCalculator>();
builder.Services.AddScoped<IRoomManagementService, RoomManagementService>();
builder.Services.AddScoped<IBookingManagementService, BookingManagementService>();
builder.Services.AddScoped<IReportingService, ReportingService>();

builder.Services.AddConferenceRoomsData(
    builder.Configuration,
    builder.Environment.ContentRootPath);

var apiKeyOptions = builder.Services
    .AddOptions<ApiKeyOptions>()
    .Bind(builder.Configuration.GetSection(ApiKeyOptions.SectionName))
    .Validate(
        options => options.Clients.All(client =>
            !string.IsNullOrWhiteSpace(client.Name)
            && (client.Role == ApiRoles.Admin || client.Role == ApiRoles.Customer)
            && (string.IsNullOrEmpty(client.Key) || client.Key.Length >= 32)),
        "Every API-key client needs a name, a supported role, and a key of at least 32 characters when configured.")
    .Validate(
        options => options.Clients
            .Where(client => !string.IsNullOrEmpty(client.Key))
            .Select(client => client.Key)
            .Distinct(StringComparer.Ordinal)
            .Count() == options.Clients.Count(client => !string.IsNullOrEmpty(client.Key)),
        "Configured API keys must be unique.");

if (builder.Environment.IsProduction())
{
    apiKeyOptions.Validate(
        options => options.Clients.Any(client => client.Role == ApiRoles.Admin && !string.IsNullOrEmpty(client.Key))
            && options.Clients.Any(client => client.Role == ApiRoles.Customer && !string.IsNullOrEmpty(client.Key)),
        "Production requires at least one configured Admin key and one configured Customer key.");
}

apiKeyOptions.ValidateOnStart();
builder.Services
    .AddAuthentication(ApiKeyDefaults.AuthenticationScheme)
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        ApiKeyDefaults.AuthenticationScheme,
        _ => { });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.Admin, policy =>
        policy.RequireAuthenticatedUser().RequireRole(ApiRoles.Admin));
    options.AddPolicy(AuthorizationPolicies.CustomerOrAdmin, policy =>
        policy.RequireAuthenticatedUser().RequireRole(ApiRoles.Customer, ApiRoles.Admin));
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, _) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        return ValueTask.CompletedTask;
    };

    options.AddPolicy(RateLimitPolicies.Public, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            $"public:{GetClientAddress(httpContext)}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy(RateLimitPolicies.Protected, httpContext =>
    {
        var isAuthenticated = httpContext.User.Identity?.IsAuthenticated == true;
        var identity = isAuthenticated
            ? $"key:{httpContext.User.Identity!.Name}"
            : $"anonymous:{GetClientAddress(httpContext)}";

        return RateLimitPartition.GetFixedWindowLimiter(
            identity,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = isAuthenticated ? 30 : 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var swaggerApiKeyDescription = BuildSwaggerApiKeyDescription(
    builder.Environment,
    builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Conference Rooms API",
        Version = "v1",
        Description = "Manage conference rooms, search availability, create bookings, and view reports."
    });

    options.AddSecurityDefinition(ApiKeyDefaults.AuthenticationScheme, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = ApiKeyDefaults.HeaderName,
        Description = swaggerApiKeyDescription
    });
    options.OperationFilter<AuthorizeOperationFilter>();
    options.OperationFilter<AvailabilityExampleOperationFilter>();

    var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();

await app.Services.InitializeConferenceRoomsDatabaseAsync();

app.Run();

static string GetClientAddress(HttpContext context) =>
    context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

static string BuildSwaggerApiKeyDescription(
    IHostEnvironment environment,
    IConfiguration configuration)
{
    const string defaultDescription = "Enter an Admin or Customer API key.";
    if (!environment.IsDevelopment())
    {
        return defaultDescription;
    }

    var clients = configuration
        .GetSection(ApiKeyOptions.SectionName)
        .Get<ApiKeyOptions>()?
        .Clients
        .Where(client => !string.IsNullOrWhiteSpace(client.Key))
        .ToList() ?? [];

    if (clients.Count == 0)
    {
        return defaultDescription;
    }

    var credentials = string.Join(
        "\n\n",
        clients.Select(client =>
            $"**{client.Name}** ({client.Role})\n\n```\n{client.Key}\n```"));

    return $"Copy one local Development key, click **Authorize**, and paste it below. "
        + $"Never use these keys in Production.\n\n{credentials}";
}

static string GetDataProtectionPath(string contentRootPath)
{
    string path;
    if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID")))
    {
        var azureHome = Environment.GetEnvironmentVariable("HOME")
            ?? throw new InvalidOperationException("Azure App Service did not provide the HOME directory.");
        path = Path.Combine(azureHome, "data", "data-protection-keys");
    }
    else
    {
        path = Path.Combine(contentRootPath, "App_Data", "data-protection-keys");
    }

    Directory.CreateDirectory(path);
    return path;
}

static TimeZoneInfo ResolveTimeZone(string configuredId)
{
    if (TimeZoneInfo.TryFindSystemTimeZoneById(configuredId, out var timeZone))
    {
        return timeZone;
    }

    if (TimeZoneInfo.TryConvertIanaIdToWindowsId(configuredId, out var windowsId)
        && TimeZoneInfo.TryFindSystemTimeZoneById(windowsId, out timeZone))
    {
        return timeZone;
    }

    if (string.Equals(configuredId, "Europe/Kyiv", StringComparison.OrdinalIgnoreCase))
    {
        foreach (var compatibleId in new[] { "Europe/Kiev", "FLE Standard Time" })
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById(compatibleId, out timeZone))
            {
                return timeZone;
            }
        }
    }

    throw new TimeZoneNotFoundException($"The configured business timezone '{configuredId}' is unavailable.");
}

public partial class Program;
