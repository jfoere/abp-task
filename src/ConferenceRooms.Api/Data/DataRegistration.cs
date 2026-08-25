using ConferenceRooms.Business.Abstractions;
using ConferenceRooms.Data.Persistence;
using ConferenceRooms.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConferenceRooms.Data;

public static class DataRegistration
{
    public static IServiceCollection AddConferenceRoomsData(
        this IServiceCollection services,
        IConfiguration configuration,
        string contentRootPath)
    {
        services.AddDbContext<ConferenceRoomsDbContext>((serviceProvider, options) =>
        {
            var currentConfiguration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = BuildConnectionString(currentConfiguration, contentRootPath);

            options.UseSqlite(
                connectionString,
                sqlite => sqlite.MigrationsAssembly(typeof(ConferenceRoomsDbContext).Assembly.FullName));
        });

        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();

        return services;
    }

    public static async Task InitializeConferenceRoomsDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ConferenceRoomsDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
        await DatabaseSeeder.SeedAsync(dbContext, cancellationToken);
    }

    private static string BuildConnectionString(IConfiguration configuration, string contentRootPath)
    {
        var configuredConnectionString = configuration.GetConnectionString("ConferenceRooms");
        if (!string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            return configuredConnectionString;
        }

        var configuredPath = configuration["Database:Path"];
        string databasePath;

        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            databasePath = Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(contentRootPath, configuredPath);
        }
        else if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID")))
        {
            var azureHome = Environment.GetEnvironmentVariable("HOME")
                ?? throw new InvalidOperationException("Azure App Service did not provide the HOME directory.");
            databasePath = Path.Combine(azureHome, "data", "conference-rooms.db");
        }
        else
        {
            databasePath = Path.Combine(contentRootPath, "App_Data", "conference-rooms.db");
        }

        var directory = Path.GetDirectoryName(databasePath)
            ?? throw new InvalidOperationException("The configured database path has no directory.");
        Directory.CreateDirectory(directory);

        return $"Data Source={databasePath};Foreign Keys=True;Default Timeout=30";
    }
}

public sealed class ConferenceRoomsDbContextFactory
    : IDesignTimeDbContextFactory<ConferenceRoomsDbContext>
{
    public ConferenceRoomsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ConferenceRoomsDbContext>()
            .UseSqlite("Data Source=conference-rooms.design.db")
            .Options;

        return new ConferenceRoomsDbContext(options);
    }
}
