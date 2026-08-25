using ConferenceRooms.Business.Contracts;

namespace ConferenceRooms.Business.Services;

public sealed class PricingCalculator
{
    public RoomPrice Calculate(decimal baseHourlyRate, BookingPeriod period)
    {
        var lines = new List<HourlyPriceLineResponse>(period.DurationHours);

        for (var hourIndex = 0; hourIndex < period.DurationHours; hourIndex++)
        {
            var segmentStart = period.LocalStart.AddHours(hourIndex);
            var (rateType, multiplier) = GetRate(segmentStart.Hour);
            var charge = RoundMoney(baseHourlyRate * multiplier);

            lines.Add(
                new HourlyPriceLineResponse(
                    segmentStart,
                    segmentStart.AddHours(1),
                    rateType,
                    multiplier,
                    charge));
        }

        return new RoomPrice(lines, lines.Sum(line => line.Charge));
    }

    private static (string RateType, decimal Multiplier) GetRate(int hour) => hour switch
    {
        >= 6 and < 9 => ("Morning", 0.90m),
        >= 12 and < 14 => ("Peak", 1.15m),
        >= 18 and < 23 => ("Evening", 0.80m),
        _ => ("Standard", 1.00m)
    };

    private static decimal RoundMoney(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}

public sealed record RoomPrice(
    IReadOnlyList<HourlyPriceLineResponse> Lines,
    decimal Total);
