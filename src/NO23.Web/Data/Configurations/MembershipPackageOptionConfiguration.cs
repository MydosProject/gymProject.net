using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class MembershipPackageOptionConfiguration : IEntityTypeConfiguration<MembershipPackageOption>
{
    public void Configure(EntityTypeBuilder<MembershipPackageOption> builder)
    {
        builder.Property(item => item.Name).HasMaxLength(100).IsRequired();
        builder.Property(item => item.Description).HasMaxLength(500).IsRequired();
        builder.Property(item => item.CreatedAtUtc).HasDefaultValueSql("NOW()");
        builder.HasIndex(item => new { item.MembershipPackageId, item.Name }).IsUnique();
        builder.HasOne(item => item.MembershipPackage).WithMany(item => item.Options)
            .HasForeignKey(item => item.MembershipPackageId).OnDelete(DeleteBehavior.Cascade);
    }
}
