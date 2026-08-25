using ConferenceRooms.Business.Domain;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRooms.Data.Persistence;

public sealed class ConferenceRoomsDbContext(DbContextOptions<ConferenceRoomsDbContext> options)
    : DbContext(options)
{
    public DbSet<Room> Rooms => Set<Room>();

    public DbSet<OptionalService> OptionalServices => Set<OptionalService>();

    public DbSet<RoomOptionalService> RoomOptionalServices => Set<RoomOptionalService>();

    public DbSet<Booking> Bookings => Set<Booking>();

    public DbSet<BookingOptionalService> BookingOptionalServices => Set<BookingOptionalService>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ConferenceRoomsDbContext).Assembly);
    }
}
