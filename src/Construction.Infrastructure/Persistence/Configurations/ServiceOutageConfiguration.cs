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
