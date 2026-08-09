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

    public DbSet<PersonalTrainingRequest> PersonalTrainingRequests =>
        Set<PersonalTrainingRequest>();

    public DbSet<TrainerConversation> TrainerConversations =>
        Set<TrainerConversation>();

    public DbSet<TrainerMessage> TrainerMessages =>
        Set<TrainerMessage>();

    public DbSet<KitchenMenuItem> KitchenMenuItems => Set<KitchenMenuItem>();

    public DbSet<KitchenSubscription> KitchenSubscriptions => Set<KitchenSubscription>();

    public DbSet<KitchenSubscriptionPackage> KitchenSubscriptionPackages =>
        Set<KitchenSubscriptionPackage>();

    public DbSet<KitchenMealPlan> KitchenMealPlans => Set<KitchenMealPlan>();

    public DbSet<KitchenMealPlanDay> KitchenMealPlanDays => Set<KitchenMealPlanDay>();

    public DbSet<KitchenMealPlanItem> KitchenMealPlanItems => Set<KitchenMealPlanItem>();

    public DbSet<KitchenIngredient> KitchenIngredients => Set<KitchenIngredient>();

    public DbSet<KitchenRecipeIngredient> KitchenRecipeIngredients =>
        Set<KitchenRecipeIngredient>();

    public DbSet<KitchenProductionPlan> KitchenProductionPlans =>
        Set<KitchenProductionPlan>();

    public DbSet<KitchenProductionPlanItem> KitchenProductionPlanItems =>
        Set<KitchenProductionPlanItem>();

    public DbSet<KitchenProductionPlanMaterial> KitchenProductionPlanMaterials =>
        Set<KitchenProductionPlanMaterial>();

    public DbSet<KitchenStockMovement> KitchenStockMovements => Set<KitchenStockMovement>();

    public DbSet<ShopProduct> ShopProducts => Set<ShopProduct>();

    public DbSet<ShoppingCart> ShoppingCarts => Set<ShoppingCart>();

    public DbSet<CartItem> CartItems => Set<CartItem>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

    public DbSet<CommunityEvent> CommunityEvents => Set<CommunityEvent>();

    public DbSet<CommunityChallenge> CommunityChallenges => Set<CommunityChallenge>();

    public DbSet<CommunityChallengeParticipation> CommunityChallengeParticipations =>
        Set<CommunityChallengeParticipation>();

    public DbSet<ChallengeProgressEntry> ChallengeProgressEntries => Set<ChallengeProgressEntry>();

    public DbSet<MemberProgressEntry> MemberProgressEntries => Set<MemberProgressEntry>();

    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();

    public DbSet<SuccessStory> SuccessStories => Set<SuccessStory>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
