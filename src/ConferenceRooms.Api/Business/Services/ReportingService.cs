using ConferenceRooms.Business.Abstractions;
using ConferenceRooms.Business.Contracts;

namespace ConferenceRooms.Business.Services;

public interface IReportingService
{
    Task<RevenueReportResponse> GetRevenueAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken);

    Task<UtilizationReportResponse> GetUtilizationAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken);

    Task<ServiceReportResponse> GetServicesAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken);
}

public sealed class ReportingService(
    IReportRepository reports,
    BookingTimePolicy bookingTimePolicy) : IReportingService
{
    public async Task<RevenueReportResponse> GetRevenueAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var data = await LoadAsync(from, to, cancellationToken);
        var revenueByRoom = data.Rooms
            .Select(room => new RoomRevenueResponse(
                room.Id,
                room.Name,
                data.Bookings.Where(booking => booking.RoomId == room.Id).Sum(booking => booking.TotalCharge)))
            .OrderByDescending(room => room.Revenue)
            .ThenBy(room => room.RoomName)
            .ToList();

        return new RevenueReportResponse(
            from,
            to,
            revenueByRoom.Sum(room => room.Revenue),
            revenueByRoom);
    }

    public async Task<UtilizationReportResponse> GetUtilizationAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var data = await LoadAsync(from, to, cancellationToken);
        var dayCount = to.DayNumber - from.DayNumber + 1;
        var availableHours = dayCount * (BookingTimePolicy.ClosingHour - BookingTimePolicy.OpeningHour);

        var rooms = data.Rooms
            .Select(room =>
            {
                var bookedHours = data.Bookings
                    .Where(booking => booking.RoomId == room.Id)
                    .Sum(booking => (int)(booking.EndUtc - booking.StartUtc).TotalHours);

                var utilization = availableHours == 0
                    ? 0
                    : decimal.Round((decimal)bookedHours / availableHours * 100, 2);

                return new RoomUtilizationResponse(
                    room.Id,
                    room.Name,
                    bookedHours,
                    availableHours,
                    utilization);
            })
            .OrderByDescending(room => room.UtilizationPercent)
            .ThenBy(room => room.RoomName)
            .ToList();

        return new UtilizationReportResponse(from, to, rooms);
    }

    public async Task<ServiceReportResponse> GetServicesAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var data = await LoadAsync(from, to, cancellationToken);
        var services = data.Bookings
            .SelectMany(booking => booking.SelectedServices)
            .GroupBy(service => new { service.OptionalServiceId, service.NameSnapshot })
            .Select(group => new ServiceUsageResponse(
                group.Key.OptionalServiceId,
                group.Key.NameSnapshot,
                group.Count(),
                group.Sum(service => service.PriceSnapshot)))
            .OrderByDescending(service => service.BookingCount)
            .ThenBy(service => service.ServiceName)
            .ToList();

        return new ServiceReportResponse(from, to, services);
    }

    private async Task<ReportData> LoadAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var (startUtc, endUtc) = bookingTimePolicy.GetUtcDateRange(from, to);
        var rooms = await reports.ListAllRoomsAsync(cancellationToken);
        var bookings = await reports.ListBookingsStartingBetweenAsync(
            startUtc,
            endUtc,
            cancellationToken);

        return new ReportData(rooms, bookings);
    }

    private sealed record ReportData(
        IReadOnlyList<Domain.Room> Rooms,
        IReadOnlyList<Domain.Booking> Bookings);
}
