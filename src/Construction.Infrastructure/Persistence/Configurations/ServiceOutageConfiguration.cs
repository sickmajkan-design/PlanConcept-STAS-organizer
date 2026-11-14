using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class ServiceOutageConfiguration : IEntityTypeConfiguration<ServiceOutage>
{
    public void Configure(EntityTypeBuilder<ServiceOutage> builder)
    {
        builder.ToTable("service_outages");
        builder.HasKey(o => o.Id);
        builder.HasIndex(o => o.StartedAt);
    }
}

public class ServiceHeartbeatConfiguration : IEntityTypeConfiguration<ServiceHeartbeat>
{
    public void Configure(EntityTypeBuilder<ServiceHeartbeat> builder)
    {
        builder.ToTable("service_heartbeats");
        builder.HasKey(h => h.Id);
    }
}

public class ServerAlertSettingConfiguration : IEntityTypeConfiguration<ServerAlertSetting>
{
    public void Configure(EntityTypeBuilder<ServerAlertSetting> builder)
    {
        builder.ToTable("server_alert_settings");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Key).HasMaxLength(40).IsRequired();
        builder.HasIndex(s => s.Key).IsUnique();
    }
}

public class ServerErrorConfiguration : IEntityTypeConfiguration<ServerError>
{
    public void Configure(EntityTypeBuilder<ServerError> builder)
    {
        builder.ToTable("server_errors");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Key).HasMaxLength(64).IsRequired();
        builder.Property(e => e.ExceptionType).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Location).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Route).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Version).HasMaxLength(40).IsRequired();
        builder.HasIndex(e => new { e.Key, e.Day }).IsUnique();
        builder.HasIndex(e => e.Day);
    }
}
