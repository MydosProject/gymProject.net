using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Infrastructure.Validation;
using NO23.Web.Services;
using NO23.Web.Services.Email;
using NO23.Web.ViewModels.Api;
using NO23.Web.Services.Payments;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.Configure<SmtpEmailOptions>(
    builder.Configuration.GetSection("Email:Smtp"));
builder.Services.Configure<PasswordResetOptions>(
    builder.Configuration.GetSection("Email:PasswordReset"));

builder.Services
    .AddOptions<IyzicoOptions>()
    .Bind(builder.Configuration.GetSection(IyzicoOptions.SectionName))
    .Validate(
        options =>
            !options.Enabled ||
            (Uri.TryCreate(
                options.BaseUrl,
                UriKind.Absolute,
                out var baseUri) &&
             baseUri.Scheme == Uri.UriSchemeHttps),
        "Iyzico BaseUrl geçerli bir HTTPS adresi olmalıdır.")
    .Validate(
        options =>
            !options.Enabled ||
            !string.IsNullOrWhiteSpace(options.ApiKey),
        "Iyzico ApiKey tanımlanmalıdır.")
    .Validate(
        options =>
            !options.Enabled ||
            !string.IsNullOrWhiteSpace(options.SecretKey),
        "Iyzico SecretKey tanımlanmalıdır.")
    .Validate(
        options =>
            !options.Enabled ||
            (Uri.TryCreate(
                options.CallbackUrl,
                UriKind.Absolute,
                out var callbackUri) &&
             callbackUri.Scheme == Uri.UriSchemeHttps),
        "Iyzico CallbackUrl dışarıdan erişilebilir bir HTTPS adresi olmalıdır.")
    .Validate(
        options =>
            !options.Enabled ||
            string.Equals(
                options.Currency,
                "TRY",
                StringComparison.OrdinalIgnoreCase),
        "İlk sürümde yalnızca TRY desteklenmektedir.")
    .Validate(
        options =>
            !options.Enabled ||
            options.EnabledInstallments is [1],
        "İlk sürümde yalnızca tek çekim desteklenmektedir.")
    .ValidateOnStart();

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    var tokenLifespanMinutes =
        builder.Configuration.GetValue<int?>("Email:PasswordReset:TokenLifespanMinutes")
        ?? 60;

    options.TokenLifespan = TimeSpan.FromMinutes(tokenLifespanMinutes);
});

builder.Services.AddTransient<IEmailSender>(serviceProvider =>
{
    var smtpOptions =
        serviceProvider.GetRequiredService<
            Microsoft.Extensions.Options.IOptions<SmtpEmailOptions>>().Value;

    var hasSmtpSettings =
        smtpOptions.Enabled &&
        !string.IsNullOrWhiteSpace(smtpOptions.Host) &&
        smtpOptions.Port > 0 &&
        !string.IsNullOrWhiteSpace(smtpOptions.UserName) &&
        !string.IsNullOrWhiteSpace(smtpOptions.Password) &&
        !string.IsNullOrWhiteSpace(smtpOptions.FromAddress);

    if (hasSmtpSettings)
    {
        return ActivatorUtilities
            .CreateInstance<SmtpIdentityEmailSender>(serviceProvider);
    }

    var environment =
        serviceProvider.GetRequiredService<IWebHostEnvironment>();

    return environment.IsDevelopment()
        ? ActivatorUtilities
            .CreateInstance<DevelopmentEmailSender>(serviceProvider)
        : ActivatorUtilities
            .CreateInstance<DisabledEmailSender>(serviceProvider);
});

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddErrorDescriber<TurkishIdentityErrorDescriber>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllersWithViews(options =>
{
    options.ModelMetadataDetailsProviders.Add(new TurkishValidationMetadataProvider());

    var messages = options.ModelBindingMessageProvider;
    messages.SetAttemptedValueIsInvalidAccessor((value, field) =>
        $"{field} için girilen değer geçerli değil.");
    messages.SetMissingBindRequiredValueAccessor(field =>
        $"{field} alanı zorunludur.");
    messages.SetMissingKeyOrValueAccessor(() =>
        "Eksik bilgi gönderildi.");
    messages.SetNonPropertyAttemptedValueIsInvalidAccessor(value =>
        $"Girilen değer geçerli değil: {value}.");
    messages.SetNonPropertyUnknownValueIsInvalidAccessor(() =>
        "Girilen değer geçerli değil.");
    messages.SetNonPropertyValueMustBeANumberAccessor(() =>
        "Sayısal bir değer girilmelidir.");
    messages.SetUnknownValueIsInvalidAccessor(field =>
        $"{field} alanı geçerli değil.");
    messages.SetValueIsInvalidAccessor(value =>
        $"{value} geçerli değil.");
    messages.SetValueMustBeANumberAccessor(field =>
        $"{field} sayısal bir değer olmalıdır.");
    messages.SetValueMustNotBeNullAccessor(_ =>
        "Bu alan zorunludur.");
});
builder.Services.AddScoped<ClassReservationService>();
builder.Services.AddScoped<PersonalTrainingRequestService>();
builder.Services.AddScoped<CalorieCalculatorService>();
builder.Services.AddScoped<KitchenPlanMatchingService>();
builder.Services.AddScoped<KitchenProductionPlanningService>();
builder.Services.AddScoped<CommunityChallengeProgressService>();
builder.Services.AddScoped<MemberProgressTrackingService>();
builder.Services.AddScoped<CommerceService>();
builder.Services.AddScoped<OrderWorkflowService>();
builder.Services.AddScoped<MemberCartQueryService>();
builder.Services.AddScoped<IIyzicoCheckoutClient, IyzicoCheckoutClient>();
builder.Services.AddScoped<IyzicoPaymentService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
}

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
app.UseSession();

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
                session.StartsAtUtc >= DateTime.UtcNow &&
                session.GroupClass.IsActive)
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
                UnitPrice = item.UnitPrice,
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

    api.MapGet("/shop-products", async (ApplicationDbContext dbContext) =>
    {
        return await dbContext.ShopProducts
            .AsNoTracking()
            .Where(product => product.IsActive)
            .OrderBy(product => product.DisplayOrder)
            .ThenBy(product => product.Name)
            .Select(product => new ShopProductResponse
            {
                Id = product.Id,
                Name = product.Name,
                Sku = product.Sku,
                Category = product.Category,
                UnitPrice = product.UnitPrice,
                StockQuantity = product.StockQuantity,
                Description = product.Description,
                ImageUrl = product.ImageUrl,
                Tags = product.Tags
            })
            .ToListAsync();
    })
    .WithName("GetShopProducts")
    .Produces<IReadOnlyList<ShopProductResponse>>();

    api.MapGet("/community-events", async (ApplicationDbContext dbContext) =>
    {
        var nowUtc = DateTime.UtcNow;
        var eventRows = await dbContext.CommunityEvents
            .AsNoTracking()
            .Where(item => item.Status != CommunityEventStatus.Cancelled)
            .OrderBy(item => item.StartsAtUtc)
            .Select(item => new
            {
                item.Id,
                item.Title,
                item.Slug,
                item.Summary,
                Type = item.Type.ToString(),
                item.Status,
                item.StartsAtUtc,
                item.EndsAtUtc,
                item.Location,
                item.Capacity,
                item.IsMembersOnly,
                item.ImageUrl
            })
            .ToListAsync();

        return eventRows
            .Select(item => new
            {
                Event = item,
                EffectiveStatus = CommunityEventLifecycle.GetEffectiveStatus(
                    item.Status,
                    item.StartsAtUtc,
                    item.EndsAtUtc,
                    nowUtc)
            })
            .Where(item => CommunityEventLifecycle.IsPubliclyOpen(item.EffectiveStatus))
            .Select(item => new CommunityEventResponse
            {
                Id = item.Event.Id,
                Title = item.Event.Title,
                Slug = item.Event.Slug,
                Summary = item.Event.Summary,
                Type = item.Event.Type,
                Status = item.EffectiveStatus.ToString(),
                StartsAtUtc = item.Event.StartsAtUtc,
                EndsAtUtc = item.Event.EndsAtUtc,
                Location = item.Event.Location,
                Capacity = item.Event.Capacity,
                IsMembersOnly = item.Event.IsMembersOnly,
                ImageUrl = item.Event.ImageUrl
            })
            .ToList();
    })
    .WithName("GetCommunityEvents")
    .Produces<IReadOnlyList<CommunityEventResponse>>();

    api.MapGet("/community-challenges", async (ApplicationDbContext dbContext) =>
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var challengeRows = await dbContext.CommunityChallenges
            .AsNoTracking()
            .Where(item => item.Status != CommunityChallengeStatus.Cancelled)
            .OrderBy(item => item.StartsOn)
            .Select(item => new
            {
                item.Id,
                item.Title,
                item.Slug,
                item.Summary,
                item.Goal,
                item.Reward,
                item.TargetDailyCalories,
                item.CalorieTolerancePercent,
                item.RequiredCompletionPercent,
                item.StartsOn,
                item.EndsOn,
                item.Status,
                item.ImageUrl
            })
            .ToListAsync();

        return challengeRows
            .Select(item => new
            {
                Challenge = item,
                EffectiveStatus = CommunityChallengeLifecycle.GetEffectiveStatus(
                    item.Status,
                    item.StartsOn,
                    item.EndsOn,
                    today)
            })
            .Where(item => CommunityChallengeLifecycle.IsJoinOpen(item.EffectiveStatus))
            .Select(item => new CommunityChallengeResponse
            {
                Id = item.Challenge.Id,
                Title = item.Challenge.Title,
                Slug = item.Challenge.Slug,
                Summary = item.Challenge.Summary,
                Goal = item.Challenge.Goal,
                Reward = item.Challenge.Reward,
                TargetDailyCalories = item.Challenge.TargetDailyCalories,
                CalorieTolerancePercent = item.Challenge.CalorieTolerancePercent,
                RequiredCompletionPercent = item.Challenge.RequiredCompletionPercent,
                StartsOn = item.Challenge.StartsOn,
                EndsOn = item.Challenge.EndsOn,
                Status = item.EffectiveStatus.ToString(),
                ImageUrl = item.Challenge.ImageUrl
            })
            .ToList();
    })
    .WithName("GetCommunityChallenges")
    .Produces<IReadOnlyList<CommunityChallengeResponse>>();

    api.MapGet("/blog-posts", async (ApplicationDbContext dbContext) =>
    {
        return await dbContext.BlogPosts
            .AsNoTracking()
            .Where(item => item.Status == ContentStatus.Published)
            .OrderByDescending(item => item.PublishedAtUtc ?? item.CreatedAtUtc)
            .Select(item => new BlogPostResponse
            {
                Id = item.Id,
                Title = item.Title,
                Slug = item.Slug,
                Summary = item.Summary,
                Category = item.Category,
                Tags = item.Tags,
                CoverImageUrl = item.CoverImageUrl,
                PublishedAtUtc = item.PublishedAtUtc
            })
            .ToListAsync();
    })
    .WithName("GetBlogPosts")
    .Produces<IReadOnlyList<BlogPostResponse>>();

    api.MapGet("/success-stories", async (ApplicationDbContext dbContext) =>
    {
        return await dbContext.SuccessStories
            .AsNoTracking()
            .Where(item => item.Status == ContentStatus.Published)
            .OrderByDescending(item => item.PublishedAtUtc ?? item.CreatedAtUtc)
            .Select(item => new SuccessStoryResponse
            {
                Id = item.Id,
                MemberName = item.MemberName,
                Title = item.Title,
                Slug = item.Slug,
                Summary = item.Summary,
                AchievementMetric = item.AchievementMetric,
                BeforeImageUrl = item.BeforeImageUrl,
                AfterImageUrl = item.AfterImageUrl,
                VideoUrl = item.VideoUrl,
                PublishedAtUtc = item.PublishedAtUtc
            })
            .ToListAsync();
    })
    .WithName("GetSuccessStories")
    .Produces<IReadOnlyList<SuccessStoryResponse>>();

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
