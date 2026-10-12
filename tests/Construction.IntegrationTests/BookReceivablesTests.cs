using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Book;
using Construction.Application.Features.Invoices.Commands.ChangeInvoiceStatus;
using Construction.Application.Features.Invoices.Commands.InvoicePayments;
using Construction.Application.Features.Ledgers.Commands;
using Construction.Application.Features.Ledgers.Commands.CreateLedger;
using Construction.Application.Features.Ledgers.Models;
using Construction.Application.Features.Ledgers.Queries.GetLedgerSectionRows;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// Phase 2 of the book of money: part-payments of invoices, what clients and the firm still owe, and the
/// client's hourly price taken from the site into the monthly settlement.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class BookReceivablesTests : IntegrationTestBase
{
    public BookReceivablesTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    private async Task<T> AsSuperAdminAsync<T>(Func<TestScope, Task<T>> action)
    {
        return await InScope(async scope =>
        {
            var user = await TestData.SeedUserAsync(scope, UserRole.SuperAdmin);
            scope.CurrentUser.SignInAs(user.Id, user.Role, null, user.Email);

            return await action(scope);
        });
    }

    private async Task<(Guid InvoiceId, Guid ProjectId)> SeedInvoiceAsync(
        decimal amount = 1000m, DateOnly? dueDate = null, Guid? projectId = null)
    {
        return await InScope(async scope =>
        {
            var project = projectId is null
                ? await TestData.SeedProjectAsync(scope)
                : await scope.Db.Projects.SingleAsync(p => p.Id == projectId);

            var invoice = new Invoice
            {
                ProjectId = project.Id,
                Number = $"R-{Unique()}",
                IssueDate = new DateOnly(2026, 1, 10),
                DueDate = dueDate,
                Amount = amount,
                PayrollYear = 2026,
                PayrollMonth = 1,
            };

            scope.Db.Invoices.Add(invoice);
            await scope.Db.SaveChangesAsync();

            return (invoice.Id, project.Id);
        });
    }

    private Task<Construction.Application.Features.Invoices.Models.InvoiceDto> PayAsync(
        Guid invoiceId, decimal amount, DateOnly? paidOn = null) =>
        AsSuperAdminAsync(scope => scope.Send(new AddInvoicePaymentCommand
        {
            InvoiceId = invoiceId,
            Amount = amount,
            PaidOn = paidOn ?? new DateOnly(2026, 2, 1),
        }));

    [Fact]
    public async Task An_invoice_paid_in_parts_stays_open_until_the_parts_reach_its_amount()
    {
        var (id, _) = await SeedInvoiceAsync(1000m);

        var first = await PayAsync(id, 400m);

        Assert.Equal("Issued", first.Status);
        Assert.Equal(400m, first.PaidAmount);
        Assert.Equal(600m, first.RemainingAmount);

        var second = await PayAsync(id, 600m);

        Assert.Equal("Paid", second.Status);
        Assert.Equal(1000m, second.PaidAmount);
        Assert.Equal(0m, second.RemainingAmount);
        Assert.Equal(2, second.Payments.Count);
    }

    [Fact]
    public async Task A_payment_larger_than_what_is_open_is_refused()
    {
        var (id, _) = await SeedInvoiceAsync(1000m);

        await PayAsync(id, 700m);

        await Assert.ThrowsAsync<ConflictException>(() => PayAsync(id, 400m));

        var open = await InScope(scope => scope.Db.InvoicePayments.Where(p => p.InvoiceId == id).SumAsync(p => p.Amount));
        Assert.Equal(700m, open);
    }

    [Fact]
    public async Task Marking_an_invoice_paid_records_what_was_left_as_a_payment()
    {
        var (id, _) = await SeedInvoiceAsync(1000m);

        await PayAsync(id, 250m);

        var paid = await AsSuperAdminAsync(scope => scope.Send(new MarkInvoicePaidCommand(id)));

        Assert.Equal("Paid", paid.Status);
        Assert.Equal(1000m, paid.PaidAmount);
        Assert.Equal(750m, paid.Payments.Max(p => p.Amount));
    }

    [Fact]
    public async Task Taking_back_a_payment_reopens_a_paid_invoice()
    {
        var (id, _) = await SeedInvoiceAsync(500m);

        var paid = await PayAsync(id, 500m);
        Assert.Equal("Paid", paid.Status);

        var reopened = await AsSuperAdminAsync(scope =>
            scope.Send(new RemoveInvoicePaymentCommand(id, paid.Payments.Single().Id)));

        Assert.Equal("Issued", reopened.Status);
        Assert.Equal(0m, reopened.PaidAmount);
        Assert.Equal(500m, reopened.RemainingAmount);
    }

    [Fact]
    public async Task An_invoice_with_a_payment_cannot_be_cancelled()
    {
        var (id, _) = await SeedInvoiceAsync(500m);

        await PayAsync(id, 100m);

        await Assert.ThrowsAsync<ConflictException>(() => AsSuperAdminAsync(scope =>
            scope.Send(new CancelInvoiceCommand { Id = id, Reason = "Pogreska" })));
    }

    [Fact]
    public async Task A_payment_cannot_be_dated_before_the_invoice_was_issued()
    {
        var (id, _) = await SeedInvoiceAsync(500m);

        await Assert.ThrowsAsync<ConflictException>(() => PayAsync(id, 100m, new DateOnly(2025, 12, 31)));
    }

    [Fact]
    public async Task Receivables_are_aged_by_due_date_and_less_what_was_paid()
    {
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var notDue = await SeedInvoiceAsync(100m, today.AddDays(5), project.Id);
        var late10 = await SeedInvoiceAsync(200m, today.AddDays(-10), project.Id);
        var late45 = await SeedInvoiceAsync(300m, today.AddDays(-45), project.Id);
        var late90 = await SeedInvoiceAsync(400m, today.AddDays(-90), project.Id);
        var settled = await SeedInvoiceAsync(999m, today.AddDays(-5), project.Id);

        await PayAsync(late45.InvoiceId, 100m);
        await PayAsync(settled.InvoiceId, 999m);

        var debts = await AsSuperAdminAsync(scope => scope.Send(new GetReceivablesQuery { ProjectId = project.Id }));

        Assert.Equal(100m + 200m + 200m + 400m, debts.OpenTotal);
        Assert.Equal(200m + 200m + 400m, debts.OverdueTotal);
        Assert.Equal(4, debts.Rows.Count);

        decimal Bucket(string age) => debts.Buckets.Single(b => b.Age == age).Amount;

        Assert.Equal(100m, Bucket(DebtAge.NotDue));
        Assert.Equal(200m, Bucket(DebtAge.Days1To30));
        Assert.Equal(200m, Bucket(DebtAge.Days31To60));
        Assert.Equal(400m, Bucket(DebtAge.Over60));

        // The most overdue is listed first.
        Assert.Equal(late90.InvoiceId, debts.Rows.First().Id);
        Assert.Equal(notDue.InvoiceId, debts.Rows.Last().Id);
        Assert.Equal(100m, debts.Rows.Single(r => r.Id == late45.InvoiceId).PaidAmount);
    }

    [Fact]
    public async Task An_unpaid_income_of_the_book_is_owed_and_stops_being_owed_when_marked_paid()
    {
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));

        var entry = await AsSuperAdminAsync(async scope =>
        {
            var collection = await scope.Db.BookCategories
                .Where(c => c.SystemKey == BookRules.Keys.Collection).Select(c => c.Id).SingleAsync();

            return await scope.Send(new CreateBookEntryCommand
            {
                Kind = BookEntryKind.Income,
                Amount = 800m,
                OccurredOn = new DateOnly(2026, 3, 1),
                CategoryId = collection,
                ProjectId = project.Id,
                Description = "Radovi " + Unique(),
            });
        });

        var before = await AsSuperAdminAsync(scope => scope.Send(new GetReceivablesQuery { ProjectId = project.Id }));
        Assert.Equal(800m, Assert.Single(before.Rows).OpenAmount);

        await AsSuperAdminAsync(scope => scope.Send(new MarkBookEntryPaidCommand
        {
            Id = entry.Id,
            PaidOn = new DateOnly(2026, 3, 15),
            Method = BookPaymentMethod.Bank,
        }));

        var after = await AsSuperAdminAsync(scope => scope.Send(new GetReceivablesQuery { ProjectId = project.Id }));
        Assert.Empty(after.Rows);
    }

    [Fact]
    public async Task Unpaid_costs_of_the_book_are_what_the_firm_owes()
    {
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await AsSuperAdminAsync(async scope =>
        {
            var fixedCosts = await scope.Db.BookCategories
                .Where(c => c.SystemKey == BookRules.Keys.FixedCosts).Select(c => c.Id).SingleAsync();

            await scope.Send(new CreateBookEntryCommand
            {
                Kind = BookEntryKind.Cost,
                Amount = 120m,
                OccurredOn = new DateOnly(2026, 3, 1),
                DueDate = today.AddDays(-3),
                CategoryId = fixedCosts,
                ProjectId = project.Id,
                Counterparty = "Dobavljac",
            });

            await scope.Send(new CreateBookEntryCommand
            {
                Kind = BookEntryKind.Cost,
                Amount = 80m,
                OccurredOn = new DateOnly(2026, 3, 2),
                PaidOn = new DateOnly(2026, 3, 2),
                CategoryId = fixedCosts,
                ProjectId = project.Id,
            });

            return 0;
        });

        var owed = await AsSuperAdminAsync(scope => scope.Send(new GetPayablesQuery { ProjectId = project.Id }));

        Assert.Equal(120m, owed.OpenTotal);
        Assert.Equal(120m, owed.OverdueTotal);
        Assert.Equal(DebtAge.Days1To30, Assert.Single(owed.Rows).Age);
    }

    [Fact]
    public async Task Reading_what_is_owed_needs_the_finance_grant()
    {
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(async scope =>
        {
            var user = await TestData.SeedUserAsync(scope, UserRole.Admin);
            scope.CurrentUser.SignInAs(user.Id, user.Role, null, user.Email);

            return await scope.Send(new GetReceivablesQuery());
        }));
    }

    [Fact]
    public async Task The_clients_hourly_price_of_a_site_is_offered_in_the_settlement_and_can_be_typed_over()
    {
        var project = await InScope(async scope =>
        {
            var created = await TestData.SeedProjectAsync(scope);
            created.ClientHourlyRate = 41.5m;
            await scope.Db.SaveChangesAsync();

            return created;
        });

        var withoutRate = await InScope(scope => TestData.SeedProjectAsync(scope));

        var (ledger, sectionWithRate, rowWithRate, sectionWithout, rowWithout) =
            await AsSuperAdminAsync(async scope =>
            {
                var ledger = await scope.Send(new CreateLedgerCommand
                {
                    Name = $"Obracun {Unique()}",
                    Year = 2026,
                    Month = 9,
                    Template = LedgerTemplates.Payroll,
                });

                var s1 = await scope.Send(new AddLedgerSectionCommand
                {
                    LedgerId = ledger.Id, Name = "Sa cijenom", ProjectId = project.Id,
                });
                var r1 = await scope.Send(new AddLedgerRowCommand { SectionId = s1.Id, Label = "Radnik 1" });

                var s2 = await scope.Send(new AddLedgerSectionCommand
                {
                    LedgerId = ledger.Id, Name = "Bez cijene", ProjectId = withoutRate.Id,
                });
                var r2 = await scope.Send(new AddLedgerRowCommand { SectionId = s2.Id, Label = "Radnik 2" });

                return (ledger, s1.Id, r1.Id, s2.Id, r2.Id);
            });

        var rateColumn = ledger.Columns.Single(c => c.SystemKey == LedgerTemplates.Keys.ClientRate).Id;

        var withRate = await AsSuperAdminAsync(scope =>
            scope.Send(new GetLedgerSectionRowsQuery(ledger.Id, sectionWithRate)));
        var without = await AsSuperAdminAsync(scope =>
            scope.Send(new GetLedgerSectionRowsQuery(ledger.Id, sectionWithout)));

        var offered = withRate.Rows.Single(r => r.Id == rowWithRate).Cells.Single(c => c.ColumnId == rateColumn);
        Assert.True(offered.IsComputed);
        Assert.Equal(41.5m, LedgerCellMath.ParseNumeric(offered.Value));

        // A site with no price set offers nothing: the cell reads zero, and a price typed there is plain input.
        var none = without.Rows.Single(r => r.Id == rowWithout).Cells.SingleOrDefault(c => c.ColumnId == rateColumn);
        Assert.Equal(0m, LedgerCellMath.ParseNumeric(none?.Value));

        await AsSuperAdminAsync(async scope =>
        {
            await scope.Send(new Construction.Application.Features.Ledgers.Commands.SetLedgerCell.SetLedgerCellCommand
            {
                RowId = rowWithRate, ColumnId = rateColumn, Value = "45",
            });
            await scope.Send(new Construction.Application.Features.Ledgers.Commands.SetLedgerCell.SetLedgerCellCommand
            {
                RowId = rowWithout, ColumnId = rateColumn, Value = "45",
            });

            return 0;
        });

        var typedWith = (await AsSuperAdminAsync(scope =>
                scope.Send(new GetLedgerSectionRowsQuery(ledger.Id, sectionWithRate))))
            .Rows.Single(r => r.Id == rowWithRate).Cells.Single(c => c.ColumnId == rateColumn);
        var typedWithout = (await AsSuperAdminAsync(scope =>
                scope.Send(new GetLedgerSectionRowsQuery(ledger.Id, sectionWithout))))
            .Rows.Single(r => r.Id == rowWithout).Cells.Single(c => c.ColumnId == rateColumn);

        Assert.True(typedWith.IsOverride);
        Assert.False(typedWithout.IsOverride);
        Assert.Equal(45m, LedgerCellMath.ParseNumeric(typedWithout.Value));
    }
}
