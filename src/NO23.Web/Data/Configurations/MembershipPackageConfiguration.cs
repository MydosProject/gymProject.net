using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class MembershipPackageConfiguration : IEntityTypeConfiguration<MembershipPackage>
{
    public void Configure(EntityTypeBuilder<MembershipPackage> builder)
    {
        builder.Property(package => package.Code)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(package => package.Code)
            .IsUnique();

        builder.Property(package => package.Name)
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(package => package.Audience)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(package => package.Description)
            .HasMaxLength(600)
            .IsRequired();

        builder.Property(package => package.CreatedAtUtc)
            .HasDefaultValueSql("NOW()");
    }
}
