using ConferenceRooms.Business.Abstractions;
using ConferenceRooms.Business.Domain;
using ConferenceRooms.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRooms.Data.Repositories;

internal sealed class RoomRepository(ConferenceRoomsDbContext dbContext) : IRoomRepository
{
    public async Task<IReadOnlyList<Room>> ListActiveAsync(CancellationToken cancellationToken) =>
        await RoomQuery(asTracking: false)
            .OrderBy(room => room.Name)
            .ToListAsync(cancellationToken);

    public Task<Room?> GetActiveAsync(Guid id, CancellationToken cancellationToken) =>
        RoomQuery(asTracking: true).SingleOrDefaultAsync(room => room.Id == id, cancellationToken);

    public Task<bool> ActiveNameExistsAsync(
        string name,
        Guid? excludedRoomId,
        CancellationToken cancellationToken) =>
        dbContext.Rooms.AnyAsync(
            room => EF.Functions.Collate(room.Name, "NOCASE") == name
                && (!excludedRoomId.HasValue || room.Id != excludedRoomId.Value),
            cancellationToken);

    public async Task<IReadOnlyList<OptionalService>> GetOptionalServicesAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken) =>
        await dbContext.OptionalServices
            .Where(service => ids.Contains(service.Id))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Room>> FindAvailableAsync(
        DateTime startUtc,
        DateTime endUtc,
        int capacity,
        CancellationToken cancellationToken) =>
        await RoomQuery(asTracking: false)
            .Where(room => room.Capacity >= capacity)
            .Where(room => !dbContext.Bookings.Any(
                booking => booking.RoomId == room.Id
                    && booking.StartUtc < endUtc
                    && startUtc < booking.EndUtc))
            .OrderBy(room => room.Capacity)
            .ThenBy(room => room.Name)
            .ToListAsync(cancellationToken);

    public Task AddAsync(Room room, CancellationToken cancellationToken) =>
        dbContext.Rooms.AddAsync(room, cancellationToken).AsTask();

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Room> RoomQuery(bool asTracking)
    {
        var query = dbContext.Rooms
            .Include(room => room.SupportedServices)
            .ThenInclude(link => link.OptionalService)
            .AsSplitQuery();

        return asTracking ? query : query.AsNoTracking();
    }
}
