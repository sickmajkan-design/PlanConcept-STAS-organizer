using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Book;
using Construction.Application.Features.BookImport;
using Construction.Application.Features.Branches;
using Construction.Application.Features.Invoices.Commands.InvoicePayments;
using Construction.Application.Features.Ledgers.Commands;
using Construction.Application.Features.Ledgers.Commands.CreateLedger;
using Construction.Application.Features.Ledgers.Commands.LockLedger;
using Construction.Application.Features.Ledgers.Commands.SetLedgerCell;
using Construction.Application.Features.Ledgers.Models;
using Construction.Application.Features.Overview;
using Construction.Application.Features.Payouts;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// What the screens of the approved mockup need from the server beyond the first version: payments and
/// payouts as lines of the book, the export, the month site by site, the overview's parts and the payables.
/// Every test works in a business unit of its own.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class BookScreensTests : IntegrationTestBase
{
    public BookScreensTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static int nextYear = 2030;

    private static int FreshYear() => Interlocked.Increment(ref nextYear);

    private async Task<T> AsSuperAdminAsync<T>(Func<TestScope, Task<T>> action)
    {
        return await InScope(async scope =>
        {
            var user = await TestData.SeedUserAsync(scope, UserRole.SuperAdmin);
            scope.CurrentUser.SignInAs(user.Id, user.Role, null, user.Email);

            return await action(scope);
        });
    }

    private Task<Guid> BranchAsync() =>
        AsSuperAdminAsync(async scope =>
            (await scope.Send(new CreateBranchCommand { Name = $"Jedinica {Guid.NewGuid():N}"[..20], Color = "#3457D5" })).Id);

    [Fact]
    public async Task A_payment_against_an_invoice_and_a_payout_are_lines_of_the_book()
    {
        var branch = await BranchAsync();
        var year = 2026;

        var (invoiceId, projectId) = await InScope(async scope =>
        {
            var project = await TestData.SeedProjectAsync(scope);
            project.BranchId = branch;
            await scope.Db.SaveChangesAsync();

            var invoice = new Invoice
            {
                ProjectId = project.Id,
                Number = $"R-{Guid.NewGuid():N}"[..14],
                IssueDate = new DateOnly(year, 1, 10),
                DueDate = new DateOnly(year, 2, 10),
                Amount = 1000m,
                PayrollYear = year,
                PayrollMonth = 1,
            };

            scope.Db.Invoices.Add(invoice);
            await scope.Db.SaveChangesAsync();

            return (invoice.Id, project.Id);
        });

        await AsSuperAdminAsync(scope => scope.Send(new AddInvoicePaymentCommand
        {
            InvoiceId = invoiceId, Amount = 400m, PaidOn = new DateOnly(year, 1, 20), Method = BookPaymentMethod.Bank,
        }));

        await AsSuperAdminAsync(async scope =>
        {
            await scope.Send(new CreateWagePayoutCommand
            {
                Year = year, Month = 1, Group = PayoutGroup.Workers, Kind = PayoutKind.Advance,
                PayDate = new DateOnly(year, 1, 25), Amount = 150m, BranchId = branch,
            });
            return 0;
        });

        var page = await AsSuperAdminAsync(scope => scope.Send(new GetBookEntriesQuery
        {
            From = new DateOnly(year, 1, 1), To = new DateOnly(year, 1, 31), BranchId = branch, PageSize = 100,
        }));

        var payment = Assert.Single(page.Items, r => r.Source == "InvoicePayment");
        Assert.Equal("Payment", payment.Kind);
        Assert.Equal(400m, payment.Amount);
        Assert.Equal("Bank", payment.Method);
        Assert.Equal(projectId, payment.ProjectId);

        var payout = Assert.Single(page.Items, r => r.Source == "WagePayout");
        Assert.Equal("Payout", payout.Kind);
        Assert.Equal("Advance", payout.Origin);
        Assert.Equal(150m, payout.Amount);

        // The invoice itself is still a line of income, open for what has not been paid.
        Assert.Contains(page.Items, r => r.Source == "Invoice" && r.Kind == "Income");
    }

    [Fact]
    public async Task The_book_is_exported_as_a_spreadsheet_with_the_selection()
    {
        var branch = await BranchAsync();
        var year = FreshYear();

        var file = await AsSuperAdminAsync(async scope =>
        {
            var fixedCosts = await scope.Db.BookCategories
                .Where(c => c.SystemKey == BookRules.Keys.FixedCosts).Select(c => c.Id).SingleAsync();

            await scope.Send(new CreateBookEntryCommand
            {
                Kind = BookEntryKind.Cost, Amount = 120m, OccurredOn = new DateOnly(2026, 3, 1),
                CategoryId = fixedCosts, Description = "Knjigovodstvo", BranchId = branch,
            });

            return await scope.Send(new GetBookExportQuery
            {
                From = new DateOnly(2026, 1, 1), To = new DateOnly(2026, 12, 31), BranchId = branch,
            });
        });

        Assert.EndsWith(".xlsx", file.FileName);
        Assert.True(file.Content.Length > 1000);
        Assert.NotEqual(0, year);
    }

    [Fact]
    public async Task The_month_is_summarised_site_by_site_down_to_what_the_firm_earned()
    {
        var branch = await BranchAsync();
        var year = FreshYear();

        var projectA = await InScope(async scope =>
        {
            var project = await TestData.SeedProjectAsync(scope, name: $"Hala {Guid.NewGuid():N}"[..14]);
            project.BranchId = branch;
            project.BillingMode = ProjectBillingMode.FlatRate;
            project.ClientHourlyRate = 40m;
            await scope.Db.SaveChangesAsync();

            return project;
        });

        await AsSuperAdminAsync(async scope =>
        {
            var ledger = await scope.Send(new CreateLedgerCommand
            {
                Name = $"Obracun {year}", Year = year, Month = 5, BranchId = branch, Template = LedgerTemplates.Payroll,
            });

            var section = await scope.Send(new AddLedgerSectionCommand
            {
                LedgerId = ledger.Id, Name = projectA.Name, ProjectId = projectA.Id,
            });
            var row = await scope.Send(new AddLedgerRowCommand { SectionId = section.Id, Label = "Radnik" });

            Guid Column(string key) => ledger.Columns.Single(c => c.SystemKey == key).Id;

            async Task Set(string key, string value) =>
                await scope.Send(new SetLedgerCellCommand { RowId = row.Id, ColumnId = Column(key), Value = value });

            await Set(LedgerTemplates.Keys.WorkerRate, "20");
            await Set(LedgerTemplates.Keys.Week(1), "100");
            await Set(LedgerTemplates.Keys.ClientRate, "33");
            await Set(LedgerTemplates.Keys.Contributions, "200");

            var fixedCosts = await scope.Db.BookCategories
                .Where(c => c.SystemKey == BookRules.Keys.FixedCosts).Select(c => c.Id).SingleAsync();

            await scope.Send(new CreateBookEntryCommand
            {
                Kind = BookEntryKind.Cost, Amount = 300m, OccurredOn = new DateOnly(2026, 4, 1),
                CategoryId = fixedCosts, Description = "Fiksno", BranchId = branch,
            });

            return 0;
        });

        // The month is the settlement's (year, 5); the fixed cost of that month is dated in it.
        await AsSuperAdminAsync(async scope =>
        {
            var fixedCosts = await scope.Db.BookCategories
                .Where(c => c.SystemKey == BookRules.Keys.FixedCosts).Select(c => c.Id).SingleAsync();

            await scope.Send(new CreateBookEntryCommand
            {
                Kind = BookEntryKind.Cost, Amount = 250m, OccurredOn = new DateOnly(2026, 5, 3),
                CategoryId = fixedCosts, Description = "Osiguranje", BranchId = branch,
            });

            return 0;
        });

        var summary = await AsSuperAdminAsync(scope => scope.Send(new GetSettlementSummaryQuery
        {
            Year = year, Month = 5, BranchId = branch,
        }));

        Assert.True(summary.HasSettlement);

        var site = Assert.Single(summary.Sites);
        Assert.Equal("FlatRate", site.BillingMode);
        Assert.Equal(100m, site.Hours);
        Assert.Equal(2000m, site.Wage);
        Assert.Equal(200m, site.Costs);
        Assert.Equal(4000m, site.ByHours);
        Assert.Equal(40m, site.ClientRate);
    }

    [Fact]
    public async Task How_a_site_is_billed_can_be_changed_from_the_settlement()
    {
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));

        await AsSuperAdminAsync(async scope =>
        {
            await scope.Send(new UpdateSiteBillingCommand
            {
                ProjectId = project.Id, BillingMode = ProjectBillingMode.Measured, ClientHourlyRate = 35m,
            });
            return 0;
        });

        var stored = await InScope(scope => scope.Db.Projects.AsNoTracking()
            .Where(p => p.Id == project.Id).Select(p => new { p.BillingMode, p.ClientHourlyRate }).SingleAsync());

        Assert.Equal(ProjectBillingMode.Measured, stored.BillingMode);
        Assert.Equal(35m, stored.ClientHourlyRate);
    }

    [Fact]
    public async Task What_is_owed_to_the_workers_is_a_payable_until_it_is_paid_out()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var branch = await BranchAsync();

        await AsSuperAdminAsync(async scope =>
        {
            var ledger = await scope.Send(new CreateLedgerCommand
            {
                Name = $"Obracun {Guid.NewGuid():N}"[..20],
                Year = today.Year,
                Month = today.Month,
                BranchId = branch,
                Template = LedgerTemplates.Payroll,
            });

            var section = await scope.Send(new AddLedgerSectionCommand { LedgerId = ledger.Id, Name = "Klijent" });
            var row = await scope.Send(new AddLedgerRowCommand { SectionId = section.Id, Label = "Radnik" });

            Guid Column(string key) => ledger.Columns.Single(c => c.SystemKey == key).Id;

            await scope.Send(new SetLedgerCellCommand
            {
                RowId = row.Id, ColumnId = Column(LedgerTemplates.Keys.WorkerRate), Value = "20",
            });
            await scope.Send(new SetLedgerCellCommand
            {
                RowId = row.Id, ColumnId = Column(LedgerTemplates.Keys.Week(1)), Value = "50",
            });

            await scope.Send(new CreateWagePayoutCommand
            {
                Year = today.Year, Month = today.Month, Group = PayoutGroup.Workers, Kind = PayoutKind.Advance,
                PayDate = today, Amount = 300m, BranchId = branch,
            });

            return 0;
        });

        var debts = await AsSuperAdminAsync(scope => scope.Send(new GetPayablesQuery { BranchId = branch }));

        var payroll = Assert.Single(debts.Rows, r => r.Source == "Payroll" && r.Counterparty == "Workers");
        Assert.Equal(1000m, payroll.Amount);
        Assert.Equal(700m, payroll.OpenAmount);
    }

    [Fact]
    public async Task The_overview_says_where_the_money_goes_and_lists_the_cash_events()
    {
        var branch = await BranchAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await AsSuperAdminAsync(async scope =>
        {
            var fixedCosts = await scope.Db.BookCategories
                .Where(c => c.SystemKey == BookRules.Keys.FixedCosts).Select(c => c.Id).SingleAsync();

            await scope.Send(new CreateBookEntryCommand
            {
                Kind = BookEntryKind.Cost, Amount = 500m, OccurredOn = new DateOnly(2026, 2, 3),
                CategoryId = fixedCosts, Description = "Najam", BranchId = branch,
                DueDate = today.AddDays(5),
            });

            return 0;
        });

        var overview = await AsSuperAdminAsync(scope => scope.Send(new GetOverviewQuery
        {
            From = new DateOnly(2026, 2, 1), To = new DateOnly(2026, 2, 28), BranchId = branch,
        }));

        var fixedKind = Assert.Single(overview.CostsByKind);
        Assert.Equal("Fiksni troškovi", fixedKind.Key);
        Assert.Equal(500m, fixedKind.Amount);
        Assert.Equal(100m, fixedKind.SharePercent);

        var cashflow = await AsSuperAdminAsync(scope => scope.Send(new GetCashflowQuery
        {
            Days = 35, OpeningBalance = 100m, BranchId = branch,
        }));

        var cost = Assert.Single(cashflow.Events);
        Assert.Equal("Cost", cost.Kind);
        Assert.Equal(-500m, cost.Amount);
        Assert.Equal(-400m, cost.BalanceAfter);
        Assert.Equal("Najam", cost.What);
    }

    [Fact]
    public async Task A_category_the_owner_made_can_move_to_another_cost_bucket_but_a_system_one_cannot()
    {
        var created = await AsSuperAdminAsync(scope => scope.Send(new CreateBookCategoryCommand
        {
            Name = $"Transport {Guid.NewGuid():N}"[..20],
            Kind = BookCategoryKind.Cost,
            Bucket = BookCategoryBucket.Fixed,
        }));

        var moved = await AsSuperAdminAsync(scope => scope.Send(new UpdateBookCategoryCommand
        {
            Id = created.Id, Name = created.Name, IsActive = true, Bucket = BookCategoryBucket.SiteCosts,
        }));

        Assert.Equal("SiteCosts", moved.Bucket);

        await Assert.ThrowsAsync<ConflictException>(() => AsSuperAdminAsync(async scope =>
        {
            var fuel = await scope.Db.BookCategories.AsNoTracking().SingleAsync(c => c.SystemKey == BookRules.Keys.Fuel);

            return await scope.Send(new UpdateBookCategoryCommand
            {
                Id = fuel.Id, Name = fuel.Name, IsActive = true, Bucket = BookCategoryBucket.Fixed,
            });
        }));

        await Assert.ThrowsAsync<ConflictException>(() => AsSuperAdminAsync(scope =>
            scope.Send(new UpdateBookCategoryCommand
            {
                Id = created.Id, Name = created.Name, IsActive = true, Bucket = BookCategoryBucket.Collection,
            })));
    }

    [Fact]
    public async Task An_import_can_make_the_people_the_system_does_not_know()
    {
        var tag = Guid.NewGuid().ToString("N")[..8];
        var year = 2040 + Math.Abs(tag.GetHashCode() % 50);

        var result = await AsSuperAdminAsync(scope => scope.Send(new CommitBookImportCommand(new BookImportInput
        {
            FileName = "uvoz.xlsx",
            Year = year,
            Month = 3,
            CreateUnknownEmployees = true,
            Rows =
            [
                new BookImportRowInput
                {
                    Section = $"Klijent {tag}", Label = $"Novi{tag} Radnik{tag}", Weeks = ["40"], WorkerRate = "20", ClientRate = "30",
                },
            ],
        })));

        Assert.Equal(1, result.Imported);

        var created = await InScope(scope => scope.Db.Employees
            .AnyAsync(e => e.FirstName == $"Novi{tag}" && e.LastName == $"Radnik{tag}"));
        Assert.True(created);

        var linked = await InScope(scope => scope.Db.LedgerRows
            .AnyAsync(r => r.Section.LedgerId == result.LedgerId && r.EmployeeId != null));
        Assert.True(linked);
    }
}
