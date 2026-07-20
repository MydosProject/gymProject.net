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

    public DbSet<KitchenMenuItem> KitchenMenuItems => Set<KitchenMenuItem>();

    public DbSet<KitchenSubscription> KitchenSubscriptions => Set<KitchenSubscription>();

    public DbSet<ShopProduct> ShopProducts => Set<ShopProduct>();

    public DbSet<ShoppingCart> ShoppingCarts => Set<ShoppingCart>();

    public DbSet<CartItem> CartItems => Set<CartItem>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
