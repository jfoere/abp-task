using ConferenceRooms.Business.Domain;

namespace ConferenceRooms.Business.Abstractions;

public interface IRoomRepository
{
    Task<IReadOnlyList<Room>> ListActiveAsync(CancellationToken cancellationToken);

    Task<Room?> GetActiveAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ActiveNameExistsAsync(
        string name,
        Guid? excludedRoomId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OptionalService>> GetOptionalServicesAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Room>> FindAvailableAsync(
        DateTime startUtc,
        DateTime endUtc,
        int capacity,
        CancellationToken cancellationToken);

    Task AddAsync(Room room, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IBookingRepository
{
    Task<bool> HasOverlapAsync(
        Guid roomId,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken);

    Task AddAsync(Booking booking, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<T> ExecuteSerializableAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken);
}

public interface IReportRepository
{
    Task<IReadOnlyList<Room>> ListAllRoomsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Booking>> ListBookingsStartingBetweenAsync(
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken);
}
