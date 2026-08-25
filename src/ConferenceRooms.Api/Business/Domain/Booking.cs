namespace ConferenceRooms.Business.Domain;

public sealed class Booking
{
    private Booking()
    {
    }

    public Booking(
        Guid id,
        Guid roomId,
        DateTime startUtc,
        DateTime endUtc,
        string createdBy,
        decimal roomRateSnapshot,
        decimal roomCharge,
        IEnumerable<ServicePriceSnapshot> selectedServices,
        DateTime createdAtUtc)
    {
        Id = id;
        RoomId = roomId;
        StartUtc = startUtc;
        EndUtc = endUtc;
        CreatedBy = createdBy;
        RoomRateSnapshot = roomRateSnapshot;
        RoomCharge = roomCharge;
        CreatedAtUtc = createdAtUtc;

        foreach (var service in selectedServices)
        {
            SelectedServices.Add(
                new BookingOptionalService(
                    id,
                    service.OptionalServiceId,
                    service.Name,
                    service.Price));
        }

        ServiceCharge = SelectedServices.Sum(service => service.PriceSnapshot);
        TotalCharge = RoomCharge + ServiceCharge;
    }

    public Guid Id { get; private set; }

    public Guid RoomId { get; private set; }

    public DateTime StartUtc { get; private set; }

    public DateTime EndUtc { get; private set; }

    public string CreatedBy { get; private set; } = string.Empty;

    public decimal RoomRateSnapshot { get; private set; }

    public decimal RoomCharge { get; private set; }

    public decimal ServiceCharge { get; private set; }

    public decimal TotalCharge { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public ICollection<BookingOptionalService> SelectedServices { get; private set; } = [];
}

public sealed class BookingOptionalService
{
    private BookingOptionalService()
    {
    }

    public BookingOptionalService(
        Guid bookingId,
        Guid optionalServiceId,
        string nameSnapshot,
        decimal priceSnapshot)
    {
        BookingId = bookingId;
        OptionalServiceId = optionalServiceId;
        NameSnapshot = nameSnapshot;
        PriceSnapshot = priceSnapshot;
    }

    public Guid BookingId { get; private set; }

    public Booking Booking { get; private set; } = null!;

    public Guid OptionalServiceId { get; private set; }

    public string NameSnapshot { get; private set; } = string.Empty;

    public decimal PriceSnapshot { get; private set; }
}

public sealed record ServicePriceSnapshot(Guid OptionalServiceId, string Name, decimal Price);
