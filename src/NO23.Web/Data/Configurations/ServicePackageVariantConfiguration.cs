using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class ServicePackageVariantConfiguration : IEntityTypeConfiguration<ServicePackageVariant>
{
    public void Configure(EntityTypeBuilder<ServicePackageVariant> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.BillingType).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.MonthlyPrice).HasPrecision(12, 2);
        builder.Property(x => x.TotalPrice).HasPrecision(12, 2);
        builder.Property(x => x.CreatedAtUtc).HasDefaultValueSql("NOW()");
        builder.HasIndex(x => new { x.ServicePackageId, x.Name }).IsUnique();
        builder.HasOne(x => x.ServicePackage).WithMany(x => x.Variants)
            .HasForeignKey(x => x.ServicePackageId).OnDelete(DeleteBehavior.Cascade);
    }
}
