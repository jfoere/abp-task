namespace ConferenceRooms.Tests.TestSupport;

internal static class KyivTimeZone
{
    public static TimeZoneInfo Get()
    {
        foreach (var id in new[] { "Europe/Kyiv", "Europe/Kiev", "FLE Standard Time" })
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var timeZone))
            {
                return timeZone;
            }
        }

        throw new TimeZoneNotFoundException("A Europe/Kyiv-compatible timezone is required for the tests.");
    }
}

