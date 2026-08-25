using ConferenceRooms.Business.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConferenceRooms.Data.Persistence;

internal sealed class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("Rooms");
        builder.HasKey(room => room.Id);
        builder.Property(room => room.Name).HasMaxLength(100).IsRequired();
        builder.Property(room => room.BaseHourlyRate).HasPrecision(18, 2);
        builder.HasIndex(room => room.Name);
        builder.HasQueryFilter(room => !room.IsDeleted);

        builder.HasMany(room => room.SupportedServices)
            .WithOne(link => link.Room)
            .HasForeignKey(link => link.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class OptionalServiceConfiguration : IEntityTypeConfiguration<OptionalService>
{
    public void Configure(EntityTypeBuilder<OptionalService> builder)
    {
        builder.ToTable("OptionalServices");
        builder.HasKey(service => service.Id);
        builder.Property(service => service.Name).HasMaxLength(100).IsRequired();
        builder.Property(service => service.Price).HasPrecision(18, 2);
        builder.HasIndex(service => service.Name).IsUnique();
    }
}

internal sealed class RoomOptionalServiceConfiguration : IEntityTypeConfiguration<RoomOptionalService>
{
    public void Configure(EntityTypeBuilder<RoomOptionalService> builder)
    {
        builder.ToTable("RoomOptionalServices");
        builder.HasKey(link => new { link.RoomId, link.OptionalServiceId });
        builder.HasQueryFilter(link => !link.Room.IsDeleted);

        builder.HasOne(link => link.OptionalService)
            .WithMany()
            .HasForeignKey(link => link.OptionalServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");
        builder.HasKey(booking => booking.Id);
        builder.Property(booking => booking.CreatedBy).HasMaxLength(100).IsRequired();
        builder.Property(booking => booking.RoomRateSnapshot).HasPrecision(18, 2);
        builder.Property(booking => booking.RoomCharge).HasPrecision(18, 2);
        builder.Property(booking => booking.ServiceCharge).HasPrecision(18, 2);
        builder.Property(booking => booking.TotalCharge).HasPrecision(18, 2);
        builder.HasIndex(booking => new { booking.RoomId, booking.StartUtc, booking.EndUtc });

        builder.HasOne<Room>()
            .WithMany()
            .HasForeignKey(booking => booking.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(booking => booking.SelectedServices)
            .WithOne(service => service.Booking)
            .HasForeignKey(service => service.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class BookingOptionalServiceConfiguration : IEntityTypeConfiguration<BookingOptionalService>
{
    public void Configure(EntityTypeBuilder<BookingOptionalService> builder)
    {
        builder.ToTable("BookingOptionalServices");
        builder.HasKey(service => new { service.BookingId, service.OptionalServiceId });
        builder.Property(service => service.NameSnapshot).HasMaxLength(100).IsRequired();
        builder.Property(service => service.PriceSnapshot).HasPrecision(18, 2);

        builder.HasOne<OptionalService>()
            .WithMany()
            .HasForeignKey(service => service.OptionalServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
