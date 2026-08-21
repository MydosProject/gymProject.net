using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class PersonalTrainingSessionHistoryConfiguration : IEntityTypeConfiguration<PersonalTrainingSessionHistory>
{
    public void Configure(EntityTypeBuilder<PersonalTrainingSessionHistory> builder)
    {
        builder.Property(item => item.Note).HasMaxLength(600);
        builder.Property(item => item.ChangedByUserId).HasMaxLength(450).IsRequired();
        builder.Property(item => item.ChangedAtUtc).HasDefaultValueSql("NOW()");
        builder.HasOne(item => item.PersonalTrainingSession).WithMany(item => item.History)
            .HasForeignKey(item => item.PersonalTrainingSessionId).OnDelete(DeleteBehavior.Cascade);
    }
}
