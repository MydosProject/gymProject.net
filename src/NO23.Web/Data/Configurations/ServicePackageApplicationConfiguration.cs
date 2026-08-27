using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class ServicePackageApplicationConfiguration :
    IEntityTypeConfiguration<ServicePackageApplication>
{
    public void Configure(
        EntityTypeBuilder<ServicePackageApplication> builder)
    {
        builder.Property(application => application.FullName)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(application => application.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(application => application.PhoneNumber)
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(application => application.Notes)
            .HasMaxLength(1000);

        builder.Property(application => application.Status)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(application => application.CreatedAtUtc)
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(application => application.Status);
        builder.HasIndex(application => application.CreatedAtUtc);

        builder.HasOne(application => application.ServicePackage)
            .WithMany()
            .HasForeignKey(application => application.ServicePackageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(application => application.ServicePackageVariant)
            .WithMany()
            .HasForeignKey(application => application.ServicePackageVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(application => application.ApplicationUser)
            .WithMany()
            .HasForeignKey(application => application.ApplicationUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
