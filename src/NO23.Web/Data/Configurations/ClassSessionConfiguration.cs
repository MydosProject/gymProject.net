using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class ClassSessionConfiguration : IEntityTypeConfiguration<ClassSession>
{
    public void Configure(EntityTypeBuilder<ClassSession> builder)
    {
        builder.Property(session => session.Status)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(session => session.CreatedAtUtc)
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(session => session.StartsAtUtc);

        builder.HasOne(session => session.GroupClass)
            .WithMany(groupClass => groupClass.Sessions)
            .HasForeignKey(session => session.GroupClassId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
