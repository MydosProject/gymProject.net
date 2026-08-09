using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class TrainerConversationConfiguration
    : IEntityTypeConfiguration<TrainerConversation>
{
    public void Configure(
        EntityTypeBuilder<TrainerConversation> builder)
    {
        builder.Property(conversation => conversation.CreatedAtUtc)
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(conversation => new
        {
            conversation.MemberProfileId,
            conversation.TrainerId
        })
        .IsUnique();

        builder.HasIndex(conversation =>
            conversation.LastMessageAtUtc);

        builder.HasOne(conversation =>
                conversation.MemberProfile)
            .WithMany(profile =>
                profile.TrainerConversations)
            .HasForeignKey(conversation =>
                conversation.MemberProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(conversation =>
                conversation.Trainer)
            .WithMany(trainer =>
                trainer.TrainerConversations)
            .HasForeignKey(conversation =>
                conversation.TrainerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}