using System.ComponentModel.DataAnnotations;
using ConferenceRooms.Api.Auth;
using ConferenceRooms.Business.Contracts;
using ConferenceRooms.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ConferenceRooms.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Policy = AuthorizationPolicies.Admin)]
[EnableRateLimiting(RateLimitPolicies.Protected)]
public sealed class ReportsController(IReportingService reports) : ControllerBase
{
    /// <summary>Returns total revenue and revenue by room for the date range.</summary>
    [HttpGet("revenue")]
    [ProducesResponseType<RevenueReportResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RevenueReportResponse>> GetRevenue(
        [FromQuery, Required] DateOnly? from,
        [FromQuery, Required] DateOnly? to,
        CancellationToken cancellationToken) =>
        Ok(await reports.GetRevenueAsync(from!.Value, to!.Value, cancellationToken));

    /// <summary>Returns booked hours and utilization percentage by room.</summary>
    [HttpGet("utilization")]
    [ProducesResponseType<UtilizationReportResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UtilizationReportResponse>> GetUtilization(
        [FromQuery, Required] DateOnly? from,
        [FromQuery, Required] DateOnly? to,
        CancellationToken cancellationToken) =>
        Ok(await reports.GetUtilizationAsync(from!.Value, to!.Value, cancellationToken));

    /// <summary>Returns optional-service usage and revenue for the date range.</summary>
    [HttpGet("services")]
    [ProducesResponseType<ServiceReportResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ServiceReportResponse>> GetServices(
        [FromQuery, Required] DateOnly? from,
        [FromQuery, Required] DateOnly? to,
        CancellationToken cancellationToken) =>
        Ok(await reports.GetServicesAsync(from!.Value, to!.Value, cancellationToken));
}
