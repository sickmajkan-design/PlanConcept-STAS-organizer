using Npgsql;

namespace Construction.IntegrationTests;

/// <summary>
/// A throwaway PostgreSQL database: created for one fixture, dropped after it.
/// </summary>
/// <remarks>
/// PostgreSQL rather than an in-memory provider on purpose. The behaviour
/// worth testing is provider-specific — filtered unique indexes that let a
/// soft-deleted identifier be reused, check constraints, exclusion constraints
/// over date ranges, and <c>ExecuteUpdate</c>, which the in-memory provider
/// does not implement at all. An in-memory suite would report green while
/// production broke.
///
/// Point it at a server with <c>ConstructionTests__Postgres</c>, e.g.
/// <c>Host=localhost;Port=5432;Username=postgres;Password=postgres</c>.
/// </remarks>
public sealed class TestDatabase
{
    public const string EnvironmentVariable = "ConstructionTests__Postgres";

    private const string DefaultAdminConnectionString =
        "Host=localhost;Port=5432;Username=postgres;Password=postgres";

    private readonly string _name = $"construction_test_{Guid.NewGuid():N}";

    public string AdminConnectionString { get; } =
        Environment.GetEnvironmentVariable(EnvironmentVariable) ?? DefaultAdminConnectionString;

    /// <summary>Points at this run's own database, not at the server's default.</summary>
    public string ConnectionString =>
        new NpgsqlConnectionStringBuilder(AdminConnectionString) { Database = _name }
            .ConnectionString;

    public async Task CreateAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(MaintenanceConnectionString());
            await connection.OpenAsync();

            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{_name}\"", connection);
            await create.ExecuteNonQueryAsync();
        }
        catch (NpgsqlException exception)
        {
            throw new InvalidOperationException(
                "These integration tests need a reachable PostgreSQL server. Start one " +
                "(docker compose up postgres) or point the tests at another server with " +
                $"the {EnvironmentVariable} environment variable. Tried: " +
                $"{new NpgsqlConnectionStringBuilder(AdminConnectionString) { Password = "***" }.ConnectionString}",
                exception);
        }
    }

    public async Task DropAsync()
    {
        // Pooled connections would keep the database busy and block the drop.
        NpgsqlConnection.ClearAllPools();

        await using var connection = new NpgsqlConnection(MaintenanceConnectionString());
        await connection.OpenAsync();

        await using var drop = new NpgsqlCommand(
            $"DROP DATABASE IF EXISTS \"{_name}\" WITH (FORCE)", connection);
        await drop.ExecuteNonQueryAsync();
    }

    private string MaintenanceConnectionString() =>
        new NpgsqlConnectionStringBuilder(AdminConnectionString) { Database = "postgres" }
            .ConnectionString;
}
