using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<MembershipPackage> MembershipPackages => Set<MembershipPackage>();

    public DbSet<MemberProfile> MemberProfiles => Set<MemberProfile>();

    public DbSet<Trainer> Trainers => Set<Trainer>();

    public DbSet<GroupClass> GroupClasses => Set<GroupClass>();

    public DbSet<ClassSession> ClassSessions => Set<ClassSession>();

    public DbSet<ClassReservation> ClassReservations => Set<ClassReservation>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
