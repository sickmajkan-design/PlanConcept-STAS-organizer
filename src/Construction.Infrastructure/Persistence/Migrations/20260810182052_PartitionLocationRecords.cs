using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations;

/// <summary>
/// Turns <c>location_records</c> into a table partitioned by month.
/// </summary>
/// <remarks>
/// <para>
/// PostgreSQL cannot convert a table into a partitioned one in place, so this
/// builds the new shape beside the old, moves the rows, and swaps. On an
/// existing installation that is a full copy of the table and the whole thing
/// runs in one transaction — which is the argument for doing it now rather
/// than at the size that makes it necessary.
/// </para>
/// <para>
/// The primary key gains <c>Timestamp</c>: PostgreSQL will not enforce
/// uniqueness that does not include the partition key. Ids still come from one
/// identity sequence shared across every partition, so they stay unique on
/// their own; the composite is what lets the database say so.
/// </para>
/// <para>
/// A DEFAULT partition is created deliberately. Without one, a ping for a
/// month nobody made a partition for is rejected outright — a maintenance
/// oversight turned into lost GPS data on the write path. With one, the row
/// lands somewhere valid and is still readable and still purged; what it loses
/// is only the ability to be dropped as part of a month.
/// </para>
/// </remarks>
public partial class PartitionLocationRecords : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE location_records RENAME TO location_records_unpartitioned_old;

            ALTER INDEX "PK_location_records" RENAME TO "PK_location_records_old";
            ALTER INDEX "IX_location_records_EmployeeId_Timestamp"
                RENAME TO "IX_location_records_old_EmployeeId_Timestamp";
            ALTER INDEX "IX_location_records_Timestamp"
                RENAME TO "IX_location_records_old_Timestamp";

            CREATE TABLE location_records (
                "Id" bigint GENERATED ALWAYS AS IDENTITY,
                "EmployeeId" uuid NOT NULL,
                "Latitude" double precision NOT NULL,
                "Longitude" double precision NOT NULL,
                "Accuracy" double precision,
                "Timestamp" timestamp with time zone NOT NULL,
                "ReceivedAt" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_location_records" PRIMARY KEY ("Id", "Timestamp"),
                CONSTRAINT "FK_location_records_employees_EmployeeId"
                    FOREIGN KEY ("EmployeeId") REFERENCES employees ("Id") ON DELETE CASCADE
            ) PARTITION BY RANGE ("Timestamp");

            -- The net. Everything that has no month of its own lands here
            -- rather than being refused.
            CREATE TABLE location_records_unpartitioned
                PARTITION OF location_records DEFAULT;

            -- Indexes on the parent; PostgreSQL creates and maintains the
            -- matching index on every partition, including future ones.
            CREATE INDEX "IX_location_records_EmployeeId_Timestamp"
                ON location_records ("EmployeeId", "Timestamp" DESC);

            CREATE INDEX "IX_location_records_Timestamp"
                ON location_records ("Timestamp");
            """);

        // A partition per month the old table actually covers, so existing
        // history is droppable by month straight away rather than sitting in
        // DEFAULT for ever.
        migrationBuilder.Sql("""
            DO $$
            DECLARE
                month_start date;
                stop        date;
                partition   text;
            BEGIN
                SELECT date_trunc('month', min("Timestamp"))::date,
                       date_trunc('month', max("Timestamp"))::date
                INTO month_start, stop
                FROM location_records_unpartitioned_old;

                IF month_start IS NULL THEN
                    RETURN;
                END IF;

                WHILE month_start <= stop LOOP
                    partition := 'location_records_' || to_char(month_start, 'YYYY_MM');

                    EXECUTE format(
                        'CREATE TABLE IF NOT EXISTS %I PARTITION OF location_records '
                        || 'FOR VALUES FROM (%L) TO (%L)',
                        partition,
                        month_start,
                        month_start + interval '1 month');

                    month_start := month_start + interval '1 month';
                END LOOP;
            END $$;
            """);

        migrationBuilder.Sql("""
            INSERT INTO location_records
                ("Id", "EmployeeId", "Latitude", "Longitude", "Accuracy", "Timestamp", "ReceivedAt")
            OVERRIDING SYSTEM VALUE
            SELECT "Id", "EmployeeId", "Latitude", "Longitude", "Accuracy", "Timestamp", "ReceivedAt"
            FROM location_records_unpartitioned_old;

            -- Continue the sequence rather than restarting it: ids already
            -- handed out must never come round again.
            SELECT setval(
                pg_get_serial_sequence('location_records', 'Id'),
                GREATEST(COALESCE((SELECT max("Id") FROM location_records), 0), 1));

            DROP TABLE location_records_unpartitioned_old;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Back to one plain table. Partitions go with it, so this is only
        // reversible in the sense that the data survives — the month boundaries do
        // not, and re-applying Up rebuilds them from the timestamps anyway.
        migrationBuilder.Sql("""
            CREATE TABLE location_records_flat (
                "Id" bigint GENERATED ALWAYS AS IDENTITY,
                "EmployeeId" uuid NOT NULL,
                "Latitude" double precision NOT NULL,
                "Longitude" double precision NOT NULL,
                "Accuracy" double precision,
                "Timestamp" timestamp with time zone NOT NULL,
                "ReceivedAt" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_location_records_flat" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_location_records_flat_employees_EmployeeId"
                    FOREIGN KEY ("EmployeeId") REFERENCES employees ("Id") ON DELETE CASCADE
            );

            INSERT INTO location_records_flat
                ("Id", "EmployeeId", "Latitude", "Longitude", "Accuracy", "Timestamp", "ReceivedAt")
            OVERRIDING SYSTEM VALUE
            SELECT "Id", "EmployeeId", "Latitude", "Longitude", "Accuracy", "Timestamp", "ReceivedAt"
            FROM location_records;

            DROP TABLE location_records;

            ALTER TABLE location_records_flat RENAME TO location_records;
            ALTER INDEX "PK_location_records_flat" RENAME TO "PK_location_records";
            ALTER TABLE location_records
                RENAME CONSTRAINT "FK_location_records_flat_employees_EmployeeId"
                TO "FK_location_records_employees_EmployeeId";

            SELECT setval(
                pg_get_serial_sequence('location_records', 'Id'),
                GREATEST(COALESCE((SELECT max("Id") FROM location_records), 0), 1));

            CREATE INDEX "IX_location_records_EmployeeId_Timestamp"
                ON location_records ("EmployeeId", "Timestamp" DESC);

            CREATE INDEX "IX_location_records_Timestamp"
                ON location_records ("Timestamp");
            """);
    }
}
