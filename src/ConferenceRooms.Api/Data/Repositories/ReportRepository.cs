using ConferenceRooms.Business.Abstractions;
using ConferenceRooms.Business.Domain;
using ConferenceRooms.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRooms.Data.Repositories;

internal sealed class ReportRepository(ConferenceRoomsDbContext dbContext) : IReportRepository
{
    public async Task<IReadOnlyList<Room>> ListAllRoomsAsync(CancellationToken cancellationToken) =>
        await dbContext.Rooms
            .IgnoreQueryFilters()
            .AsNoTracking()
            .OrderBy(room => room.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Booking>> ListBookingsStartingBetweenAsync(
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken) =>
        await dbContext.Bookings
            .Include(booking => booking.SelectedServices)
            .AsNoTracking()
            .Where(booking => booking.StartUtc >= startUtc && booking.StartUtc < endUtc)
            .ToListAsync(cancellationToken);
}
