using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class GroupClassConfiguration : IEntityTypeConfiguration<GroupClass>
{
    public void Configure(EntityTypeBuilder<GroupClass> builder)
    {
        builder.Property(groupClass => groupClass.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(groupClass => groupClass.Description)
            .HasMaxLength(800);

        builder.Property(groupClass => groupClass.DifficultyLevel)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(groupClass => groupClass.CreatedAtUtc)
            .HasDefaultValueSql("NOW()");

        builder.HasOne(groupClass => groupClass.Trainer)
            .WithMany(trainer => trainer.GroupClasses)
            .HasForeignKey(groupClass => groupClass.TrainerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
