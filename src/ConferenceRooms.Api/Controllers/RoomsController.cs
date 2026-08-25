using ConferenceRooms.Api.Auth;
using ConferenceRooms.Api.Models;
using ConferenceRooms.Business.Contracts;
using ConferenceRooms.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ConferenceRooms.Api.Controllers;

[ApiController]
[Route("api/rooms")]
[Produces("application/json")]
public sealed class RoomsController(IRoomManagementService rooms) : ControllerBase
{
    /// <summary>Returns the active room catalog.</summary>
    [HttpGet]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Public)]
    [ProducesResponseType<IReadOnlyList<RoomResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RoomResponse>>> List(CancellationToken cancellationToken) =>
        Ok(await rooms.ListAsync(cancellationToken));

    /// <summary>Returns one active room.</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Public)]
    [ProducesResponseType<RoomResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoomResponse>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await rooms.GetAsync(id, cancellationToken));

    /// <summary>Finds active rooms with enough capacity and no overlapping booking.</summary>
    [HttpGet("available")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Public)]
    [ProducesResponseType<IReadOnlyList<RoomResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<RoomResponse>>> FindAvailable(
        [FromQuery] DateTimeOffset startTime,
        [FromQuery] int durationHours,
        [FromQuery] int capacity,
        CancellationToken cancellationToken) =>
        Ok(await rooms.FindAvailableAsync(
            new AvailabilityQuery(startTime, durationHours, capacity),
            cancellationToken));

    /// <summary>Creates a conference room.</summary>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    [EnableRateLimiting(RateLimitPolicies.Protected)]
    [ProducesResponseType<RoomResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoomResponse>> Create(
        CreateRoomRequest request,
        CancellationToken cancellationToken)
    {
        var response = await rooms.CreateAsync(
            new CreateRoomCommand(
                request.Name,
                request.Capacity,
                request.BaseHourlyRate,
                request.OptionalServiceIds ?? []),
            cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = response.Id }, response);
    }

    /// <summary>Replaces an active room's editable information.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    [EnableRateLimiting(RateLimitPolicies.Protected)]
    [ProducesResponseType<RoomResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoomResponse>> Update(
        Guid id,
        UpdateRoomRequest request,
        CancellationToken cancellationToken) =>
        Ok(await rooms.UpdateAsync(
            id,
            new UpdateRoomCommand(
                request.Name,
                request.Capacity,
                request.BaseHourlyRate,
                request.OptionalServiceIds ?? []),
            cancellationToken));

    /// <summary>Soft-deletes an active room.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    [EnableRateLimiting(RateLimitPolicies.Protected)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await rooms.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
