using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConferenceRooms.Business.Contracts;
using ConferenceRooms.Tests.TestSupport;

namespace ConferenceRooms.Tests;

public sealed class ApiIntegrationTests(ConferenceRoomsApiFactory factory)
    : IClassFixture<ConferenceRoomsApiFactory>
{
    private static readonly Guid RoomAId = new("20000000-0000-0000-0000-000000000001");
    private static readonly Guid ProjectorId = new("10000000-0000-0000-0000-000000000001");
    private static readonly Guid WifiId = new("10000000-0000-0000-0000-000000000002");

    [Fact]
    public async Task PublicEndpoints_ExposeSwaggerAndSeededRooms()
    {
        using var client = factory.CreateClient();

        var swagger = await client.GetAsync("/swagger/v1/swagger.json");
        var swaggerDocument = await swagger.Content.ReadAsStringAsync();
        using var openApiDocument = JsonDocument.Parse(swaggerDocument);
        var paths = openApiDocument.RootElement.GetProperty("paths");
        JsonElement Operation(string path, string method) =>
            paths.GetProperty(path).GetProperty(method);
        JsonElement Parameter(JsonElement operation, string name) =>
            operation.GetProperty("parameters").EnumerateArray().Single(parameter =>
                parameter.GetProperty("name").GetString() == name);
        JsonElement BodyExample(JsonElement operation) =>
            operation.GetProperty("requestBody")
                .GetProperty("content")
                .GetProperty("application/json")
                .GetProperty("example");

        var roomList = Operation("/api/rooms", "get");
        var roomGet = Operation("/api/rooms/{id}", "get");
        var roomAvailability = Operation("/api/rooms/available", "get");
        var roomCreate = Operation("/api/rooms", "post");
        var roomUpdate = Operation("/api/rooms/{id}", "put");
        var roomDelete = Operation("/api/rooms/{id}", "delete");
        var bookingCreate = Operation("/api/bookings", "post");
        var roomCreateExample = BodyExample(roomCreate);
        var roomUpdateExample = BodyExample(roomUpdate);
        var bookingExample = BodyExample(bookingCreate);
        var rooms = await client.GetFromJsonAsync<List<RoomResponse>>("/api/rooms");

        Assert.Equal(HttpStatusCode.OK, swagger.StatusCode);
        Assert.DoesNotContain(ConferenceRoomsApiFactory.AdminKey, swaggerDocument);
        Assert.DoesNotContain(ConferenceRoomsApiFactory.CustomerKey, swaggerDocument);
        Assert.Contains("No input is required", roomList.GetProperty("description").GetString());
        Assert.Equal(RoomAId, Parameter(roomGet, "id").GetProperty("example").GetGuid());
        Assert.Equal(
            "2027-09-01T10:00:00+03:00",
            Parameter(roomAvailability, "startTime").GetProperty("example").GetString());
        Assert.Equal(2, Parameter(roomAvailability, "durationHours").GetProperty("example").GetInt32());
        Assert.Equal(50, Parameter(roomAvailability, "capacity").GetProperty("example").GetInt32());
        Assert.Equal("Room D", roomCreateExample.GetProperty("name").GetString());
        Assert.Equal(20, roomCreateExample.GetProperty("capacity").GetInt32());
        Assert.Equal("Room D Updated", roomUpdateExample.GetProperty("name").GetString());
        Assert.Equal(
            "30000000-0000-0000-0000-000000000001",
            Parameter(roomUpdate, "id").GetProperty("example").GetString());
        Assert.Equal(
            "30000000-0000-0000-0000-000000000001",
            Parameter(roomDelete, "id").GetProperty("example").GetString());
        Assert.Equal(RoomAId, bookingExample.GetProperty("roomId").GetGuid());
        Assert.Equal("2027-09-01T10:00:00+03:00", bookingExample.GetProperty("startTime").GetString());
        Assert.Equal(2, bookingExample.GetProperty("durationHours").GetInt32());
        Assert.Equal(ProjectorId, bookingExample.GetProperty("optionalServiceIds")[0].GetGuid());
        foreach (var reportPath in new[]
                 {
                     "/api/reports/revenue",
                     "/api/reports/utilization",
                     "/api/reports/services"
                 })
        {
            var report = Operation(reportPath, "get");
            Assert.Equal("2027-09-01", Parameter(report, "from").GetProperty("example").GetString());
            Assert.Equal("2027-09-01", Parameter(report, "to").GetProperty("example").GetString());
        }

        foreach (var (operation, status) in new[]
                 {
                     (roomList, "200"),
                     (roomGet, "200"),
                     (roomAvailability, "200"),
                     (roomCreate, "201"),
                     (roomUpdate, "200"),
                     (bookingCreate, "201"),
                     (Operation("/api/reports/revenue", "get"), "200"),
                     (Operation("/api/reports/utilization", "get"), "200"),
                     (Operation("/api/reports/services", "get"), "200")
                 })
        {
            Assert.DoesNotContain(
                operation.GetProperty("responses")
                    .GetProperty(status)
                    .GetProperty("content")
                    .EnumerateObject(),
                content => content.NameEquals("text/plain"));
        }

        Assert.NotNull(rooms);
        Assert.True(rooms.Count >= 3);
        Assert.Contains(rooms, room => room.Id == RoomAId && room.SupportedServices.Count == 3);
    }

    [Fact]
    public async Task RoomCreation_RequiresTheAdminRole()
    {
        var request = new
        {
            name = "Authorization Test Room",
            capacity = 10,
            baseHourlyRate = 1000m,
            optionalServiceIds = Array.Empty<Guid>()
        };

        using var anonymousClient = factory.CreateClient();
        using var customerClient = factory.CreateCustomerClient();
        using var adminClient = factory.CreateAdminClient();

        var anonymousResponse = await anonymousClient.PostAsJsonAsync("/api/rooms", request);
        var customerResponse = await customerClient.PostAsJsonAsync("/api/rooms", request);
        var adminResponse = await adminClient.PostAsJsonAsync("/api/rooms", request);

        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, customerResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, adminResponse.StatusCode);
    }

    [Fact]
    public async Task InvalidBooking_WithPlainTextAccept_ReturnsConsistentValidationProblem()
    {
        using var client = factory.CreateAdminClient();
        client.DefaultRequestHeaders.Accept.ParseAdd("text/plain");

        var response = await client.PostAsJsonAsync("/api/bookings", new
        {
            roomId = "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            startTime = "2026-08-25T16:17:24.647Z",
            durationHours = 17,
            optionalServiceIds = new[] { "3fa85f64-5717-4562-b3fc-2c963f66afa6" }
        });
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(400, problem.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Validation failed", problem.RootElement.GetProperty("title").GetString());
        Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty("startTime", out _));
    }

    [Fact]
    public async Task Booking_PreventsOverlapAndFeedsAvailabilityAndReports()
    {
        using var customerClient = factory.CreateCustomerClient();
        using var adminClient = factory.CreateAdminClient();
        var request = new
        {
            roomId = RoomAId,
            startTime = "2026-10-01T11:00:00+03:00",
            durationHours = 4,
            optionalServiceIds = new[] { ProjectorId, WifiId }
        };

        var createdResponse = await customerClient.PostAsJsonAsync("/api/bookings", request);
        var duplicateResponse = await customerClient.PostAsJsonAsync("/api/bookings", request);
        var booking = await createdResponse.Content.ReadFromJsonAsync<BookingResponse>();
        var availableRooms = await customerClient.GetFromJsonAsync<List<RoomResponse>>(
            "/api/rooms/available?startTime=2026-10-01T11%3A00%3A00%2B03%3A00&durationHours=4&capacity=40");
        var backToBackRooms = await customerClient.GetFromJsonAsync<List<RoomResponse>>(
            "/api/rooms/available?startTime=2026-10-01T15%3A00%3A00%2B03%3A00&durationHours=1&capacity=40");
        var revenue = await adminClient.GetFromJsonAsync<RevenueReportResponse>(
            "/api/reports/revenue?from=2026-10-01&to=2026-10-01");
        var services = await adminClient.GetFromJsonAsync<ServiceReportResponse>(
            "/api/reports/services?from=2026-10-01&to=2026-10-01");

        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
        Assert.NotNull(booking);
        Assert.Equal(8600m, booking.RoomCharge);
        Assert.Equal(800m, booking.ServiceCharge);
        Assert.Equal(9400m, booking.TotalCharge);
        Assert.Equal(["Standard", "Peak", "Peak", "Standard"],
            booking.HourlyPriceBreakdown.Select(line => line.RateType));

        Assert.NotNull(availableRooms);
        Assert.DoesNotContain(availableRooms, room => room.Id == RoomAId);
        Assert.NotNull(backToBackRooms);
        Assert.Contains(backToBackRooms, room => room.Id == RoomAId);

        Assert.NotNull(revenue);
        Assert.Equal(9400m, revenue.TotalRevenue);
        Assert.NotNull(services);
        Assert.Equal(2, services.Services.Count);
        Assert.Equal(800m, services.Services.Sum(service => service.Revenue));
    }

    [Fact]
    public async Task SoftDeletedRoom_IsTreatedAsMissing()
    {
        using var adminClient = factory.CreateAdminClient();
        var createResponse = await adminClient.PostAsJsonAsync("/api/rooms", new
        {
            name = "Room To Delete",
            capacity = 20,
            baseHourlyRate = 1200m,
            optionalServiceIds = new[] { ProjectorId }
        });
        var createdRoom = await createResponse.Content.ReadFromJsonAsync<RoomResponse>();

        Assert.NotNull(createdRoom);

        var firstDelete = await adminClient.DeleteAsync($"/api/rooms/{createdRoom.Id}");
        var getAfterDelete = await adminClient.GetAsync($"/api/rooms/{createdRoom.Id}");
        var secondDelete = await adminClient.DeleteAsync($"/api/rooms/{createdRoom.Id}");

        Assert.Equal(HttpStatusCode.NoContent, firstDelete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getAfterDelete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, secondDelete.StatusCode);
    }

    [Fact]
    public async Task RoomUpdate_CanKeepAndAddSupportedServices()
    {
        using var adminClient = factory.CreateAdminClient();
        var createResponse = await adminClient.PostAsJsonAsync("/api/rooms", new
        {
            name = "Room To Update",
            capacity = 25,
            baseHourlyRate = 1400m,
            optionalServiceIds = new[] { ProjectorId }
        });
        var createdRoom = await createResponse.Content.ReadFromJsonAsync<RoomResponse>();

        Assert.NotNull(createdRoom);

        var updateRequest = new
        {
            name = "Updated Room",
            capacity = 30,
            baseHourlyRate = 1600m,
            optionalServiceIds = new[] { ProjectorId, WifiId }
        };
        var firstUpdate = await adminClient.PutAsJsonAsync($"/api/rooms/{createdRoom.Id}", updateRequest);
        var secondUpdate = await adminClient.PutAsJsonAsync($"/api/rooms/{createdRoom.Id}", updateRequest);
        var updatedRoom = await secondUpdate.Content.ReadFromJsonAsync<RoomResponse>();

        Assert.Equal(HttpStatusCode.OK, firstUpdate.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondUpdate.StatusCode);
        Assert.NotNull(updatedRoom);
        Assert.Equal(2, updatedRoom.SupportedServices.Count);
    }
}
