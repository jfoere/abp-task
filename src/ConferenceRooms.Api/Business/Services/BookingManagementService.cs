using ConferenceRooms.Business.Abstractions;
using ConferenceRooms.Business.Common;
using ConferenceRooms.Business.Contracts;
using ConferenceRooms.Business.Domain;

namespace ConferenceRooms.Business.Services;

public interface IBookingManagementService
{
    Task<BookingResponse> CreateAsync(CreateBookingCommand command, CancellationToken cancellationToken);
}

public sealed class BookingManagementService(
    IRoomRepository rooms,
    IBookingRepository bookings,
    BookingTimePolicy bookingTimePolicy,
    PricingCalculator pricingCalculator) : IBookingManagementService
{
    public Task<BookingResponse> CreateAsync(
        CreateBookingCommand command,
        CancellationToken cancellationToken)
    {
        ValidateCommand(command);
        var period = bookingTimePolicy.Validate(command.StartTime, command.DurationHours);

        return bookings.ExecuteSerializableAsync(
            transactionCancellationToken => CreateInsideTransactionAsync(
                command,
                period,
                transactionCancellationToken),
            cancellationToken);
    }

    private async Task<BookingResponse> CreateInsideTransactionAsync(
        CreateBookingCommand command,
        BookingPeriod period,
        CancellationToken cancellationToken)
    {
        var room = await rooms.GetActiveAsync(command.RoomId, cancellationToken)
            ?? throw new ResourceNotFoundException($"Room '{command.RoomId}' was not found.");

        if (await bookings.HasOverlapAsync(room.Id, period.StartUtc, period.EndUtc, cancellationToken))
        {
            throw new ResourceConflictException("The room is already booked during the requested period.");
        }

        var supportedServices = room.SupportedServices
            .Select(link => link.OptionalService)
            .ToDictionary(service => service.Id);

        var unsupportedServiceIds = command.OptionalServiceIds
            .Where(id => !supportedServices.ContainsKey(id))
            .ToList();

        if (unsupportedServiceIds.Count > 0)
        {
            throw new RequestValidationException(
                "optionalServiceIds",
                "The room does not support one or more selected optional services.");
        }

        var selectedServiceSnapshots = command.OptionalServiceIds
            .Select(id => supportedServices[id])
            .Select(service => new ServicePriceSnapshot(service.Id, service.Name, service.Price))
            .ToList();

        var roomPrice = pricingCalculator.Calculate(room.BaseHourlyRate, period);
        var booking = new Booking(
            Guid.NewGuid(),
            room.Id,
            period.StartUtc,
            period.EndUtc,
            command.CreatedBy,
            room.BaseHourlyRate,
            roomPrice.Total,
            selectedServiceSnapshots,
            DateTime.UtcNow);

        await bookings.AddAsync(booking, cancellationToken);
        await bookings.SaveChangesAsync(cancellationToken);

        return new BookingResponse(
            booking.Id,
            booking.RoomId,
            booking.StartUtc,
            booking.EndUtc,
            roomPrice.Lines,
            selectedServiceSnapshots
                .Select(service => new SelectedServiceResponse(
                    service.OptionalServiceId,
                    service.Name,
                    service.Price))
                .ToList(),
            booking.RoomCharge,
            booking.ServiceCharge,
            booking.TotalCharge);
    }

    private static void ValidateCommand(CreateBookingCommand command)
    {
        var errors = new Dictionary<string, string[]>();

        if (command.RoomId == Guid.Empty)
        {
            errors["roomId"] = ["Room ID is required."];
        }

        if (string.IsNullOrWhiteSpace(command.CreatedBy))
        {
            errors["createdBy"] = ["The authenticated API-key identity is required."];
        }

        if (command.OptionalServiceIds.Count != command.OptionalServiceIds.Distinct().Count())
        {
            errors["optionalServiceIds"] = ["Optional service IDs cannot contain duplicates."];
        }

        if (errors.Count > 0)
        {
            throw new RequestValidationException(errors);
        }
    }
}
