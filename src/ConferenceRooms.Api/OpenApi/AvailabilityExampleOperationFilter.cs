using ConferenceRooms.Api.Controllers;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ConferenceRooms.Api.OpenApi;

public sealed class AvailabilityExampleOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(RoomsController)
            || context.MethodInfo.Name != nameof(RoomsController.FindAvailable))
        {
            return;
        }

        var examples = new Dictionary<string, IOpenApiAny>(StringComparer.OrdinalIgnoreCase)
        {
            ["startTime"] = new OpenApiString("2027-09-01T10:00:00+03:00"),
            ["durationHours"] = new OpenApiInteger(4),
            ["capacity"] = new OpenApiInteger(50)
        };

        foreach (var parameter in operation.Parameters)
        {
            if (examples.TryGetValue(parameter.Name, out var example))
            {
                parameter.Example = example;
            }
        }
    }
}
