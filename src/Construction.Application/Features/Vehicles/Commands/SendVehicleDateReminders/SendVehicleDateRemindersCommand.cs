using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Vehicles.Commands.SendVehicleDateReminders;

/// <summary>
/// Tells the office when a vehicle's registration runs out, a rental ends, or a vehicle rented out is
/// due back: once 30 days before and once 7 days before.
/// </summary>
/// <remarks>
/// A date entered inside the last week gets the 7-day notice only; saying "30 days" and "7 days" in the
/// same minute would be noise. Returns how many notices went out.
/// </remarks>
public record SendVehicleDateRemindersCommand : IRequest<int>
{
    public const int EarlyStage = 30;

    public const int LateStage = 7;
}

public class SendVehicleDateRemindersCommandHandler : IRequestHandler<SendVehicleDateRemindersCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notifications;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SendVehicleDateRemindersCommandHandler(
        IApplicationDbContext context,
        INotificationService notifications,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _notifications = notifications;
        _dateTimeProvider = dateTimeProvider;
    }

    private sealed record Due(Guid VehicleId, string VehicleName, VehicleDateKind Kind, DateOnly Date);

    public async Task<int> Handle(SendVehicleDateRemindersCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var horizon = today.AddDays(SendVehicleDateRemindersCommand.EarlyStage);

        var due = new List<Due>();

        // Every date a vehicle carries itself, read in one pass: the ones inside the window are announced.
        var vehicles = await _context.Vehicles
            .AsNoTracking()
            .Where(v => (v.RegistrationValidUntil >= today && v.RegistrationValidUntil <= horizon)
                || (v.TechnicalInspectionValidUntil >= today && v.TechnicalInspectionValidUntil <= horizon)
                || (v.InsuranceValidUntil >= today && v.InsuranceValidUntil <= horizon)
                || (v.NextServiceDue >= today && v.NextServiceDue <= horizon)
                || (v.OwnershipType != VehicleOwnershipType.Owned
                    && v.RentedUntil >= today && v.RentedUntil <= horizon))
            .Select(v => new
            {
                v.Id,
                Name = v.Brand + " " + v.Model + " (" + v.RegistrationNumber + ")",
                v.OwnershipType,
                v.RegistrationValidUntil,
                v.TechnicalInspectionValidUntil,
                v.InsuranceValidUntil,
                v.NextServiceDue,
                v.RentedUntil
            })
            .ToListAsync(cancellationToken);

        foreach (var v in vehicles)
        {
            void Add(VehicleDateKind kind, DateOnly? date)
            {
                if (date is { } d && d >= today && d <= horizon)
                {
                    due.Add(new Due(v.Id, v.Name, kind, d));
                }
            }

            Add(VehicleDateKind.Registration, v.RegistrationValidUntil);
            Add(VehicleDateKind.TechnicalInspection, v.TechnicalInspectionValidUntil);
            Add(VehicleDateKind.Insurance, v.InsuranceValidUntil);
            Add(VehicleDateKind.Service, v.NextServiceDue);

            if (v.OwnershipType != VehicleOwnershipType.Owned)
            {
                Add(VehicleDateKind.RentedUntil, v.RentedUntil);
            }
        }

        due.AddRange((await _context.VehicleRentalsOut
                .AsNoTracking()
                .Where(r => r.EndDate == null && r.ExpectedEndDate != null
                    && r.ExpectedEndDate >= today && r.ExpectedEndDate <= horizon)
                .Select(r => new
                {
                    r.VehicleId,
                    Name = r.Vehicle.Brand + " " + r.Vehicle.Model + " (" + r.Vehicle.RegistrationNumber + ")",
                    r.ExpectedEndDate
                })
                .ToListAsync(cancellationToken))
            .Select(r => new Due(r.VehicleId, r.Name, VehicleDateKind.RentalOutReturn, r.ExpectedEndDate!.Value)));

        if (due.Count == 0)
        {
            return 0;
        }

        var recipientIds = await _context.Users
            .Where(u => u.IsActive && (u.Role == UserRole.SuperAdmin || u.Role == UserRole.Admin))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        var sent = 0;

        foreach (var item in due)
        {
            var daysLeft = item.Date.DayNumber - today.DayNumber;
            var stage = daysLeft <= SendVehicleDateRemindersCommand.LateStage
                ? SendVehicleDateRemindersCommand.LateStage
                : SendVehicleDateRemindersCommand.EarlyStage;

            // The stage being announced, plus every earlier one it makes redundant.
            var claims = stage == SendVehicleDateRemindersCommand.LateStage
                ? new[] { SendVehicleDateRemindersCommand.EarlyStage, SendVehicleDateRemindersCommand.LateStage }
                : new[] { SendVehicleDateRemindersCommand.EarlyStage };

            var announced = false;

            foreach (var daysBefore in claims)
            {
                var claim = new VehicleDateReminder
                {
                    VehicleId = item.VehicleId,
                    Kind = item.Kind,
                    DueDate = item.Date,
                    DaysBefore = daysBefore,
                    SentAt = now
                };

                _context.VehicleDateReminders.Add(claim);

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);

                    if (daysBefore == stage)
                    {
                        announced = true;
                    }
                }
                catch (DbUpdateException)
                {
                    _context.VehicleDateReminders.Remove(claim);
                }
            }

            if (!announced || recipientIds.Count == 0)
            {
                continue;
            }

            await _notifications.NotifyUsersAsync(
                recipientIds,
                NotificationType.VehicleDateExpiring,
                "Vehicle date coming up",
                $"{item.VehicleName}: {item.Kind} {item.Date:dd.MM.yyyy} ({daysLeft} days left)",
                new Dictionary<string, string>
                {
                    ["vehicleId"] = item.VehicleId.ToString(),
                    ["vehicleName"] = item.VehicleName,
                    ["kind"] = item.Kind.ToString(),
                    ["date"] = item.Date.ToString("yyyy-MM-dd"),
                    ["daysLeft"] = daysLeft.ToString()
                },
                cancellationToken: cancellationToken);

            sent++;
        }

        return sent;
    }
}
