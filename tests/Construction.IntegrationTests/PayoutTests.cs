using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Ledgers.Commands;
using Construction.Application.Features.Ledgers.Commands.CreateLedger;
using Construction.Application.Features.Ledgers.Commands.SetLedgerCell;
using Construction.Application.Features.Ledgers.Models;
using Construction.Application.Features.Payouts;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// What workers are paid out for a month, against what each pay group earned in the settlement. Each test
/// uses a month of its own so rows from other tests cannot leak into the figures.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class PayoutTests : IntegrationTestBase
{
    public PayoutTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static int nextYear = 2000;

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

    /// <summary>
    /// A month whose settlement has a worker row earning 800 and a subcontractor row earning 300
    /// (rate × hours, no deductions).
    /// </summary>
    private async Task<int> SeedSettlementAsync()
    {
        var year = FreshYear();

        await AsSuperAdminAsync(async scope =>
        {
            var ledger = await scope.Send(new CreateLedgerCommand
            {
                Name = $"Obracun {year}",
                Year = year,
                Month = 3,
                Template = LedgerTemplates.Payroll,
            });

            var section = await scope.Send(new AddLedgerSectionCommand { LedgerId = ledger.Id, Name = "Klijent" });

            async Task<Guid> Row(string label, PayoutGroup group, string rate, string hours)
            {
                var row = await scope.Send(new AddLedgerRowCommand
                {
                    SectionId = section.Id, Label = label, PayGroup = group,
                });

                Guid Column(string key) => ledger.Columns.Single(c => c.SystemKey == key).Id;

                await scope.Send(new SetLedgerCellCommand
                {
                    RowId = row.Id, ColumnId = Column(LedgerTemplates.Keys.WorkerRate), Value = rate,
                });
                await scope.Send(new SetLedgerCellCommand
                {
                    RowId = row.Id, ColumnId = Column(LedgerTemplates.Keys.Week(1)), Value = hours,
                });

                return row.Id;
            }

            await Row("Radnik", PayoutGroup.Workers, "20", "40");
            await Row("Kooperant", PayoutGroup.Subcontractors, "15", "20");

            return 0;
        });

        return year;
    }

    private Task<PayoutsDto> ReadAsync(int year) =>
        AsSuperAdminAsync(scope => scope.Send(new GetPayoutsQuery { Year = year, Month = 3 }));

    private static PayoutGroupDto Of(PayoutsDto payouts, string group) =>
        payouts.Groups.Single(g => g.Group == group);

    [Fact]
    public async Task What_a_group_earned_is_read_from_the_pay_group_of_the_settlement_rows()
    {
        var year = await SeedSettlementAsync();

        var payouts = await ReadAsync(year);

        Assert.True(payouts.HasSettlement);
        Assert.Equal(800m, Of(payouts, "Workers").Earned);
        Assert.Equal(300m, Of(payouts, "Subcontractors").Earned);
        Assert.Equal(0m, Of(payouts, "Office").Earned);
        Assert.Equal(1100m, payouts.TotalEarned);
        Assert.Equal(1100m, payouts.TotalRemaining);
    }

    [Fact]
    public async Task Advances_and_paid_final_payments_lower_what_is_left_but_a_planned_one_does_not()
    {
        var year = await SeedSettlementAsync();

        await AsSuperAdminAsync(async scope =>
        {
            await scope.Send(new CreateWagePayoutCommand
            {
                Year = year, Month = 3, Group = PayoutGroup.Workers, Kind = PayoutKind.Advance,
                PayDate = new DateOnly(year, 3, 10), Amount = 200m, Method = BookPaymentMethod.Cash,
            });
            await scope.Send(new CreateWagePayoutCommand
            {
                Year = year, Month = 3, Group = PayoutGroup.Workers, Kind = PayoutKind.Advance,
                PayDate = new DateOnly(year, 3, 20), Amount = 100m, Method = BookPaymentMethod.Bank,
            });
            await scope.Send(new CreateWagePayoutCommand
            {
                Year = year, Month = 3, Group = PayoutGroup.Workers, Kind = PayoutKind.Final,
                PayDate = new DateOnly(year, 4, 5), Amount = 500m,
            });

            return 0;
        });

        var workers = Of(await ReadAsync(year), "Workers");

        Assert.Equal(300m, workers.Advances);
        Assert.Equal(0m, workers.FinalPaid);
        Assert.Equal(500m, workers.FinalPlanned);
        Assert.Equal(500m, workers.Remaining);
        Assert.Equal(0m, workers.CarryOver);
    }

    [Fact]
    public async Task Marking_the_final_payment_paid_settles_the_month()
    {
        var year = await SeedSettlementAsync();

        var planned = await AsSuperAdminAsync(scope => scope.Send(new CreateWagePayoutCommand
        {
            Year = year, Month = 3, Group = PayoutGroup.Subcontractors, Kind = PayoutKind.Final,
            PayDate = new DateOnly(year, 4, 5), Amount = 300m,
        }));

        Assert.Null(planned.PaidOn);

        var paid = await AsSuperAdminAsync(scope => scope.Send(new MarkWagePayoutPaidCommand
        {
            Id = planned.Id, PaidOn = new DateOnly(year, 4, 6), Method = BookPaymentMethod.Bank,
        }));

        Assert.Equal(new DateOnly(year, 4, 6), paid.PaidOn);

        var group = Of(await ReadAsync(year), "Subcontractors");
        Assert.Equal(300m, group.FinalPaid);
        Assert.Equal(0m, group.Remaining);

        await Assert.ThrowsAsync<ConflictException>(() => AsSuperAdminAsync(scope =>
            scope.Send(new MarkWagePayoutPaidCommand { Id = planned.Id })));
    }

    [Fact]
    public async Task Paying_more_than_was_earned_is_carried_over_and_does_not_fail()
    {
        var year = await SeedSettlementAsync();

        await AsSuperAdminAsync(scope => scope.Send(new CreateWagePayoutCommand
        {
            Year = year, Month = 3, Group = PayoutGroup.Subcontractors, Kind = PayoutKind.Advance,
            PayDate = new DateOnly(year, 3, 15), Amount = 450m,
        }));

        var group = Of(await ReadAsync(year), "Subcontractors");

        Assert.Equal(0m, group.Remaining);
        Assert.Equal(150m, group.CarryOver);
    }

    [Fact]
    public async Task A_correction_in_the_settlement_moves_what_is_left_without_a_second_edit()
    {
        var year = await SeedSettlementAsync();

        await AsSuperAdminAsync(scope => scope.Send(new CreateWagePayoutCommand
        {
            Year = year, Month = 3, Group = PayoutGroup.Workers, Kind = PayoutKind.Advance,
            PayDate = new DateOnly(year, 3, 10), Amount = 100m,
        }));

        Assert.Equal(700m, Of(await ReadAsync(year), "Workers").Remaining);

        // The worker's hours are corrected from 40 to 50.
        await AsSuperAdminAsync(async scope =>
        {
            var cell = await scope.Db.LedgerCells
                .Include(c => c.Column)
                .Where(c => c.Row.Section.Ledger.Year == year
                    && c.Column.SystemKey == LedgerTemplates.Keys.Week(1)
                    && c.Row.Label == "Radnik")
                .SingleAsync();

            await scope.Send(new SetLedgerCellCommand { RowId = cell.RowId, ColumnId = cell.ColumnId, Value = "50" });

            return 0;
        });

        Assert.Equal(1000m, Of(await ReadAsync(year), "Workers").Earned);
        Assert.Equal(900m, Of(await ReadAsync(year), "Workers").Remaining);
    }

    [Fact]
    public async Task An_advance_cannot_be_dated_in_the_future()
    {
        var year = await SeedSettlementAsync();

        await Assert.ThrowsAnyAsync<Exception>(() => AsSuperAdminAsync(scope => scope.Send(
            new CreateWagePayoutCommand
            {
                Year = year, Month = 3, Group = PayoutGroup.Workers, Kind = PayoutKind.Advance,
                PayDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(5), Amount = 100m,
            })));

        Assert.Empty((await ReadAsync(year)).Items);
    }

    [Fact]
    public async Task A_payout_can_be_corrected_and_deleted()
    {
        var year = await SeedSettlementAsync();

        var created = await AsSuperAdminAsync(scope => scope.Send(new CreateWagePayoutCommand
        {
            Year = year, Month = 3, Group = PayoutGroup.Workers, Kind = PayoutKind.Advance,
            PayDate = new DateOnly(year, 3, 10), Amount = 100m,
        }));

        var updated = await AsSuperAdminAsync(scope => scope.Send(new UpdateWagePayoutCommand
        {
            Id = created.Id, PayDate = new DateOnly(year, 3, 12), Amount = 150m, Note = "ispravak",
        }));

        Assert.Equal(150m, updated.Amount);
        Assert.Equal(new DateOnly(year, 3, 12), updated.PaidOn);
        Assert.Equal("ispravak", updated.Note);

        await AsSuperAdminAsync(async scope =>
        {
            await scope.Send(new DeleteWagePayoutCommand(created.Id));
            return 0;
        });

        Assert.Empty((await ReadAsync(year)).Items);
    }

    [Theory]
    [InlineData(UserRole.Admin, FinanceAccess.None)]
    [InlineData(UserRole.Admin, FinanceAccess.StatisticsOnly)]
    [InlineData(UserRole.ProjectManager, FinanceAccess.Full)]
    [InlineData(UserRole.Foreman, FinanceAccess.Full)]
    public async Task Payouts_need_an_administrator_with_the_full_finance_grant(UserRole role, FinanceAccess finance)
    {
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(async scope =>
        {
            var user = await TestData.SeedUserAsync(scope, role);
            user.FinanceAccess = finance;
            await scope.Db.SaveChangesAsync();
            scope.CurrentUser.SignInAs(user.Id, role, null, user.Email);

            return await scope.Send(new GetPayoutsQuery { Year = 2010, Month = 3 });
        }));
    }

    [Fact]
    public async Task The_pay_group_of_a_row_is_kept_when_a_month_is_copied()
    {
        var year = await SeedSettlementAsync();

        var copiedGroups = await AsSuperAdminAsync(async scope =>
        {
            var source = await scope.Db.Ledgers.SingleAsync(l => l.Year == year);

            var copy = await scope.Send(new CreateLedgerCommand
            {
                Name = $"Kopija {year}",
                Year = year,
                Month = 4,
                CopyFromLedgerId = source.Id,
            });

            return await scope.Db.LedgerRows
                .Where(r => r.Section.LedgerId == copy.Id)
                .Select(r => r.PayGroup)
                .ToListAsync();
        });

        Assert.Contains(PayoutGroup.Subcontractors, copiedGroups);
        Assert.Contains(PayoutGroup.Workers, copiedGroups);
    }
}
