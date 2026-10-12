using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class OrderPaymentInstallmentConfiguration : IEntityTypeConfiguration<OrderPaymentInstallment>
{
    public void Configure(EntityTypeBuilder<OrderPaymentInstallment> builder)
    {
        builder.Property(item => item.Amount).HasPrecision(10, 2);
        builder.Property(item => item.PaymentReference).HasMaxLength(160);
        builder.Property(item => item.RecordedByUserId).HasMaxLength(450);
        builder.HasIndex(item => new { item.OrderId, item.Number }).IsUnique();
        builder.HasOne(item => item.Order).WithMany(order => order.PaymentInstallments)
            .HasForeignKey(item => item.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}
