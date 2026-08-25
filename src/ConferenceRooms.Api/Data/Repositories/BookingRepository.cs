using System.Data;
using ConferenceRooms.Business.Abstractions;
using ConferenceRooms.Business.Domain;
using ConferenceRooms.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRooms.Data.Repositories;

internal sealed class BookingRepository(ConferenceRoomsDbContext dbContext) : IBookingRepository
{
    private static readonly SemaphoreSlim ProcessBookingGate = new(1, 1);

    public Task<bool> HasOverlapAsync(
        Guid roomId,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken) =>
        dbContext.Bookings.AnyAsync(
            booking => booking.RoomId == roomId
                && booking.StartUtc < endUtc
                && startUtc < booking.EndUtc,
            cancellationToken);

    public Task AddAsync(Booking booking, CancellationToken cancellationToken) =>
        dbContext.Bookings.AddAsync(booking, cancellationToken).AsTask();

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<T> ExecuteSerializableAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        await ProcessBookingGate.WaitAsync(cancellationToken);

        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            try
            {
                var result = await action(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        finally
        {
            ProcessBookingGate.Release();
        }
    }
}
