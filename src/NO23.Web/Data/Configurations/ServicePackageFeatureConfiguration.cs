using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class ServicePackageFeatureConfiguration : IEntityTypeConfiguration<ServicePackageFeature>
{
    public void Configure(EntityTypeBuilder<ServicePackageFeature> builder)
    {
        builder.Property(x => x.Text).HasMaxLength(240).IsRequired();
        builder.HasOne(x => x.ServicePackage).WithMany(x => x.Features)
            .HasForeignKey(x => x.ServicePackageId).OnDelete(DeleteBehavior.Cascade);
    }
}
