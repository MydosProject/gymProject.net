using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public sealed class MemberLessonAccountConfiguration : IEntityTypeConfiguration<MemberLessonAccount>
{
    public void Configure(EntityTypeBuilder<MemberLessonAccount> b)
    {
        b.HasIndex(x => new { x.MemberProfileId, x.PeriodId, x.Category }).IsUnique();
        b.Property(x => x.Remaining).IsConcurrencyToken();
        b.Property(x => x.ConvertedCredits).IsConcurrencyToken();
        b.HasOne(x => x.MemberProfile).WithMany(x => x.LessonAccounts).HasForeignKey(x => x.MemberProfileId).OnDelete(DeleteBehavior.Cascade);
    }
}
public sealed class LessonCreditMovementConfiguration : IEntityTypeConfiguration<LessonCreditMovement>
{
    public void Configure(EntityTypeBuilder<LessonCreditMovement> b)
    {
        b.Property(x => x.Actor).HasMaxLength(450).IsRequired();
        b.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        b.HasIndex(x => new { x.AccountId, x.OperationId }).IsUnique();
        b.HasOne(x => x.Account).WithMany(x => x.Movements).HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
    }
}
public sealed class LessonConversionRuleConfiguration : IEntityTypeConfiguration<LessonConversionRule>
{
    public void Configure(EntityTypeBuilder<LessonConversionRule> b)
    {
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.CreatedBy).HasMaxLength(450).IsRequired();
    }
}
