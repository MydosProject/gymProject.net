using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class UserNotificationConfiguration
    : IEntityTypeConfiguration<UserNotification>
{
    public void Configure(
        EntityTypeBuilder<UserNotification> builder)
    {
        builder.HasKey(notification =>
            notification.Id);

        builder.Property(notification =>
                notification.ApplicationUserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(notification =>
                notification.Type)
            .IsRequired();

        builder.Property(notification =>
                notification.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(notification =>
                notification.Message)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(notification =>
                notification.Url)
            .HasMaxLength(1000);

        builder.Property(notification =>
                notification.CreatedAtUtc)
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(notification =>
            new
            {
                notification.ApplicationUserId,
                notification.ReadAtUtc
            });

        builder.HasIndex(notification =>
            new
            {
                notification.ApplicationUserId,
                notification.CreatedAtUtc
            });

        builder.HasOne(notification =>
                notification.ApplicationUser)
            .WithMany(user =>
                user.Notifications)
            .HasForeignKey(notification =>
                notification.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}