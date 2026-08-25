using ConferenceRooms.Business.Domain;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRooms.Data.Persistence;

internal static class DatabaseSeeder
{
    private static readonly DateTime SeededAtUtc = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly OptionalService[] ServiceSeeds =
    [
        new(new Guid("10000000-0000-0000-0000-000000000001"), "Projector", 500m),
        new(new Guid("10000000-0000-0000-0000-000000000002"), "Wi-Fi", 300m),
        new(new Guid("10000000-0000-0000-0000-000000000003"), "Sound", 700m)
    ];

    public static async Task SeedAsync(
        ConferenceRoomsDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var existingServiceIds = await dbContext.OptionalServices
            .Select(service => service.Id)
            .ToListAsync(cancellationToken);

        foreach (var service in ServiceSeeds.Where(service => !existingServiceIds.Contains(service.Id)))
        {
            dbContext.OptionalServices.Add(service);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var services = await dbContext.OptionalServices.ToListAsync(cancellationToken);
        var roomSeeds = new[]
        {
            new Room(
                new Guid("20000000-0000-0000-0000-000000000001"),
                "Room A",
                50,
                2000m,
                services,
                SeededAtUtc),
            new Room(
                new Guid("20000000-0000-0000-0000-000000000002"),
                "Room B",
                100,
                3500m,
                services,
                SeededAtUtc),
            new Room(
                new Guid("20000000-0000-0000-0000-000000000003"),
                "Room C",
                30,
                1500m,
                services,
                SeededAtUtc)
        };

        var existingRoomIds = await dbContext.Rooms
            .IgnoreQueryFilters()
            .Select(room => room.Id)
            .ToListAsync(cancellationToken);

        foreach (var room in roomSeeds.Where(room => !existingRoomIds.Contains(room.Id)))
        {
            dbContext.Rooms.Add(room);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
