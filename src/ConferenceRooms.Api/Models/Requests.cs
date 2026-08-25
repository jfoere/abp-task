using System.ComponentModel.DataAnnotations;

namespace ConferenceRooms.Api.Models;

public sealed class CreateRoomRequest
{
    [Required, MaxLength(100)]
    public string Name { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Capacity { get; init; }

    [Range(typeof(decimal), "0.01", "9999999999999999")]
    public decimal BaseHourlyRate { get; init; }

    public IReadOnlyCollection<Guid>? OptionalServiceIds { get; init; } = [];
}

public sealed class UpdateRoomRequest
{
    [Required, MaxLength(100)]
    public string Name { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Capacity { get; init; }

    [Range(typeof(decimal), "0.01", "9999999999999999")]
    public decimal BaseHourlyRate { get; init; }

    public IReadOnlyCollection<Guid>? OptionalServiceIds { get; init; } = [];
}

public sealed class CreateBookingRequest
{
    public Guid RoomId { get; init; }

    public DateTimeOffset StartTime { get; init; }

    [Range(1, 17)]
    public int DurationHours { get; init; }

    public IReadOnlyCollection<Guid>? OptionalServiceIds { get; init; } = [];
}
