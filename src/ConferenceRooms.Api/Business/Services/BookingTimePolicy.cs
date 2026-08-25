using ConferenceRooms.Business.Common;

namespace ConferenceRooms.Business.Services;

public sealed class BookingTimePolicy(TimeZoneInfo businessTimeZone, string? configuredTimeZoneId = null)
{
    public const int OpeningHour = 6;
    public const int ClosingHour = 23;

    public TimeZoneInfo BusinessTimeZone { get; } = businessTimeZone;

    public string BusinessTimeZoneId { get; } = configuredTimeZoneId ?? businessTimeZone.Id;

    public BookingPeriod Validate(DateTimeOffset requestedStart, int durationHours)
    {
        if (durationHours < 1 || durationHours > ClosingHour - OpeningHour)
        {
            throw new RequestValidationException(
                "durationHours",
                $"Duration must be between 1 and {ClosingHour - OpeningHour} whole hours.");
        }

        var expectedOffset = BusinessTimeZone.GetUtcOffset(requestedStart.UtcDateTime);
        if (requestedStart.Offset != expectedOffset)
        {
            throw new RequestValidationException(
                "startTime",
                $"The timestamp offset must match the {BusinessTimeZoneId} business timezone.");
        }

        var localStart = TimeZoneInfo.ConvertTime(requestedStart, BusinessTimeZone);

        if (localStart.Minute != 0 || localStart.Second != 0 || localStart.Millisecond != 0)
        {
            throw new RequestValidationException(
                "startTime",
                "Bookings must start on a full hour.");
        }

        var localEnd = localStart.AddHours(durationHours);
        if (localStart.Date != localEnd.Date
            || localStart.Hour < OpeningHour
            || localEnd.Hour > ClosingHour)
        {
            throw new RequestValidationException(
                "startTime",
                $"Bookings must remain within {OpeningHour:00}:00–{ClosingHour:00}:00 on one business day.");
        }

        return new BookingPeriod(
            requestedStart.UtcDateTime,
            requestedStart.UtcDateTime.AddHours(durationHours),
            localStart,
            localEnd,
            durationHours);
    }

    public (DateTime StartUtc, DateTime EndUtc) GetUtcDateRange(DateOnly from, DateOnly to)
    {
        if (to < from)
        {
            throw new RequestValidationException("to", "The end date must be on or after the start date.");
        }

        if (to.DayNumber - from.DayNumber > 366)
        {
            throw new RequestValidationException("to", "The report range cannot exceed 367 days.");
        }

        var localStart = DateTime.SpecifyKind(from.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        var localEnd = DateTime.SpecifyKind(to.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);

        return (
            TimeZoneInfo.ConvertTimeToUtc(localStart, BusinessTimeZone),
            TimeZoneInfo.ConvertTimeToUtc(localEnd, BusinessTimeZone));
    }
}

public sealed record BookingPeriod(
    DateTime StartUtc,
    DateTime EndUtc,
    DateTimeOffset LocalStart,
    DateTimeOffset LocalEnd,
    int DurationHours);
