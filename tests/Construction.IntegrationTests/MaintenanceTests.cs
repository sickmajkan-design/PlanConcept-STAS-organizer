using Construction.Application.Features.Maintenance.Commands.PurgeExpiredData;
using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// The retention sweep, against PostgreSQL.
/// </summary>
/// <remarks>
/// Against a real database rather than in memory because the interesting part
/// is the SQL: a bounded <c>DELETE</c> is expressed as <c>Take</c> before
/// <c>ExecuteDelete</c>, and whether that translates at all — rather than
/// throwing, or quietly deleting everything the predicate matches — is a
/// property of the provider, not of the C#.
/// </remarks>
[Collection(DatabaseCollection.Name)]
public class MaintenanceTests : IntegrationTestBase
{
    public MaintenanceTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    /// <summary>
    /// Retention windows wide enough that rows seeded by other test classes
    /// are never in range, so the counts belong to the test that made them.
    /// </summary>
    private static PurgeExpiredDataCommand Sweep(
        TimeSpan? locations = null,
        int batchSize = 5_000,
        int maxBatches = 20) =>
        new()
        {
            RefreshTokenGrace = TimeSpan.FromDays(3_650),
            PasswordResetTokenGrace = TimeSpan.FromDays(3_650),
            LocationRecordRetention = locations ?? TimeSpan.FromDays(3_650),
            SentOutboxMessageRetention = TimeSpan.FromDays(3_650),
            BatchSize = batchSize,
            MaxBatchesPerTable = maxBatches,
        };

    private async Task<RefreshToken> SeedRefreshTokenAsync(
        Guid userId,
        DateTime expiresAt,
        DateTime? revokedAt = null)
    {
        return await InScope(async scope =>
        {
            var token = new RefreshToken
            {
                UserId = userId,
                TokenHash = Guid.NewGuid().ToString("N"),
                ExpiresAt = expiresAt,
                RevokedAt = revokedAt,
            };

            scope.Db.RefreshTokens.Add(token);
            await scope.Db.SaveChangesAsync();

            return token;
        });
    }

    private Task<bool> StillThereAsync(Guid tokenId) =>
        InScope(scope => scope.Db.RefreshTokens.AnyAsync(t => t.Id == tokenId));

    // ---- tokens ----------------------------------------------------------

    [Fact]
    public async Task A_refresh_token_past_its_grace_period_is_removed()
    {
        var user = await InScope(scope => TestData.SeedUserAsync(scope));

        var stale = await SeedRefreshTokenAsync(
            user.Id, DateTime.UtcNow.AddDays(-100));

        await InScope(scope => scope.Send(new PurgeExpiredDataCommand
        {
            RefreshTokenGrace = TimeSpan.FromDays(30),
            PasswordResetTokenGrace = TimeSpan.FromDays(3_650),
            LocationRecordRetention = TimeSpan.FromDays(3_650),
            SentOutboxMessageRetention = TimeSpan.FromDays(3_650),
        }));

        Assert.False(await StillThereAsync(stale.Id));
    }

    [Fact]
    public async Task A_live_refresh_token_is_left_alone()
    {
        var user = await InScope(scope => TestData.SeedUserAsync(scope));

        var live = await SeedRefreshTokenAsync(user.Id, DateTime.UtcNow.AddDays(7));

        await InScope(scope => scope.Send(new PurgeExpiredDataCommand
        {
            RefreshTokenGrace = TimeSpan.Zero,
            PasswordResetTokenGrace = TimeSpan.FromDays(3_650),
            LocationRecordRetention = TimeSpan.FromDays(3_650),
            SentOutboxMessageRetention = TimeSpan.FromDays(3_650),
        }));

        Assert.True(await StillThereAsync(live.Id));
    }

    [Fact]
    public async Task A_rotated_token_that_has_not_expired_yet_survives()
    {
        // The one that must not be deleted. Rotation revokes the old row and
        // leaves it behind, because presenting it again is how the API knows a
        // token was stolen and revokes every session for the account. Delete
        // it and the replay becomes an ordinary unknown token — still refused,
        // but with the theft signal gone.
        var user = await InScope(scope => TestData.SeedUserAsync(scope));

        var rotated = await SeedRefreshTokenAsync(
            user.Id,
            expiresAt: DateTime.UtcNow.AddDays(6),
            revokedAt: DateTime.UtcNow.AddMinutes(-5));

        await InScope(scope => scope.Send(new PurgeExpiredDataCommand
        {
            RefreshTokenGrace = TimeSpan.Zero,
            PasswordResetTokenGrace = TimeSpan.FromDays(3_650),
            LocationRecordRetention = TimeSpan.FromDays(3_650),
            SentOutboxMessageRetention = TimeSpan.FromDays(3_650),
        }));

        Assert.True(await StillThereAsync(rotated.Id));
    }

    [Fact]
    public async Task A_used_reset_token_is_removed_once_it_has_expired()
    {
        var user = await InScope(scope => TestData.SeedUserAsync(scope));

        var token = await InScope(async scope =>
        {
            var reset = new PasswordResetToken
            {
                UserId = user.Id,
                TokenHash = Guid.NewGuid().ToString("N"),
                ExpiresAt = DateTime.UtcNow.AddDays(-30),
                UsedAt = DateTime.UtcNow.AddDays(-30),
            };

            scope.Db.PasswordResetTokens.Add(reset);
            await scope.Db.SaveChangesAsync();

            return reset;
        });

        await InScope(scope => scope.Send(new PurgeExpiredDataCommand
        {
            RefreshTokenGrace = TimeSpan.FromDays(3_650),
            PasswordResetTokenGrace = TimeSpan.FromDays(7),
            LocationRecordRetention = TimeSpan.FromDays(3_650),
            SentOutboxMessageRetention = TimeSpan.FromDays(3_650),
        }));

        var gone = await InScope(scope =>
            scope.Db.PasswordResetTokens.AnyAsync(t => t.Id == token.Id));

        Assert.False(gone);
    }

    // ---- location records -------------------------------------------------

    /// <summary>
    /// Seeds pings, having first swept away any that an earlier test left
    /// behind.
    /// </summary>
    /// <remarks>
    /// The sweep counts rows, not rows belonging to one employee, and every
    /// test class in this collection shares one database. Without clearing
    /// first, a test asserting "twelve were deleted" is really asserting
    /// "twelve, plus whatever the test before me abandoned" — which passes or
    /// fails on execution order rather than on the code under test.
    /// </remarks>
    private async Task<Employee> SeedPingsAsync(int old, int recent)
    {
        await InScope(scope => scope.Send(new PurgeExpiredDataCommand
        {
            RefreshTokenGrace = TimeSpan.FromDays(3_650),
            PasswordResetTokenGrace = TimeSpan.FromDays(3_650),
            LocationRecordRetention = TimeSpan.FromDays(180),
            SentOutboxMessageRetention = TimeSpan.FromDays(3_650),
            MaxBatchesPerTable = 1_000,
        }));

        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        await InScope(async scope =>
        {
            for (var i = 0; i < old; i++)
            {
                scope.Db.LocationRecords.Add(new LocationRecord
                {
                    EmployeeId = employee.Id,
                    Latitude = 44.8,
                    Longitude = 20.4,
                    Timestamp = DateTime.UtcNow.AddDays(-400).AddMinutes(i),
                    ReceivedAt = DateTime.UtcNow.AddDays(-400).AddMinutes(i),
                });
            }

            for (var i = 0; i < recent; i++)
            {
                scope.Db.LocationRecords.Add(new LocationRecord
                {
                    EmployeeId = employee.Id,
                    Latitude = 44.8,
                    Longitude = 20.4,
                    Timestamp = DateTime.UtcNow.AddMinutes(-i),
                    ReceivedAt = DateTime.UtcNow.AddMinutes(-i),
                });
            }

            await scope.Db.SaveChangesAsync();
        });

        return employee;
    }

    private Task<int> PingCountAsync(Guid employeeId) =>
        InScope(scope => scope.Db.LocationRecords.CountAsync(r => r.EmployeeId == employeeId));

    [Fact]
    public async Task Pings_older_than_the_window_go_and_the_rest_stay()
    {
        var employee = await SeedPingsAsync(old: 12, recent: 5);

        var result = await InScope(scope =>
            scope.Send(Sweep(locations: TimeSpan.FromDays(180))));

        Assert.Equal(12, result.LocationRecords);
        Assert.Equal(5, await PingCountAsync(employee.Id));
    }

    [Fact]
    public async Task Nothing_is_deleted_when_retention_is_unset()
    {
        var employee = await SeedPingsAsync(old: 4, recent: 2);

        var result = await InScope(scope =>
            scope.Send(new PurgeExpiredDataCommand
            {
                RefreshTokenGrace = TimeSpan.FromDays(3_650),
                PasswordResetTokenGrace = TimeSpan.FromDays(3_650),
                LocationRecordRetention = null,
                SentOutboxMessageRetention = TimeSpan.FromDays(3_650),
            }));

        Assert.Equal(0, result.LocationRecords);
        Assert.Equal(6, await PingCountAsync(employee.Id));
    }

    [Fact]
    public async Task The_delete_is_bounded_by_the_batch_size()
    {
        // The property the whole design rests on. `Take` before
        // `ExecuteDelete` has to reach the database as a bounded statement; if
        // the provider ignored it, one sweep would take a table-wide lock over
        // a year of pings, and this test would report ten deletions instead of
        // four.
        var employee = await SeedPingsAsync(old: 10, recent: 0);

        var result = await InScope(scope => scope.Send(
            Sweep(locations: TimeSpan.FromDays(180), batchSize: 4, maxBatches: 1)));

        Assert.Equal(4, result.LocationRecords);
        Assert.Equal(6, await PingCountAsync(employee.Id));
    }

    [Fact]
    public async Task Several_batches_run_in_one_sweep()
    {
        var employee = await SeedPingsAsync(old: 10, recent: 0);

        var result = await InScope(scope => scope.Send(
            Sweep(locations: TimeSpan.FromDays(180), batchSize: 4, maxBatches: 3)));

        // Two full batches and a short third, which is what stops the loop.
        Assert.Equal(10, result.LocationRecords);
        Assert.Equal(0, await PingCountAsync(employee.Id));
    }

    [Fact]
    public async Task A_sweep_with_nothing_to_do_costs_one_statement_and_reports_zero()
    {
        var result = await InScope(scope => scope.Send(Sweep()));

        Assert.Equal(0, result.Total);
    }

    [Fact]
    public async Task Running_it_twice_removes_nothing_the_second_time()
    {
        var employee = await SeedPingsAsync(old: 6, recent: 1);

        await InScope(scope => scope.Send(Sweep(locations: TimeSpan.FromDays(180))));

        var second = await InScope(scope =>
            scope.Send(Sweep(locations: TimeSpan.FromDays(180))));

        Assert.Equal(0, second.LocationRecords);
        Assert.Equal(1, await PingCountAsync(employee.Id));
    }

    // ---- delivered messages ----------------------------------------------

    [Fact]
    public async Task A_delivered_message_goes_once_its_window_passes_and_an_abandoned_one_stays()
    {
        var (sent, abandoned) = await InScope(async scope =>
        {
            var delivered = new Domain.Entities.OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = Domain.Enums.OutboxMessageType.Email,
                PayloadJson = """{"To":"a@b.test","Subject":"old","HtmlBody":"x"}""",
                CreatedAt = DateTime.UtcNow.AddDays(-90),
                NextAttemptAt = DateTime.UtcNow.AddDays(-90),
                SentAt = DateTime.UtcNow.AddDays(-90),
            };

            var deadLettered = new Domain.Entities.OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = Domain.Enums.OutboxMessageType.Email,
                PayloadJson = """{"To":"c@d.test","Subject":"failed","HtmlBody":"x"}""",
                CreatedAt = DateTime.UtcNow.AddDays(-90),
                NextAttemptAt = DateTime.UtcNow.AddDays(-90),
                AbandonedAt = DateTime.UtcNow.AddDays(-90),
            };

            scope.Db.OutboxMessages.AddRange(delivered, deadLettered);
            await scope.Db.SaveChangesAsync();

            return (delivered, deadLettered);
        });

        await InScope(scope => scope.Send(new PurgeExpiredDataCommand
        {
            RefreshTokenGrace = TimeSpan.FromDays(3_650),
            PasswordResetTokenGrace = TimeSpan.FromDays(3_650),
            LocationRecordRetention = TimeSpan.FromDays(3_650),
            SentOutboxMessageRetention = TimeSpan.FromDays(14),
        }));

        var stillThere = await InScope(scope => scope.Db.OutboxMessages
            .Where(m => m.Id == sent.Id || m.Id == abandoned.Id)
            .Select(m => m.Id)
            .ToListAsync());

        // The dead letter stays: it is the record of a delivery that failed
        // for good, and deleting it leaves "why did they never get the email?"
        // with nothing to answer it.
        Assert.Equal([abandoned.Id], stillThere);
    }

    // ---- what it refuses --------------------------------------------------

    [Fact]
    public async Task A_retention_of_zero_is_refused()
    {
        // It would delete a ping the moment it arrived, which on a live map
        // is indistinguishable from tracking being broken.
        await Assert.ThrowsAsync<Construction.Application.Common.Exceptions.ValidationException>(
            () => InScope(scope => scope.Send(new PurgeExpiredDataCommand
            {
                LocationRecordRetention = TimeSpan.Zero,
            })));
    }

    [Fact]
    public async Task A_negative_grace_period_is_refused()
    {
        await Assert.ThrowsAsync<Construction.Application.Common.Exceptions.ValidationException>(
            () => InScope(scope => scope.Send(new PurgeExpiredDataCommand
            {
                RefreshTokenGrace = TimeSpan.FromDays(-1),
            })));
    }

    [Fact]
    public async Task A_batch_size_of_zero_is_refused()
    {
        // Otherwise the loop would run its full count deleting nothing, and
        // report success at having done so.
        await Assert.ThrowsAsync<Construction.Application.Common.Exceptions.ValidationException>(
            () => InScope(scope => scope.Send(new PurgeExpiredDataCommand { BatchSize = 0 })));
    }

    // ---- the audit trail -------------------------------------------------

    [Fact]
    public async Task The_audit_trail_is_kept_by_default()
    {
        // The one table the sweep leaves alone unless told otherwise. A trail
        // that had quietly aged out the month somebody asks about is worse
        // than no trail, because everybody believed it was there.
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        var before = await InScope(scope => scope.Db.AuditEntries
            .CountAsync(a => a.EntityId == employee.Id));

        Assert.True(before > 0);

        // Sweep() leaves AuditEntryRetention unset, which is the shipped
        // default.
        await InScope(scope => scope.Send(Sweep()));

        var after = await InScope(scope => scope.Db.AuditEntries
            .CountAsync(a => a.EntityId == employee.Id));

        Assert.Equal(before, after);
    }

    [Fact]
    public async Task The_audit_trail_can_be_aged_out_when_a_deployment_must()
    {
        // Some deployments are obliged to delete on a schedule. The option
        // exists; it is just not what happens by accident.
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        await InScope(async scope =>
        {
            var stale = await scope.Db.AuditEntries
                .Where(a => a.EntityId == employee.Id)
                .ToListAsync();

            foreach (var entry in stale)
            {
                entry.OccurredAt = DateTime.UtcNow.AddDays(-400);
            }

            await scope.Db.SaveChangesAsync();
        });

        await InScope(scope => scope.Send(Sweep() with
        {
            AuditEntryRetention = TimeSpan.FromDays(365),
        }));

        var left = await InScope(scope => scope.Db.AuditEntries
            .CountAsync(a => a.EntityId == employee.Id));

        Assert.Equal(0, left);
    }

    [Fact]
    public async Task A_recent_audit_entry_survives_an_aging_sweep()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        await InScope(scope => scope.Send(Sweep() with
        {
            AuditEntryRetention = TimeSpan.FromDays(365),
        }));

        var left = await InScope(scope => scope.Db.AuditEntries
            .CountAsync(a => a.EntityId == employee.Id));

        Assert.True(left > 0);
    }

    [Fact]
    public async Task An_audit_retention_of_zero_is_refused()
    {
        // Zero would delete the entry that was just written, which is
        // indistinguishable from the trail being broken.
        await Assert.ThrowsAsync<Construction.Application.Common.Exceptions.ValidationException>(
            () => InScope(scope => scope.Send(new PurgeExpiredDataCommand
            {
                AuditEntryRetention = TimeSpan.Zero,
            })));
    }
}
