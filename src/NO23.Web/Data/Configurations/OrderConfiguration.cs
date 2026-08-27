using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Property(order => order.OrderNumber)
            .HasMaxLength(40)
            .IsRequired();

        builder.HasIndex(order => order.OrderNumber)
            .IsUnique();

        builder.Property(order => order.Type)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(order => order.Status)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(order => order.PaymentStatus)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(order => order.DeliveryMethod)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(order => order.GuestEmail)
            .HasMaxLength(256);

        builder.Property(order => order.DeliveryFullName)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(order => order.DeliveryPhoneNumber)
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(order => order.DeliveryAddressLine)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(order => order.DeliveryDistrict)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(order => order.DeliveryCity)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(order => order.DeliveryPostalCode)
            .HasMaxLength(20);

        builder.Property(order => order.DeliveryTimeSlot)
            .HasMaxLength(40);

        builder.Property(order => order.Notes)
            .HasMaxLength(500);

        builder.Property(order => order.Subtotal)
            .HasPrecision(10, 2);

        builder.Property(order => order.DeliveryFee)
            .HasPrecision(10, 2);

        builder.Property(order => order.Total)
            .HasPrecision(10, 2);

        builder.Property(order => order.CreatedAtUtc)
            .HasDefaultValueSql("NOW()");

        builder.HasOne(order => order.MemberProfile)
            .WithMany(member => member.Orders)
            .HasForeignKey(order => order.MemberProfileId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(order => order.KitchenSubscription)
            .WithMany(subscription => subscription.Orders)
            .HasForeignKey(order => order.KitchenSubscriptionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
