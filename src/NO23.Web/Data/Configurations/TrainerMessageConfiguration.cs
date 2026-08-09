using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class TrainerMessageConfiguration
    : IEntityTypeConfiguration<TrainerMessage>
{
    public void Configure(
        EntityTypeBuilder<TrainerMessage> builder)
    {
        builder.Property(message => message.SenderApplicationUserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(message => message.Body)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(message => message.SentAtUtc)
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(message => new
        {
            message.TrainerConversationId,
            message.SentAtUtc
        });

        builder.HasIndex(message => new
        {
            message.TrainerConversationId,
            message.ReadAtUtc
        });

        builder.HasOne(message =>
                message.TrainerConversation)
            .WithMany(conversation =>
                conversation.Messages)
            .HasForeignKey(message =>
                message.TrainerConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(message =>
                message.SenderApplicationUser)
            .WithMany(user =>
                user.SentTrainerMessages)
            .HasForeignKey(message =>
                message.SenderApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}