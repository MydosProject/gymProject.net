using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class ServicePackageConfiguration : IEntityTypeConfiguration<ServicePackage>
{
    public void Configure(EntityTypeBuilder<ServicePackage> builder)
    {
        builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Slug).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Subtitle).HasMaxLength(180).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(700).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasDefaultValueSql("NOW()");
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasOne(x => x.MembershipPackage).WithMany()
            .HasForeignKey(x => x.MembershipPackageId).OnDelete(DeleteBehavior.SetNull);
    }
}
