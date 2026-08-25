using ConferenceRooms.Api.Controllers;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ConferenceRooms.Api.OpenApi;

public sealed class ApiInputExamplesOperationFilter : IOperationFilter
{
    private const string RoomAId = "20000000-0000-0000-0000-000000000001";
    private const string CreatedRoomIdPlaceholder = "30000000-0000-0000-0000-000000000001";
    private const string ProjectorId = "10000000-0000-0000-0000-000000000001";
    private const string WifiId = "10000000-0000-0000-0000-000000000002";
    private const string SoundId = "10000000-0000-0000-0000-000000000003";
    private const string ExampleStartTime = "2027-09-01T10:00:00+03:00";
    private const string ExampleDate = "2027-09-01";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var controller = context.MethodInfo.DeclaringType;
        var action = context.MethodInfo.Name;

        if (controller == typeof(RoomsController))
        {
            ApplyRoomExamples(operation, action);
        }
        else if (controller == typeof(BookingsController))
        {
            ApplyBookingExamples(operation, action);
        }
        else if (controller == typeof(ReportsController))
        {
            ApplyReportExamples(operation);
        }
    }

    private static void ApplyRoomExamples(OpenApiOperation operation, string action)
    {
        switch (action)
        {
            case nameof(RoomsController.List):
                operation.Description = "No input is required. Click **Execute** to list the seeded rooms.";
                break;

            case nameof(RoomsController.Get):
                SetParameter(operation, "id", new OpenApiString(RoomAId));
                operation.Description = "The example ID returns seeded Room A.";
                break;

            case nameof(RoomsController.FindAvailable):
                SetParameter(operation, "startTime", new OpenApiString(ExampleStartTime));
                SetParameter(operation, "durationHours", new OpenApiInteger(2));
                SetParameter(operation, "capacity", new OpenApiInteger(50));
                operation.Description = "The example searches for a two-hour slot using a valid Kyiv offset.";
                break;

            case nameof(RoomsController.Create):
                SetJsonBodyExample(operation, CreateRoomExample());
                operation.Description = "The example creates Room D. Change its name before running it again.";
                break;

            case nameof(RoomsController.Update):
                SetParameter(operation, "id", new OpenApiString(CreatedRoomIdPlaceholder));
                SetJsonBodyExample(operation, UpdateRoomExample());
                operation.Description = "Replace the example ID with the ID returned by **POST /api/rooms**.";
                break;

            case nameof(RoomsController.Delete):
                SetParameter(operation, "id", new OpenApiString(CreatedRoomIdPlaceholder));
                operation.Description = "Replace the example ID with the ID returned by **POST /api/rooms**. "
                    + "The placeholder intentionally does not identify a seeded room.";
                break;
        }
    }

    private static void ApplyBookingExamples(OpenApiOperation operation, string action)
    {
        if (action != nameof(BookingsController.Create))
        {
            return;
        }

        SetJsonBodyExample(operation, new OpenApiObject
        {
            ["roomId"] = new OpenApiString(RoomAId),
            ["startTime"] = new OpenApiString(ExampleStartTime),
            ["durationHours"] = new OpenApiInteger(2),
            ["optionalServiceIds"] = new OpenApiArray
            {
                new OpenApiString(ProjectorId)
            }
        });
        operation.Description = "The example books seeded Room A with a Projector. "
            + "After the first successful booking, change the date or time before running it again.";
    }

    private static void ApplyReportExamples(OpenApiOperation operation)
    {
        SetParameter(operation, "from", new OpenApiString(ExampleDate));
        SetParameter(operation, "to", new OpenApiString(ExampleDate));
        operation.Description = "The example reports on the same date used by the booking example.";
    }

    private static OpenApiObject CreateRoomExample() => new()
    {
        ["name"] = new OpenApiString("Room D"),
        ["capacity"] = new OpenApiInteger(20),
        ["baseHourlyRate"] = new OpenApiDouble(1800),
        ["optionalServiceIds"] = new OpenApiArray
        {
            new OpenApiString(ProjectorId),
            new OpenApiString(WifiId)
        }
    };

    private static OpenApiObject UpdateRoomExample() => new()
    {
        ["name"] = new OpenApiString("Room D Updated"),
        ["capacity"] = new OpenApiInteger(30),
        ["baseHourlyRate"] = new OpenApiDouble(2200),
        ["optionalServiceIds"] = new OpenApiArray
        {
            new OpenApiString(ProjectorId),
            new OpenApiString(WifiId),
            new OpenApiString(SoundId)
        }
    };

    private static void SetParameter(OpenApiOperation operation, string name, IOpenApiAny example)
    {
        var parameter = operation.Parameters.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase));

        if (parameter is not null)
        {
            parameter.Example = example;
        }
    }

    private static void SetJsonBodyExample(OpenApiOperation operation, IOpenApiAny example)
    {
        if (operation.RequestBody?.Content.TryGetValue("application/json", out var jsonContent) == true)
        {
            jsonContent.Example = example;
        }
    }
}
