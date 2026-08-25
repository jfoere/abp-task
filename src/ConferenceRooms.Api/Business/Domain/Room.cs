namespace ConferenceRooms.Business.Domain;

public sealed class Room
{
    private Room()
    {
    }

    public Room(
        Guid id,
        string name,
        int capacity,
        decimal baseHourlyRate,
        IEnumerable<OptionalService> supportedServices,
        DateTime createdAtUtc)
    {
        Id = id;
        Name = name;
        Capacity = capacity;
        BaseHourlyRate = baseHourlyRate;
        CreatedAtUtc = createdAtUtc;
        ReplaceSupportedServices(supportedServices);
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public int Capacity { get; private set; }

    public decimal BaseHourlyRate { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? DeletedAtUtc { get; private set; }

    public ICollection<RoomOptionalService> SupportedServices { get; private set; } = [];

    public void Update(
        string name,
        int capacity,
        decimal baseHourlyRate,
        IEnumerable<OptionalService> supportedServices)
    {
        Name = name;
        Capacity = capacity;
        BaseHourlyRate = baseHourlyRate;
        ReplaceSupportedServices(supportedServices);
    }

    public void SoftDelete(DateTime deletedAtUtc)
    {
        IsDeleted = true;
        DeletedAtUtc = deletedAtUtc;
    }

    private void ReplaceSupportedServices(IEnumerable<OptionalService> services)
    {
        var desiredServices = services
            .DistinctBy(service => service.Id)
            .ToDictionary(service => service.Id);

        foreach (var existingLink in SupportedServices
                     .Where(link => !desiredServices.ContainsKey(link.OptionalServiceId))
                     .ToList())
        {
            SupportedServices.Remove(existingLink);
        }

        foreach (var service in desiredServices.Values
                     .Where(service => SupportedServices.All(
                         link => link.OptionalServiceId != service.Id)))
        {
            SupportedServices.Add(new RoomOptionalService(Id, service.Id, service));
        }
    }
}

public sealed class OptionalService
{
    private OptionalService()
    {
    }

    public OptionalService(Guid id, string name, decimal price)
    {
        Id = id;
        Name = name;
        Price = price;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public decimal Price { get; private set; }
}

public sealed class RoomOptionalService
{
    private RoomOptionalService()
    {
    }

    public RoomOptionalService(Guid roomId, Guid optionalServiceId, OptionalService optionalService)
    {
        RoomId = roomId;
        OptionalServiceId = optionalServiceId;
        OptionalService = optionalService;
    }

    public Guid RoomId { get; private set; }

    public Room Room { get; private set; } = null!;

    public Guid OptionalServiceId { get; private set; }

    public OptionalService OptionalService { get; private set; } = null!;
}
