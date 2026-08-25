using ConferenceRooms.Api.Auth;
using ConferenceRooms.Api.Models;
using ConferenceRooms.Business.Contracts;
using ConferenceRooms.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ConferenceRooms.Api.Controllers;

[ApiController]
[Route("api/bookings")]
[Authorize(Policy = AuthorizationPolicies.CustomerOrAdmin)]
[EnableRateLimiting(RateLimitPolicies.Protected)]
[Produces("application/json")]
public sealed class BookingsController(IBookingManagementService bookings) : ControllerBase
{
    /// <summary>Creates a booking and returns its immutable price snapshot.</summary>
    [HttpPost]
    [ProducesResponseType<BookingResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingResponse>> Create(
        CreateBookingRequest request,
        CancellationToken cancellationToken)
    {
        var response = await bookings.CreateAsync(
            new CreateBookingCommand(
                request.RoomId,
                request.StartTime,
                request.DurationHours,
                request.OptionalServiceIds ?? [],
                User.Identity!.Name!),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }
}
