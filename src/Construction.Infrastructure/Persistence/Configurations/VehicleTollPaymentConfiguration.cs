using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class VehicleTollPaymentConfiguration : IEntityTypeConfiguration<VehicleTollPayment>
{
    public void Configure(EntityTypeBuilder<VehicleTollPayment> builder)
    {
        builder.ToTable("vehicle_toll_payments");

        builder.HasKey(p => p.Id);

        builder.HasOne(p => p.VehicleToll)
            .WithMany(t => t.Payments)
            .HasForeignKey(p => p.VehicleTollId)
            .OnDelete(DeleteBehavior.Cascade);

        // PaidByUserId is required here (unlike VehicleExpense.RecordedByUserId),
        // so a departing user cannot silently erase who paid a toll — deleting
        // that user is blocked instead, the same restraint InvoiceConfigurations
        // and RefundConfiguration put around their own required user links.
        builder.HasOne(p => p.PaidByUser)
            .WithMany()
            .HasForeignKey(p => p.PaidByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.VehicleTollId);
    }
}
