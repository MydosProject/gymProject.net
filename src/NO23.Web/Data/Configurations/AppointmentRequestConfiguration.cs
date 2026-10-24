using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class AppointmentRequestConfiguration : IEntityTypeConfiguration<AppointmentRequest>
{
    public void Configure(EntityTypeBuilder<AppointmentRequest> builder)
    {
        builder.HasOne(x => x.ScheduledTrainer).WithMany().HasForeignKey(x => x.ScheduledTrainerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.ScheduledTrainerId, x.ScheduledAtUtc });
        builder.HasIndex(x => new { x.Status, x.CreatedAtUtc });
    }
}
