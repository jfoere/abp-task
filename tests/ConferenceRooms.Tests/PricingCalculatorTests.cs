using ConferenceRooms.Business.Common;
using ConferenceRooms.Business.Services;
using ConferenceRooms.Tests.TestSupport;

namespace ConferenceRooms.Tests;

public sealed class PricingCalculatorTests
{
    private readonly BookingTimePolicy timePolicy = new(KyivTimeZone.Get(), "Europe/Kyiv");
    private readonly PricingCalculator calculator = new();

    [Fact]
    public void Calculate_SplitsAReservationAcrossRatePeriods()
    {
        var period = timePolicy.Validate(
            DateTimeOffset.Parse("2026-09-01T11:00:00+03:00"),
            4);

        var result = calculator.Calculate(2000m, period);

        Assert.Equal(8600m, result.Total);
        Assert.Equal(["Standard", "Peak", "Peak", "Standard"], result.Lines.Select(line => line.RateType));
        Assert.Equal([2000m, 2300m, 2300m, 2000m], result.Lines.Select(line => line.Charge));
    }

    [Theory]
    [InlineData("2026-09-01T05:00:00+03:00", 1)]
    [InlineData("2026-09-01T22:00:00+03:00", 2)]
    [InlineData("2026-09-01T10:30:00+03:00", 1)]
    public void Validate_RejectsTimesOutsideTheBookingPolicy(string startTime, int durationHours)
    {
        Assert.Throws<RequestValidationException>(() =>
            timePolicy.Validate(DateTimeOffset.Parse(startTime), durationHours));
    }

    [Fact]
    public void Validate_RejectsAnOffsetThatDoesNotMatchTheBusinessTimezone()
    {
        Assert.Throws<RequestValidationException>(() =>
            timePolicy.Validate(DateTimeOffset.Parse("2026-09-01T10:00:00+00:00"), 1));
    }
}
