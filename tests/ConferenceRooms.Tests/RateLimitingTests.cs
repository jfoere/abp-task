using System.Net;
using System.Net.Http.Json;
using ConferenceRooms.Tests.TestSupport;

namespace ConferenceRooms.Tests;

public sealed class RateLimitingTests(ConferenceRoomsApiFactory factory)
    : IClassFixture<ConferenceRoomsApiFactory>
{
    [Fact]
    public async Task AnonymousProtectedRequests_AreRateLimitedAfterTenAttempts()
    {
        using var client = factory.CreateClient();
        var statuses = new List<HttpStatusCode>();

        for (var attempt = 0; attempt < 11; attempt++)
        {
            var response = await client.PostAsJsonAsync("/api/bookings", new
            {
                roomId = "20000000-0000-0000-0000-000000000001",
                startTime = "2026-11-01T11:00:00+02:00",
                durationHours = 1,
                optionalServiceIds = Array.Empty<Guid>()
            });
            statuses.Add(response.StatusCode);
        }

        Assert.All(statuses.Take(10), status => Assert.Equal(HttpStatusCode.Unauthorized, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[10]);
    }
}
