using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class PaymentTransactionConfiguration
    : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.HasKey(payment => payment.Id);

        builder.Property(payment => payment.Provider)
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(payment => payment.ConversationId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(payment => payment.BasketId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(payment => payment.Token)
            .HasMaxLength(512);

        builder.Property(payment => payment.PaymentPageUrl)
            .HasMaxLength(2048);

        builder.Property(payment => payment.PaymentId)
            .HasMaxLength(100);

        builder.Property(payment => payment.RawStatus)
            .HasMaxLength(40);

        builder.Property(payment => payment.PaymentStatus)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(payment => payment.Amount)
            .HasPrecision(18, 2);

        builder.Property(payment => payment.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(payment => payment.LastError)
            .HasMaxLength(2000);

        builder.Property(payment => payment.RawInitializeResponseJson)
            .HasColumnType("jsonb");

        builder.Property(payment => payment.RawRetrieveResponseJson)
            .HasColumnType("jsonb");

        builder.Property(payment => payment.RawWebhookJson)
            .HasColumnType("jsonb");

        builder.Property(payment => payment.CreatedAtUtc)
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(payment => payment.ConversationId)
            .IsUnique();

        builder.HasIndex(payment => payment.Token)
            .IsUnique();

        builder.HasIndex(payment => payment.BasketId);

        builder.HasIndex(payment => new
            {
                payment.Provider,
                payment.PaymentId
            })
            .IsUnique();

        builder.HasOne(payment => payment.Order)
            .WithMany(order => order.PaymentTransactions)
            .HasForeignKey(payment => payment.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(payment => new
        {
            payment.Provider,
            payment.PaymentStatus,
            payment.CheckoutExpiresAtUtc
        });
    }
}