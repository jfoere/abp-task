using ConferenceRooms.Business.Abstractions;
using ConferenceRooms.Business.Common;
using ConferenceRooms.Business.Contracts;
using ConferenceRooms.Business.Domain;

namespace ConferenceRooms.Business.Services;

public interface IRoomManagementService
{
    Task<IReadOnlyList<RoomResponse>> ListAsync(CancellationToken cancellationToken);

    Task<RoomResponse> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<RoomResponse> CreateAsync(CreateRoomCommand command, CancellationToken cancellationToken);

    Task<RoomResponse> UpdateAsync(Guid id, UpdateRoomCommand command, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<RoomResponse>> FindAvailableAsync(
        AvailabilityQuery query,
        CancellationToken cancellationToken);
}

public sealed class RoomManagementService(
    IRoomRepository rooms,
    BookingTimePolicy bookingTimePolicy) : IRoomManagementService
{
    public async Task<IReadOnlyList<RoomResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var result = await rooms.ListActiveAsync(cancellationToken);
        return result.Select(Map).ToList();
    }

    public async Task<RoomResponse> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var room = await GetRequiredRoomAsync(id, cancellationToken);
        return Map(room);
    }

    public async Task<RoomResponse> CreateAsync(
        CreateRoomCommand command,
        CancellationToken cancellationToken)
    {
        ValidateRoom(command.Name, command.Capacity, command.BaseHourlyRate, command.OptionalServiceIds);

        if (await rooms.ActiveNameExistsAsync(command.Name.Trim(), null, cancellationToken))
        {
            throw new ResourceConflictException($"An active room named '{command.Name.Trim()}' already exists.");
        }

        var services = await ResolveServicesAsync(command.OptionalServiceIds, cancellationToken);
        var room = new Room(
            Guid.NewGuid(),
            command.Name.Trim(),
            command.Capacity,
            command.BaseHourlyRate,
            services,
            DateTime.UtcNow);

        await rooms.AddAsync(room, cancellationToken);
        await rooms.SaveChangesAsync(cancellationToken);

        return Map(room);
    }

    public async Task<RoomResponse> UpdateAsync(
        Guid id,
        UpdateRoomCommand command,
        CancellationToken cancellationToken)
    {
        ValidateRoom(command.Name, command.Capacity, command.BaseHourlyRate, command.OptionalServiceIds);
        var room = await GetRequiredRoomAsync(id, cancellationToken);

        if (await rooms.ActiveNameExistsAsync(command.Name.Trim(), id, cancellationToken))
        {
            throw new ResourceConflictException($"An active room named '{command.Name.Trim()}' already exists.");
        }

        var services = await ResolveServicesAsync(command.OptionalServiceIds, cancellationToken);
        room.Update(command.Name.Trim(), command.Capacity, command.BaseHourlyRate, services);
        await rooms.SaveChangesAsync(cancellationToken);

        return Map(room);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var room = await GetRequiredRoomAsync(id, cancellationToken);
        room.SoftDelete(DateTime.UtcNow);
        await rooms.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RoomResponse>> FindAvailableAsync(
        AvailabilityQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Capacity < 1)
        {
            throw new RequestValidationException("capacity", "Capacity must be greater than zero.");
        }

        var period = bookingTimePolicy.Validate(query.StartTime, query.DurationHours);
        var availableRooms = await rooms.FindAvailableAsync(
            period.StartUtc,
            period.EndUtc,
            query.Capacity,
            cancellationToken);

        return availableRooms.Select(Map).ToList();
    }

    private async Task<Room> GetRequiredRoomAsync(Guid id, CancellationToken cancellationToken) =>
        await rooms.GetActiveAsync(id, cancellationToken)
        ?? throw new ResourceNotFoundException($"Room '{id}' was not found.");

    private async Task<IReadOnlyList<OptionalService>> ResolveServicesAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var services = await rooms.GetOptionalServicesAsync(ids, cancellationToken);
        if (services.Count != ids.Count)
        {
            throw new RequestValidationException(
                "optionalServiceIds",
                "One or more selected optional services do not exist.");
        }

        return services;
    }

    private static void ValidateRoom(
        string name,
        int capacity,
        decimal baseHourlyRate,
        IReadOnlyCollection<Guid> optionalServiceIds)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100)
        {
            errors["name"] = ["Name is required and cannot exceed 100 characters."];
        }

        if (capacity < 1)
        {
            errors["capacity"] = ["Capacity must be greater than zero."];
        }

        if (baseHourlyRate <= 0)
        {
            errors["baseHourlyRate"] = ["The base hourly rate must be greater than zero."];
        }

        if (optionalServiceIds.Count != optionalServiceIds.Distinct().Count())
        {
            errors["optionalServiceIds"] = ["Optional service IDs cannot contain duplicates."];
        }

        if (errors.Count > 0)
        {
            throw new RequestValidationException(errors);
        }
    }

    private static RoomResponse Map(Room room) =>
        new(
            room.Id,
            room.Name,
            room.Capacity,
            room.BaseHourlyRate,
            room.SupportedServices
                .Select(link => link.OptionalService)
                .OrderBy(service => service.Name)
                .Select(service => new OptionalServiceResponse(service.Id, service.Name, service.Price))
                .ToList());
}
