using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace ConferenceRooms.Tests.TestSupport;

public sealed class ConferenceRoomsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public static string AdminKey { get; } = new('A', 32);
    public static string SecondAdminKey { get; } = new('B', 32);
    public static string CustomerKey { get; } = new('C', 32);
    public static string SecondCustomerKey { get; } = new('D', 32);

    private readonly string databaseDirectory = Path.Combine(
        Path.GetTempPath(),
        "ConferenceRooms.Tests",
        Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(databaseDirectory);
        var databasePath = Path.Combine(databaseDirectory, "conference-rooms.tests.db");

        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ConferenceRooms"] =
                    $"Data Source={databasePath};Foreign Keys=True;Default Timeout=30",
                ["ApiKeys:Clients:0:Key"] = AdminKey,
                ["ApiKeys:Clients:1:Key"] = SecondAdminKey,
                ["ApiKeys:Clients:2:Key"] = CustomerKey,
                ["ApiKeys:Clients:3:Key"] = SecondCustomerKey
            });
        });
    }

    public HttpClient CreateAdminClient() => CreateAuthenticatedClient(AdminKey);

    public HttpClient CreateCustomerClient() => CreateAuthenticatedClient(CustomerKey);

    private HttpClient CreateAuthenticatedClient(string apiKey)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        return client;
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(databaseDirectory))
        {
            Directory.Delete(databaseDirectory, recursive: true);
        }
    }
}
