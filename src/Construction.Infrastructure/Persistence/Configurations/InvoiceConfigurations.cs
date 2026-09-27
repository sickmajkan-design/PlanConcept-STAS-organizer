using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class CustomerCompanyConfiguration : IEntityTypeConfiguration<CustomerCompany>
{
    public void Configure(EntityTypeBuilder<CustomerCompany> builder)
    {
        builder.ToTable("customer_companies");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Address).HasMaxLength(500);

        builder.HasOne(c => c.Customer)
            .WithMany()
            .HasForeignKey(c => c.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.CustomerId, c.Name }).IsUnique();
    }
}

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Number).IsRequired().HasMaxLength(64);
        builder.Property(i => i.Description).HasMaxLength(1000);
        builder.Property(i => i.CancelReason).HasMaxLength(500);
        builder.Property(i => i.Amount).HasPrecision(12, 2);

        builder.HasOne(i => i.Project)
            .WithMany()
            .HasForeignKey(i => i.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.CreatedByUser)
            .WithMany()
            .HasForeignKey(i => i.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(i => i.Shares)
            .WithOne(s => s.Invoice)
            .HasForeignKey(s => s.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable(t => t.HasCheckConstraint("ck_invoices_amount_not_zero", "\"Amount\" <> 0"));
        builder.ToTable(t => t.HasCheckConstraint(
            "ck_invoices_payroll_month", "\"PayrollMonth\" BETWEEN 1 AND 12"));

        // An invoice number is issued once. A cancelled invoice keeps its number, so it is never reused.
        builder.HasIndex(i => i.Number).IsUnique();
        builder.HasIndex(i => new { i.ProjectId, i.PayrollYear, i.PayrollMonth });

        builder.Property<uint>("Version").IsRowVersion();
    }
}

public class InvoiceShareConfiguration : IEntityTypeConfiguration<InvoiceShare>
{
    public void Configure(EntityTypeBuilder<InvoiceShare> builder)
    {
        builder.ToTable("invoice_shares");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Amount).HasPrecision(12, 2);

        builder.HasOne(s => s.CustomerCompany)
            .WithMany()
            .HasForeignKey(s => s.CustomerCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t => t.HasCheckConstraint("ck_invoice_shares_amount_not_zero", "\"Amount\" <> 0"));

        builder.HasIndex(s => s.CustomerCompanyId);
    }
}
