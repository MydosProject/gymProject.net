using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.ViewModels.Api;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ClassReservationService>();
builder.Services.AddScoped<CalorieCalculatorService>();

var app = builder.Build();

await DatabaseSeeder.SeedAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "NO23 Sports Club API v1");
    });
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    var api = app.MapGroup("/api")
        .WithTags("Development API");

    api.MapGet("/health", () => Results.Ok(new HealthResponse
    {
        Status = "Healthy",
        Application = "NO23 Sports Club"
    }))
    .WithName("GetHealth")
    .Produces<HealthResponse>();

    api.MapGet("/membership-packages", async (ApplicationDbContext dbContext) =>
    {
        return await dbContext.MembershipPackages
            .AsNoTracking()
            .Where(package => package.IsActive)
            .OrderBy(package => package.DisplayOrder)
            .Select(package => new MembershipPackageResponse
            {
                Id = package.Id,
                Code = package.Code.ToString(),
                Name = package.Name,
                Audience = package.Audience,
                Description = package.Description,
                WeeklyClassLimit = package.WeeklyClassLimit,
                DisplayOrder = package.DisplayOrder
            })
            .ToListAsync();
    })
    .WithName("GetMembershipPackages")
    .Produces<IReadOnlyList<MembershipPackageResponse>>();

    api.MapGet("/class-sessions", async (ApplicationDbContext dbContext) =>
    {
        return await dbContext.ClassSessions
            .AsNoTracking()
            .Where(session =>
                session.Status == ClassSessionStatus.Scheduled &&
                session.StartsAtUtc >= DateTime.UtcNow)
            .OrderBy(session => session.StartsAtUtc)
            .Select(session => new ClassSessionResponse
            {
                Id = session.Id,
                ClassName = session.GroupClass.Name,
                TrainerName = session.GroupClass.Trainer.FirstName + " " + session.GroupClass.Trainer.LastName,
                StartsAtUtc = session.StartsAtUtc,
                Capacity = session.CapacityOverride ?? session.GroupClass.Capacity,
                ReservedCount = session.Reservations.Count(reservation =>
                    reservation.Status == ClassReservationStatus.Reserved),
                DurationMinutes = session.GroupClass.DurationMinutes,
                DifficultyLevel = session.GroupClass.DifficultyLevel.ToString(),
                AverageCaloriesBurned = session.GroupClass.AverageCaloriesBurned
            })
            .ToListAsync();
    })
    .WithName("GetClassSessions")
    .Produces<IReadOnlyList<ClassSessionResponse>>();

    api.MapGet("/kitchen-menu-items", async (ApplicationDbContext dbContext) =>
    {
        return await dbContext.KitchenMenuItems
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Name)
            .Select(item => new KitchenMenuItemResponse
            {
                Id = item.Id,
                Name = item.Name,
                Category = item.Category.ToString(),
                Calories = item.Calories,
                ProteinGrams = item.ProteinGrams,
                CarbohydrateGrams = item.CarbohydrateGrams,
                FatGrams = item.FatGrams,
                Ingredients = item.Ingredients,
                Allergens = item.Allergens,
                Tags = item.Tags
            })
            .ToListAsync();
    })
    .WithName("GetKitchenMenuItems")
    .Produces<IReadOnlyList<KitchenMenuItemResponse>>();

    api.MapPost("/calorie/calculate", (
        CalorieCalculationApiRequest request,
        CalorieCalculatorService calculator) =>
    {
        var result = calculator.Calculate(new CalorieCalculationRequest
        {
            HeightCm = request.HeightCm,
            WeightKg = request.WeightKg,
            Age = request.Age,
            Gender = request.Gender,
            ActivityLevel = request.ActivityLevel,
            Goal = request.Goal
        });

        return Results.Ok(new CalorieCalculationResponse
        {
            DailyCalories = result.DailyCalories,
            ProteinGrams = result.ProteinGrams,
            CarbohydrateGrams = result.CarbohydrateGrams,
            FatGrams = result.FatGrams
        });
    })
    .WithName("CalculateCalories")
    .Produces<CalorieCalculationResponse>();
}

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
