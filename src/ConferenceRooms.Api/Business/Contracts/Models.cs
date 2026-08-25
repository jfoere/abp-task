namespace ConferenceRooms.Business.Contracts;

public sealed record CreateRoomCommand(
    string Name,
    int Capacity,
    decimal BaseHourlyRate,
    IReadOnlyCollection<Guid> OptionalServiceIds);

public sealed record UpdateRoomCommand(
    string Name,
    int Capacity,
    decimal BaseHourlyRate,
    IReadOnlyCollection<Guid> OptionalServiceIds);

public sealed record RoomResponse(
    Guid Id,
    string Name,
    int Capacity,
    decimal BaseHourlyRate,
    IReadOnlyList<OptionalServiceResponse> SupportedServices);

public sealed record OptionalServiceResponse(Guid Id, string Name, decimal Price);

public sealed record AvailabilityQuery(
    DateTimeOffset StartTime,
    int DurationHours,
    int Capacity);

public sealed record CreateBookingCommand(
    Guid RoomId,
    DateTimeOffset StartTime,
    int DurationHours,
    IReadOnlyCollection<Guid> OptionalServiceIds,
    string CreatedBy);

public sealed record BookingResponse(
    Guid Id,
    Guid RoomId,
    DateTime StartUtc,
    DateTime EndUtc,
    IReadOnlyList<HourlyPriceLineResponse> HourlyPriceBreakdown,
    IReadOnlyList<SelectedServiceResponse> SelectedServices,
    decimal RoomCharge,
    decimal ServiceCharge,
    decimal TotalCharge);

public sealed record HourlyPriceLineResponse(
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string RateType,
    decimal Multiplier,
    decimal Charge);

public sealed record SelectedServiceResponse(Guid Id, string Name, decimal Price);

public sealed record RevenueReportResponse(
    DateOnly From,
    DateOnly To,
    decimal TotalRevenue,
    IReadOnlyList<RoomRevenueResponse> Rooms);

public sealed record RoomRevenueResponse(Guid RoomId, string RoomName, decimal Revenue);

public sealed record UtilizationReportResponse(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<RoomUtilizationResponse> Rooms);

public sealed record RoomUtilizationResponse(
    Guid RoomId,
    string RoomName,
    int BookedHours,
    int AvailableHours,
    decimal UtilizationPercent);

public sealed record ServiceReportResponse(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<ServiceUsageResponse> Services);

public sealed record ServiceUsageResponse(
    Guid ServiceId,
    string ServiceName,
    int BookingCount,
    decimal Revenue);
