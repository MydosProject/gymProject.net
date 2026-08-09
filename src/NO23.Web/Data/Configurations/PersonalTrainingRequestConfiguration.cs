using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class PersonalTrainingRequestConfiguration
    : IEntityTypeConfiguration<PersonalTrainingRequest>
{
    public void Configure(EntityTypeBuilder<PersonalTrainingRequest> builder)
    {
        builder.Property(request => request.PreferredTimeWindow)
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(request => request.GoalNote)
            .HasMaxLength(1200);

        builder.Property(request => request.AdminNote)
            .HasMaxLength(1200);

        builder.Property(request => request.TrainerNote)
            .HasMaxLength(1200);

        builder.Property(request => request.Status)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(request => request.CreatedAtUtc)
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(request => request.Status);

        builder.HasIndex(request => request.CreatedAtUtc);

        builder.HasIndex(request => new
        {
            request.MemberProfileId,
            request.TrainerId,
            request.PreferredDate,
            request.Status
        });

        builder.HasOne(request => request.MemberProfile)
            .WithMany(profile => profile.PersonalTrainingRequests)
            .HasForeignKey(request => request.MemberProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(request => request.Trainer)
            .WithMany(trainer => trainer.PersonalTrainingRequests)
            .HasForeignKey(request => request.TrainerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
