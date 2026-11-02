using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class PersonalTrainingSessionConfiguration : IEntityTypeConfiguration<PersonalTrainingSession>
{
    public void Configure(EntityTypeBuilder<PersonalTrainingSession> builder)
    {
        builder.HasOne(x => x.LessonAccount).WithMany().HasForeignKey(x => x.LessonAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(x => x.LessonAccount).AutoInclude();
        builder.Property(item => item.Note).HasMaxLength(600);
        builder.Property(item => item.CreatedAtUtc).HasDefaultValueSql("NOW()");
        builder.HasIndex(item => new { item.TrainerId, item.StartsAtUtc });
        builder.HasIndex(item => item.RecurrenceId);
        builder.HasOne(item => item.Trainer).WithMany(item => item.PersonalTrainingSessions)
            .HasForeignKey(item => item.TrainerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.MemberProfile).WithMany(item => item.PersonalTrainingSessions)
            .HasForeignKey(item => item.MemberProfileId).OnDelete(DeleteBehavior.Restrict);
    }
}
