using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(user => user.FirstName)
            .HasMaxLength(80);

        builder.Property(user => user.LastName)
            .HasMaxLength(80);

        builder.Property(user => user.CreatedAtUtc)
            .HasDefaultValueSql("NOW()");
    }
}
