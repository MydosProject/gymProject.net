using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.ViewModels.Admin;

namespace NO23.Tests;

public class CategoryCreditTests
{
    [Theory]
    [InlineData("pt-flex", LessonCategory.PersonalTraining, 8)]
    [InlineData("group-performance", LessonCategory.Performance, 8)]
    [InlineData("group-reformer", LessonCategory.Reformer, 8)]
    [InlineData("kids-club", LessonCategory.Kids, 8)]
    [InlineData("hybrid", LessonCategory.PersonalTraining, 4)]
    public async Task PublishedCatalog_CanBePurchasedIncludingPtWithDefaultDuration(string slug, LessonCategory expectedCategory, int expectedCredits)
    {
        await using var db = Db();
        var package = ServicePackageSeed.Defaults.Single(x => x.Slug == slug);
        var variant = package.Variants.First();
        var member = new MemberProfile { ApplicationUser = new() { UserName = "catalog-buyer" },
            MembershipPackage = new() { Code = MembershipPackageCode.Start }, MembershipActivationPending = true };
        db.AddRange(package, member); await db.SaveChangesAsync();
        var service = new MembershipRenewalService(db);
        var created = await service.CreateOrderAsync(member.ApplicationUserId, variant.Id, "QA", "05550000000", "Adres", "İlçe", "İstanbul");
        Assert.Null(created.Error); Assert.NotNull(created.OrderId);
        var order = await db.Orders.SingleAsync(); order.PaymentStatus = PaymentStatus.Paid; await db.SaveChangesAsync();
        Assert.True(await service.ActivatePaidOrderAsync(order.Id));
        Assert.Equal(expectedCredits, Account(member, expectedCategory).Remaining);
        Assert.Equal(MemberPackageEntitlement.CalculateInitialCredits(variant), member.RemainingClassCredits);
        Assert.Equal(MemberPackageEntitlement.CalculateEndDate(variant, member.MembershipStartsAtUtc!.Value), member.MembershipEndsAtUtc);
    }

    [Fact]
    public async Task UnpaidRegistration_ShowsPendingWithZeroAvailableAndNoFalseBalanceError()
    {
        await using var db = Db();
        var member = new MemberProfile { ApplicationUser = new() { UserName = "pending" },
            MembershipPackage = new() { Code = MembershipPackageCode.Start }, MembershipActivationPending = true,
            ServicePackageVariant = new() { PersonalTrainingSessionCount = 8, ServicePackage = new() { Name = "PT" } } };
        db.Add(member); await db.SaveChangesAsync();
        var summary = (await new LessonCreditSummaryService(db).GetAsync(member.Id))!;
        Assert.True(summary.IsActivationPending);
        Assert.False(summary.IsPackageActive);
        Assert.False(summary.NeedsReview);
        Assert.Equal(0, summary.Available); Assert.Equal(0, summary.Remaining);
    }

    [Fact]
    public async Task SeventyTwoLessons_HaveIndependentTwentyFourPtAndFortyEightPerformance()
    {
        await using var db = Db();
        var member = Member(db, new() { DurationMonths = 6, LessonsRenewMonthly = true,
            PersonalTrainingSessionCount = 4, PerformanceClassCreditCount = 8 });
        await db.SaveChangesAsync();
        var summary = (await new LessonCreditSummaryService(db).GetAsync(member.Id))!;
        Assert.Equal(72, summary.PackageTotal);
        Assert.Equal(24, summary.Categories.Single(x => x.Category == LessonCategory.PersonalTraining).Available);
        Assert.Equal(48, summary.Categories.Single(x => x.Category == LessonCategory.Performance).Available);
        Assert.Equal(0, summary.Categories.Single(x => x.Category == LessonCategory.Reformer).Available);
        Assert.False(summary.NeedsReview);
    }

    [Theory]
    [InlineData(LessonCategory.Performance)]
    [InlineData(LessonCategory.Reformer)]
    [InlineData(LessonCategory.Group)]
    [InlineData(LessonCategory.Kids)]
    public async Task GroupBookingAndRefund_ChangeOnlyTheSelectedCategoryOnce(LessonCategory category)
    {
        await using var db = Db();
        var member = Member(db, new() { PersonalTrainingSessionCount = 2, PerformanceClassCreditCount = 2,
            ReformerClassCreditCount = 2, GroupClassCreditCount = 2, KidsClassCreditCount = 2 });
        var session = Group(db, member.AssignedTrainer!, category);
        await db.SaveChangesAsync();
        var service = new ClassReservationService(db);
        Assert.True((await service.ReserveAsync(member.ApplicationUserId, session.Id)).Succeeded);
        Assert.Equal(9, member.RemainingClassCredits);
        Assert.All(member.LessonAccounts, a => Assert.Equal(a.Category == category ? 1 : 2, a.Remaining));
        var reservation = await db.ClassReservations.SingleAsync();
        Assert.Equal(category, reservation.LessonAccount!.Category);
        Assert.True((await service.CancelByAdminAsync(session.Id, reservation.Id)).Succeeded);
        Assert.False((await service.CancelByAdminAsync(session.Id, reservation.Id)).Succeeded);
        Assert.All(member.LessonAccounts, a => Assert.Equal(2, a.Remaining));
        Assert.Equal(10, member.RemainingClassCredits);
    }

    [Fact]
    public async Task PerformanceRights_CannotBookReformerOrPersonalTraining()
    {
        await using var db = Db();
        var member = Member(db, new() { PerformanceClassCreditCount = 8 });
        var reformer = Group(db, member.AssignedTrainer!, LessonCategory.Reformer);
        await db.SaveChangesAsync();
        Assert.False((await new ClassReservationService(db).ReserveAsync(member.ApplicationUserId, reformer.Id)).Succeeded);
        Assert.False((await new PersonalTrainingCalendarService(db).CreateByAdminAsync(member.AssignedTrainerId!.Value,
            member.Id, DateTime.UtcNow.AddDays(3), 50, null)).Succeeded);
        Assert.Empty(db.ClassReservations); Assert.Empty(db.PersonalTrainingSessions);
        Assert.Equal(8, member.RemainingClassCredits);
    }

    [Fact]
    public async Task PtCompletionAndCancellation_NeverSpendGroupRights()
    {
        await using var db = Db();
        var member = Member(db, new() { PersonalTrainingSessionCount = 1, PerformanceClassCreditCount = 5 });
        await db.SaveChangesAsync();
        var service = new PersonalTrainingCalendarService(db);
        Assert.True((await service.CreateByAdminAsync(member.AssignedTrainerId!.Value, member.Id, DateTime.UtcNow.AddDays(1), 50, null)).Succeeded);
        Assert.False((await service.CreateByAdminAsync(member.AssignedTrainerId.Value, member.Id, DateTime.UtcNow.AddDays(2), 50, null)).Succeeded);
        var lesson = await db.PersonalTrainingSessions.SingleAsync();
        lesson.StartsAtUtc = DateTime.UtcNow.AddHours(-1); // The lesson has taken place before it is completed.
        await db.SaveChangesAsync();
        Assert.True((await service.ChangeStatusAsync(lesson.TrainerId, lesson.Id, PersonalTrainingSessionStatus.Completed, null, "admin", null)).Succeeded);
        Assert.False((await service.ChangeStatusAsync(lesson.TrainerId, lesson.Id, PersonalTrainingSessionStatus.Completed, null, "admin", null)).Succeeded);
        Assert.Equal(0, Account(member, LessonCategory.PersonalTraining).Remaining);
        Assert.Equal(5, Account(member, LessonCategory.Performance).Remaining);
        Assert.Equal(5, member.RemainingClassCredits);
    }

    [Theory]
    [InlineData(PersonalTrainingSessionStatus.Completed)]
    [InlineData(PersonalTrainingSessionStatus.NoShow)]
    public async Task FuturePtLesson_CannotBeCompletedOrMarkedNoShow(PersonalTrainingSessionStatus status)
    {
        await using var db = Db();
        var member = Member(db, new() { PersonalTrainingSessionCount = 2 });
        await db.SaveChangesAsync();
        var service = new PersonalTrainingCalendarService(db);
        Assert.True((await service.CreateByAdminAsync(member.AssignedTrainerId!.Value, member.Id, DateTime.UtcNow.AddDays(1), 50, null)).Succeeded);
        var lesson = await db.PersonalTrainingSessions.SingleAsync();

        var result = await service.ChangeStatusAsync(lesson.TrainerId, lesson.Id, status, null, "admin", null);

        Assert.False(result.Succeeded);
        Assert.Contains("Henüz başlamamış", result.Message);
        Assert.Equal(PersonalTrainingSessionStatus.Scheduled, lesson.Status);
        Assert.Equal(2, Account(member, LessonCategory.PersonalTraining).Remaining);
    }

    [Fact]
    public async Task OldPeriodRefund_DoesNotAddCreditsToRenewedPackage()
    {
        await using var db = Db();
        var member = Member(db, new() { ReformerClassCreditCount = 2 });
        var session = Group(db, member.AssignedTrainer!, LessonCategory.Reformer);
        await db.SaveChangesAsync();
        var service = new ClassReservationService(db);
        Assert.True((await service.ReserveAsync(member.ApplicationUserId, session.Id)).Succeeded);
        var reservation = await db.ClassReservations.SingleAsync(); var oldAccount = reservation.LessonAccount!;
        CategoryCreditService.Initialize(member, new() { PerformanceClassCreditCount = 8 }, "paid-renewal");
        await db.SaveChangesAsync();
        Assert.True((await service.CancelByAdminAsync(session.Id, reservation.Id)).Succeeded);
        Assert.Equal(2, oldAccount.Remaining);
        Assert.Equal(8, member.RemainingClassCredits);
        Assert.Equal(0, Account(member, LessonCategory.Reformer).Remaining);
    }

    [Fact]
    public async Task Conversion_RequiresRuleAndFreeSourceRights_AndIsIdempotent()
    {
        await using var db = Db();
        var member = Member(db, new() { PersonalTrainingSessionCount = 1, PerformanceClassCreditCount = 8 });
        var rule = new LessonConversionRule { Name = "2 Performance = 1 PT", From = LessonCategory.Performance,
            To = LessonCategory.PersonalTraining, SourceCredits = 2, TargetCredits = 1, CreatedBy = "admin" };
        db.LessonConversionRules.Add(rule); await db.SaveChangesAsync();
        var service = new CategoryCreditAdminService(db);
        var input = new CreditConversionInput { MemberId = member.Id, RuleId = rule.Id, Times = 2, Reason = "Üye talebi ile dönüşüm", OperationId = Guid.NewGuid() };
        Assert.True((await service.ConvertAsync(input, "admin")).Succeeded);
        Assert.True((await service.ConvertAsync(input, "admin")).Succeeded);
        Assert.Equal(3, Account(member, LessonCategory.PersonalTraining).Remaining);
        Assert.Equal(4, Account(member, LessonCategory.Performance).Remaining);
        Assert.Equal(2, await db.LessonCreditMovements.CountAsync(x => x.OperationId == input.OperationId));
        var summary = (await new LessonCreditSummaryService(db).GetAsync(member.Id))!;
        Assert.Equal(9, summary.PackageTotal);
        Assert.Equal(-2, summary.ConvertedCredits);
        Assert.Equal(7, summary.EffectivePackageTotal);
        Assert.False(summary.NeedsReview);
        rule.IsActive = false; await db.SaveChangesAsync(); input.OperationId = Guid.NewGuid();
        Assert.False((await service.ConvertAsync(input, "admin")).Succeeded);
        Assert.Equal(7, member.RemainingClassCredits);
    }

    [Fact]
    public async Task Conversion_CannotTakeAnAlreadyPlannedPtRight()
    {
        await using var db = Db();
        var member = Member(db, new() { PersonalTrainingSessionCount = 1 });
        db.PersonalTrainingSessions.Add(new() { MemberProfile = member, Trainer = member.AssignedTrainer!, StartsAtUtc = DateTime.UtcNow.AddDays(1),
            LessonAccount = Account(member, LessonCategory.PersonalTraining) });
        var rule = new LessonConversionRule { Name = "PT to Group", From = LessonCategory.PersonalTraining, To = LessonCategory.Group, CreatedBy = "admin" };
        db.LessonConversionRules.Add(rule); await db.SaveChangesAsync();
        Assert.False((await new CategoryCreditAdminService(db).ConvertAsync(new() { MemberId = member.Id, RuleId = rule.Id,
            Reason = "Dönüşüm testi", OperationId = Guid.NewGuid() }, "admin")).Succeeded);
        Assert.Equal(1, member.RemainingClassCredits);
    }

    [Fact]
    public async Task Adjustment_AddsAndRemovesCategoryRights_KeepsTotalsConsistent_AndIsIdempotent()
    {
        await using var db = Db();
        var member = Member(db, new() { PersonalTrainingSessionCount = 2, PerformanceClassCreditCount = 8 });
        await db.SaveChangesAsync();
        var service = new CategoryCreditAdminService(db);
        var add = new CreditAdjustmentInput { MemberId = member.Id, Category = LessonCategory.Performance, Delta = 2,
            Reason = "Kaçırılan ders telafisi", OperationId = Guid.NewGuid() };
        Assert.True((await service.AdjustAsync(add, "admin")).Succeeded);
        Assert.True((await service.AdjustAsync(add, "admin")).Succeeded);
        Assert.Equal(10, Account(member, LessonCategory.Performance).Remaining);
        Assert.Equal(1, await db.LessonCreditMovements.CountAsync(x => x.OperationId == add.OperationId));
        Assert.True((await service.AdjustAsync(new() { MemberId = member.Id, Category = LessonCategory.PersonalTraining, Delta = -1,
            Reason = "Hatalı tanımlanan hak", OperationId = Guid.NewGuid() }, "admin")).Succeeded);
        Assert.Equal(1, Account(member, LessonCategory.PersonalTraining).Remaining);
        Assert.Equal(11, member.RemainingClassCredits);
        var summary = (await new LessonCreditSummaryService(db).GetAsync(member.Id))!;
        Assert.Equal(10, summary.PackageTotal);
        Assert.Equal(1, summary.ConvertedCredits);
        Assert.Equal(11, summary.EffectivePackageTotal);
        Assert.False(summary.NeedsReview);
    }

    [Fact]
    public async Task Adjustment_CannotRemoveAPlannedPtRight_OrRunWithoutReason()
    {
        await using var db = Db();
        var member = Member(db, new() { PersonalTrainingSessionCount = 1 });
        db.PersonalTrainingSessions.Add(new() { MemberProfile = member, Trainer = member.AssignedTrainer!, StartsAtUtc = DateTime.UtcNow.AddDays(1),
            LessonAccount = Account(member, LessonCategory.PersonalTraining) });
        await db.SaveChangesAsync();
        var service = new CategoryCreditAdminService(db);
        Assert.False((await service.AdjustAsync(new() { MemberId = member.Id, Category = LessonCategory.PersonalTraining, Delta = -1,
            Reason = "Planlı hakkı silme denemesi", OperationId = Guid.NewGuid() }, "admin")).Succeeded);
        Assert.False((await service.AdjustAsync(new() { MemberId = member.Id, Category = LessonCategory.PersonalTraining, Delta = 1,
            Reason = "kısa", OperationId = Guid.NewGuid() }, "admin")).Succeeded);
        Assert.Equal(1, Account(member, LessonCategory.PersonalTraining).Remaining);
        Assert.Empty(await db.LessonCreditMovements.Where(x => x.Reason.StartsWith("Yönetici düzeltmesi")).ToListAsync());
    }

    [Fact]
    public async Task Adjustment_ChangesAndRemovesMonthlyLimit_WithAuditMovement()
    {
        await using var db = Db();
        var member = Member(db, new() { PerformanceClassCreditCount = 8 });
        await db.SaveChangesAsync();
        var service = new CategoryCreditAdminService(db);
        var account = Account(member, LessonCategory.Performance);
        Assert.True((await service.AdjustAsync(new() { MemberId = member.Id, Category = LessonCategory.Performance,
            ChangeMonthlyLimit = true, MonthlyLimit = 4, Reason = "Aylık plan sınırı", OperationId = Guid.NewGuid() }, "admin")).Succeeded);
        Assert.Equal(4, account.MonthlyLimit);
        Assert.False((await service.AdjustAsync(new() { MemberId = member.Id, Category = LessonCategory.Performance,
            ChangeMonthlyLimit = true, MonthlyLimit = 4, Reason = "Aynı sınır tekrar", OperationId = Guid.NewGuid() }, "admin")).Succeeded);
        Assert.True((await service.AdjustAsync(new() { MemberId = member.Id, Category = LessonCategory.Performance,
            ChangeMonthlyLimit = true, MonthlyLimit = null, Reason = "Sınır kaldırıldı", OperationId = Guid.NewGuid() }, "admin")).Succeeded);
        Assert.Null(account.MonthlyLimit);
        Assert.Equal(8, account.Remaining);
        var limitMovements = await db.LessonCreditMovements.Where(x => x.Reason.StartsWith("Aylık sınır")).ToListAsync();
        Assert.Equal(2, limitMovements.Count);
        Assert.All(limitMovements, movement => Assert.Equal(0, movement.Delta));

        // Credits and limit together must stay one movement: (AccountId, OperationId) is unique in PostgreSQL.
        var combined = new CreditAdjustmentInput { MemberId = member.Id, Category = LessonCategory.Performance, Delta = 2,
            ChangeMonthlyLimit = true, MonthlyLimit = 5, Reason = "Telafi ve yeni sınır", OperationId = Guid.NewGuid() };
        Assert.True((await service.AdjustAsync(combined, "admin")).Succeeded);
        var movement = Assert.Single(await db.LessonCreditMovements.Where(x => x.OperationId == combined.OperationId).ToListAsync());
        Assert.Equal(2, movement.Delta);
        Assert.Contains("Aylık sınır yok → 5", movement.Reason);
        Assert.Equal(5, account.MonthlyLimit);
        Assert.Equal(10, account.Remaining);
        Assert.False((await new LessonCreditSummaryService(db).GetAsync(member.Id))!.NeedsReview);
    }

    [Fact]
    public async Task AmbiguousLegacyBalance_IsBlockedUntilAnExplicitReviewedAllocation()
    {
        await using var db = Db();
        var member = new MemberProfile { ApplicationUser = new() { UserName = "legacy" }, MembershipPackage = new() { Code = MembershipPackageCode.Start },
            MembershipStartsAtUtc = DateTime.UtcNow.AddDays(-1), MembershipEndsAtUtc = DateTime.UtcNow.AddMonths(1), RemainingClassCredits = 3 };
        db.MemberProfiles.Add(member); await db.SaveChangesAsync();
        Assert.NotNull((await new CategoryCreditService(db).PrepareAsync(member, true)).Error);
        Assert.Null(member.LessonPeriodId);
        var input = new CategoryMigrationInput { MemberId = member.Id, ExpectedBalance = 3, Confirmed = true, Reason = "Üye kaydı ve sözleşme doğrulandı",
            Balances = Enum.GetValues<LessonCategory>().Select(c => new CategoryBalanceInput { Category = c, Granted = c == LessonCategory.PersonalTraining ? 8 : 0,
                Remaining = c == LessonCategory.PersonalTraining ? 3 : 0 }).ToList() };
        Assert.True((await new CategoryCreditAdminService(db).MigrateAsync(input, "admin")).Succeeded);
        Assert.False((await new CategoryCreditAdminService(db).MigrateAsync(input, "admin")).Succeeded);
        Assert.Equal(5, await db.MemberLessonAccounts.CountAsync());
        Assert.Null((await new CategoryCreditService(db).PrepareAsync(member, false)).Error);
        Assert.Equal(3, member.RemainingClassCredits);
    }

    [Theory]
    [InlineData(ServicePackageCategory.Membership)]
    [InlineData(ServicePackageCategory.PersonalTraining)]
    [InlineData(ServicePackageCategory.GroupClasses)]
    [InlineData(ServicePackageCategory.KidsClub)]
    public async Task PaidPurchase_GrantsFrozenCategoryRightsExactlyOnce(ServicePackageCategory category)
    {
        await using var db = Db();
        var member = new MemberProfile { ApplicationUser = new() { UserName = "buyer" }, MembershipPackage = new() { Code = MembershipPackageCode.Start }, MembershipActivationPending = true };
        var variant = new ServicePackageVariant { Name = "Paket", DurationMonths = 6, TotalPrice = 72000, PersonalTrainingSessionCount = 4,
            PerformanceClassCreditCount = 8, ReformerClassCreditCount = 2, KidsClassCreditCount = 1, LessonsRenewMonthly = true,
            ServicePackage = new() { Slug = "purchase-test", Name = "Satın alma", Category = category } };
        db.AddRange(member, variant); await db.SaveChangesAsync();
        var service = new MembershipRenewalService(db);
        var created = await service.CreateOrderAsync(member.ApplicationUserId, variant.Id, "QA", "05550000000", "Adres", "İlçe", "İstanbul");
        Assert.Null(created.Error);
        var order = await db.Orders.SingleAsync();
        Assert.False(await service.ActivatePaidOrderAsync(order.Id));
        Assert.Empty(db.MemberLessonAccounts);
        variant.PersonalTrainingSessionCount = 99; variant.PerformanceClassCreditCount = 0; variant.DurationMonths = 1;
        order.PaymentStatus = PaymentStatus.Paid; await db.SaveChangesAsync();
        Assert.True(await service.ActivatePaidOrderAsync(order.Id)); var period = member.LessonPeriodId;
        Assert.True(await service.ActivatePaidOrderAsync(order.Id));
        Assert.Equal(period, member.LessonPeriodId); Assert.False(member.MembershipActivationPending);
        Assert.Equal(24, Account(member, LessonCategory.PersonalTraining).Remaining);
        Assert.Equal(48, Account(member, LessonCategory.Performance).Remaining);
        Assert.Equal(12, Account(member, LessonCategory.Reformer).Remaining);
        Assert.Equal(6, Account(member, LessonCategory.Kids).Remaining);
        Assert.Equal(90, member.RemainingClassCredits);
        Assert.Equal(member.MembershipStartsAtUtc!.Value.AddMonths(6), member.MembershipEndsAtUtc);
        Assert.Equal(5, await db.MemberLessonAccounts.CountAsync());
    }

    [Fact]
    public async Task LegacyPtOnlyBalance_WithManualAdjustment_IsAdoptedAsPersonalTrainingRights()
    {
        await using var db = Db();
        var member = LegacyMember(db, new ServicePackageVariant { Name = "COMMIT 70", TotalPrice = 98000, PersonalTrainingSessionCount = 70 }, storedBalance: 63);
        AddPersonalSessions(db, member, PersonalTrainingSessionStatus.Completed, count: 12, creditConsumed: true, daysFromNow: -3);
        AddPersonalSessions(db, member, PersonalTrainingSessionStatus.Scheduled, count: 2, creditConsumed: false, daysFromNow: 3);
        await db.SaveChangesAsync();
        var service = new CategoryCreditService(db);

        var allocation = await service.PrepareAsync(member, true);
        await db.SaveChangesAsync();

        Assert.Null(allocation.Error);
        Assert.NotNull(member.LessonPeriodId);
        var pt = Account(member, LessonCategory.PersonalTraining);
        Assert.Equal(70, pt.Granted);
        Assert.Equal(63, pt.Remaining);
        Assert.Equal(63, member.RemainingClassCredits);
        Assert.All(member.LessonAccounts.Where(x => x.Category != LessonCategory.PersonalTraining), x => Assert.Equal(0, x.Remaining));
        Assert.Contains(pt.Movements, x => x.Reason.Contains("kayıtlı bakiye olduğu gibi devralındı") && x.Reason.Contains("58"));
        Assert.Equal(61, await service.AvailableAsync(member, pt));
    }

    [Fact]
    public async Task LegacyPtOnlyBalance_WithUnchargedCompletedLesson_StillRequiresReview()
    {
        await using var db = Db();
        var member = LegacyMember(db, new ServicePackageVariant { Name = "ROUTINE 36", TotalPrice = 54000, PersonalTrainingSessionCount = 36 }, storedBalance: 28);
        AddPersonalSessions(db, member, PersonalTrainingSessionStatus.Completed, count: 8, creditConsumed: true, daysFromNow: -5);
        AddPersonalSessions(db, member, PersonalTrainingSessionStatus.Completed, count: 1, creditConsumed: false, daysFromNow: -1);
        await db.SaveChangesAsync();

        var allocation = await new CategoryCreditService(db).PrepareAsync(member, true);

        Assert.Equal(CategoryCreditService.ReviewMessage, allocation.Error);
        Assert.Null(member.LessonPeriodId);
    }

    [Fact]
    public async Task LegacyMixedPackageMismatch_StillRequiresReview()
    {
        await using var db = Db();
        var member = LegacyMember(db, new ServicePackageVariant { Name = "HYBRID", TotalPrice = 13000, PersonalTrainingSessionCount = 4,
            PerformanceClassCreditCount = 8 }, storedBalance: 10);
        await db.SaveChangesAsync();

        var allocation = await new CategoryCreditService(db).PrepareAsync(member, true);

        Assert.Equal(CategoryCreditService.ReviewMessage, allocation.Error);
        Assert.Null(member.LessonPeriodId);
    }

    private static MemberProfile LegacyMember(ApplicationDbContext db, ServicePackageVariant variant, int storedBalance)
    {
        variant.ServicePackage = new() { Slug = Guid.NewGuid().ToString(), Name = variant.Name, Category = ServicePackageCategory.PersonalTraining };
        var member = new MemberProfile { ApplicationUser = new() { UserName = Guid.NewGuid().ToString() },
            MembershipPackage = new() { Code = MembershipPackageCode.Start }, ServicePackageVariant = variant,
            MembershipStartsAtUtc = DateTime.UtcNow.AddDays(-30), MembershipEndsAtUtc = DateTime.UtcNow.AddMonths(3),
            RemainingClassCredits = storedBalance };
        db.Add(member);
        return member;
    }

    private static void AddPersonalSessions(ApplicationDbContext db, MemberProfile member, PersonalTrainingSessionStatus status,
        int count, bool creditConsumed, int daysFromNow)
    {
        var trainer = new Trainer { FirstName = "QA", IsActive = true };
        for (var index = 0; index < count; index++)
            db.Add(new PersonalTrainingSession { Trainer = trainer, MemberProfile = member, Status = status, CreditConsumed = creditConsumed,
                StartsAtUtc = DateTime.UtcNow.AddDays(daysFromNow).AddHours(index) });
    }

    private static MemberLessonAccount Account(MemberProfile member, LessonCategory c) => member.LessonAccounts.Single(a => a.PeriodId == member.LessonPeriodId && a.Category == c);
    private static ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static MemberProfile Member(ApplicationDbContext db, ServicePackageVariant definition)
    {
        var member = new MemberProfile { ApplicationUser = new() { UserName = Guid.NewGuid().ToString() },
            MembershipPackage = new() { Code = MembershipPackageCode.Start }, AssignedTrainer = new() { FirstName = "QA", IsActive = true },
            MembershipStartsAtUtc = DateTime.UtcNow.AddDays(-1), MembershipEndsAtUtc = DateTime.UtcNow.AddMonths(6) };
        CategoryCreditService.Initialize(member, definition, "test-purchase"); db.Add(member); return member;
    }
    private static ClassSession Group(ApplicationDbContext db, Trainer trainer, LessonCategory category)
    {
        var session = new ClassSession { StartsAtUtc = DateTime.UtcNow.AddDays(2), GroupClass = new() {
            Trainer = trainer, Name = category.ToString(), CreditCategory = category, Capacity = 8, DurationMinutes = 50, IsActive = true } };
        db.Add(session); return session;
    }
}
