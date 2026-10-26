using Construction.Application.Common;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Construction.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<FailedLogin> FailedLogins => Set<FailedLogin>();

    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    public DbSet<MfaRecoveryCode> MfaRecoveryCodes => Set<MfaRecoveryCode>();

    public DbSet<MfaChallenge> MfaChallenges => Set<MfaChallenge>();

    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<CompanySettings> CompanySettings => Set<CompanySettings>();

    public DbSet<DashboardLayout> DashboardLayouts => Set<DashboardLayout>();

    public DbSet<DashboardSnapshot> DashboardSnapshots => Set<DashboardSnapshot>();

    public DbSet<GeneralExpense> GeneralExpenses => Set<GeneralExpense>();

    public DbSet<Accommodation> Accommodations => Set<Accommodation>();

    public DbSet<AccommodationRate> AccommodationRates => Set<AccommodationRate>();
    public DbSet<AccommodationStay> AccommodationStays => Set<AccommodationStay>();

    public DbSet<Ledger> Ledgers => Set<Ledger>();

    public DbSet<LedgerColumn> LedgerColumns => Set<LedgerColumn>();

    public DbSet<LedgerSection> LedgerSections => Set<LedgerSection>();

    public DbSet<LedgerRow> LedgerRows => Set<LedgerRow>();

    public DbSet<LedgerCell> LedgerCells => Set<LedgerCell>();

    public DbSet<LedgerSummaryBox> LedgerSummaryBoxes => Set<LedgerSummaryBox>();

    public DbSet<EmployeeProject> EmployeeProjects => Set<EmployeeProject>();

    public DbSet<ProjectStaffingNeed> ProjectStaffingNeeds => Set<ProjectStaffingNeed>();

    public DbSet<EmployeeCertificate> EmployeeCertificates => Set<EmployeeCertificate>();

    public DbSet<ProjectCertificateRequirement> ProjectCertificateRequirements => Set<ProjectCertificateRequirement>();

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    public DbSet<Tool> Tools => Set<Tool>();

    public DbSet<Material> Materials => Set<Material>();

    public DbSet<LocationRecord> LocationRecords => Set<LocationRecord>();

    public DbSet<DeviceToken> DeviceTokens => Set<DeviceToken>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<EmployeeInvitation> EmployeeInvitations => Set<EmployeeInvitation>();

    public DbSet<NotificationGroup> NotificationGroups => Set<NotificationGroup>();

    public DbSet<NotificationGroupMember> NotificationGroupMembers => Set<NotificationGroupMember>();

    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();

    public DbSet<Attachment> Attachments => Set<Attachment>();

    public DbSet<AttachmentExpiryReminder> AttachmentExpiryReminders => Set<AttachmentExpiryReminder>();

    public DbSet<AttachmentRetentionReminder> AttachmentRetentionReminders => Set<AttachmentRetentionReminder>();

    public DbSet<WorkItem> WorkItems => Set<WorkItem>();

    public DbSet<Absence> Absences => Set<Absence>();

    public DbSet<ArticleOrder> ArticleOrders => Set<ArticleOrder>();

    public DbSet<ArticleOrderItem> ArticleOrderItems => Set<ArticleOrderItem>();

    public DbSet<Refund> Refunds => Set<Refund>();

    public DbSet<DocumentRequest> DocumentRequests => Set<DocumentRequest>();

    public DbSet<DocumentRequestEvent> DocumentRequestEvents => Set<DocumentRequestEvent>();

    public DbSet<DocumentRequestReminder> DocumentRequestReminders => Set<DocumentRequestReminder>();
    public DbSet<LeaveAdjustment> LeaveAdjustments => Set<LeaveAdjustment>();
    public DbSet<CustomerCompany> CustomerCompanies => Set<CustomerCompany>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceShare> InvoiceShares => Set<InvoiceShare>();

    public DbSet<SignedTimesheet> SignedTimesheets => Set<SignedTimesheet>();

    public DbSet<EmployeeRate> EmployeeRates => Set<EmployeeRate>();

    public DbSet<MaterialMovement> MaterialMovements => Set<MaterialMovement>();

    public DbSet<VehicleExpense> VehicleExpenses => Set<VehicleExpense>();

    public DbSet<FuelCard> FuelCards => Set<FuelCard>();

    public DbSet<VehicleDateReminder> VehicleDateReminders => Set<VehicleDateReminder>();

    public DbSet<FuelTransaction> FuelTransactions => Set<FuelTransaction>();

    public DbSet<FuelImportBatch> FuelImportBatches => Set<FuelImportBatch>();

    public DbSet<VehicleRentalRate> VehicleRentalRates => Set<VehicleRentalRate>();

    public DbSet<VehicleToll> VehicleTolls => Set<VehicleToll>();

    public DbSet<VehicleTollPayment> VehicleTollPayments => Set<VehicleTollPayment>();

    public DbSet<VehicleTollExpiryReminder> VehicleTollExpiryReminders => Set<VehicleTollExpiryReminder>();

    public DbSet<VehicleRentalOut> VehicleRentalsOut => Set<VehicleRentalOut>();

    public DbSet<ToolExpense> ToolExpenses => Set<ToolExpense>();
    public DbSet<ToolKitItem> ToolKitItems => Set<ToolKitItem>();
    public DbSet<ToolKitEvent> ToolKitEvents => Set<ToolKitEvent>();

    public DbSet<ToolRentalRate> ToolRentalRates => Set<ToolRentalRate>();

    public DbSet<ToolRentalOut> ToolRentalsOut => Set<ToolRentalOut>();

    public DbSet<PublicHoliday> PublicHolidays => Set<PublicHoliday>();

    public DbSet<Branch> Branches => Set<Branch>();

    public DbSet<EmployeeBranch> EmployeeBranches => Set<EmployeeBranch>();

    public DbSet<BulletinPost> BulletinPosts => Set<BulletinPost>();

    public DbSet<BulletinView> BulletinViews => Set<BulletinView>();

    public DbSet<FinanceEntry> FinanceEntries => Set<FinanceEntry>();

    public DbSet<ProjectRevenue> ProjectRevenues => Set<ProjectRevenue>();

    public DbSet<CompanyRevenue> CompanyRevenues => Set<CompanyRevenue>();

    public DbSet<BookCategory> BookCategories => Set<BookCategory>();

    public DbSet<BookCategoryRule> BookCategoryRules => Set<BookCategoryRule>();

    public DbSet<BookEntry> BookEntries => Set<BookEntry>();

    public DbSet<InvoicePayment> InvoicePayments => Set<InvoicePayment>();

    public DbSet<WagePayout> WagePayouts => Set<WagePayout>();

    public DbSet<RecurringExpense> RecurringExpenses => Set<RecurringExpense>();

    public DbSet<BookImportBatch> BookImportBatches => Set<BookImportBatch>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<ScheduledReportSubscription> ScheduledReportSubscriptions => Set<ScheduledReportSubscription>();

    public DbSet<NotificationBroadcast> NotificationBroadcasts => Set<NotificationBroadcast>();

    public DbSet<WeeklySiteReport> WeeklySiteReports => Set<WeeklySiteReport>();

    public DbSet<WeeklySiteReportPage> WeeklySiteReportPages => Set<WeeklySiteReportPage>();

    public DbSet<WeeklySiteReportLine> WeeklySiteReportLines => Set<WeeklySiteReportLine>();

    public DbSet<WeeklyReportReminder> WeeklyReportReminders => Set<WeeklyReportReminder>();

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        // Joining an open transaction rather than nesting: PostgreSQL has no
        // nested transactions, and starting a second one here would throw
        // rather than do what the caller means. The outermost caller owns the
        // commit either way.
        if (Database.CurrentTransaction is not null)
        {
            await action(cancellationToken);
            return;
        }

        // The execution strategy owns the retry loop, so the whole block is
        // replayed on a transient failure instead of half of it.
        var strategy = Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction =
                await Database.BeginTransactionAsync(cancellationToken);

            await action(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        });
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // An instant as the date and time on the company's clock, worked out in the database: a day taken from a
        // shift's start is the company's day, not the UTC one. PostgreSQL's own timezone(zone, instant); the zone is
        // a parameter of the call, so it is the query's parameter rather than part of its cached text.
        modelBuilder.HasDbFunction(
                typeof(CompanyTime).GetMethod(nameof(CompanyTime.InZone), [typeof(DateTime), typeof(string)])!)
            .HasTranslation(args => new SqlFunctionExpression(
                "timezone",
                [args[1], args[0]],
                nullable: true,
                argumentsPropagateNullability: [true, true],
                typeof(DateTime),
                args[0].TypeMapping));

        base.OnModelCreating(modelBuilder);
    }
}
