using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class TrainerConfiguration : IEntityTypeConfiguration<Trainer>
{
    public void Configure(EntityTypeBuilder<Trainer> builder)
    {
        builder.Property(trainer => trainer.FirstName)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(trainer => trainer.LastName)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(trainer => trainer.Specialty)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(trainer => trainer.Certifications)
            .HasMaxLength(600);

        builder.Property(trainer => trainer.Bio)
            .HasMaxLength(1200);

        builder.Property(trainer => trainer.CreatedAtUtc)
            .HasDefaultValueSql("NOW()");
    }
}
