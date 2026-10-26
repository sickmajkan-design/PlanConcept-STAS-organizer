using Construction.Application.Common;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.VehicleFines;

public record VehicleFineDto
{
    public Guid Id { get; init; }

    public Guid VehicleId { get; init; }

    public DateOnly OccurredOn { get; init; }

    public string Description { get; init; } = null!;

    public decimal Amount { get; init; }

    public DateOnly? DueDate { get; init; }

    public Guid? DriverEmployeeId { get; init; }

    public string? DriverName { get; init; }

    /// <summary>Firm or Driver.</summary>
    public string BorneBy { get; init; } = nameof(VehicleFineBearer.Firm);

    /// <summary>Open, Paid or Contested.</summary>
    public string Status { get; init; } = nameof(VehicleFineStatus.Open);

    public DateOnly? PaidOn { get; init; }

    /// <summary>Open and past its due date.</summary>
    public bool Overdue { get; init; }

    public string? Note { get; init; }

    public DateTime CreatedAt { get; init; }
}

internal static class VehicleFineMapping
{
    public static async Task<List<VehicleFineDto>> LoadAsync(
        IApplicationDbContext context, IQueryable<VehicleFine> fines, DateOnly today, CancellationToken cancellationToken)
    {
        var rows = await fines
            .AsNoTracking()
            .Select(f => new
            {
                f.Id,
                f.VehicleId,
                f.OccurredOn,
                f.Description,
                f.Amount,
                f.DueDate,
                f.DriverEmployeeId,
                DriverName = f.DriverEmployee != null ? f.DriverEmployee.FirstName + " " + f.DriverEmployee.LastName : null,
                f.BorneBy,
                f.Status,
                f.PaidOn,
                f.Note,
                f.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return rows
            // Open ones first, the nearest due date on top; the rest by date.
            .OrderBy(f => f.Status == VehicleFineStatus.Open ? 0 : 1)
            .ThenBy(f => f.Status == VehicleFineStatus.Open ? f.DueDate ?? DateOnly.MaxValue : DateOnly.MaxValue)
            .ThenByDescending(f => f.OccurredOn)
            .Select(f => new VehicleFineDto
            {
                Id = f.Id,
                VehicleId = f.VehicleId,
                OccurredOn = f.OccurredOn,
                Description = f.Description,
                Amount = f.Amount,
                DueDate = f.DueDate,
                DriverEmployeeId = f.DriverEmployeeId,
                DriverName = f.DriverName,
                BorneBy = f.BorneBy.ToString(),
                Status = f.Status.ToString(),
                PaidOn = f.PaidOn,
                Overdue = f.Status == VehicleFineStatus.Open && f.DueDate != null && f.DueDate < today,
                Note = f.Note,
                CreatedAt = f.CreatedAt,
            })
            .ToList();
    }
}

// ---- reading -----------------------------------------------------------------------------------------------------

public record GetVehicleFinesQuery(Guid VehicleId) : IRequest<IReadOnlyList<VehicleFineDto>>;

public class GetVehicleFinesQueryHandler(IApplicationDbContext context, IDateTimeProvider clock)
    : IRequestHandler<GetVehicleFinesQuery, IReadOnlyList<VehicleFineDto>>
{
    public async Task<IReadOnlyList<VehicleFineDto>> Handle(GetVehicleFinesQuery request, CancellationToken cancellationToken)
    {
        if (!await context.Vehicles.AnyAsync(v => v.Id == request.VehicleId, cancellationToken))
        {
            throw new NotFoundException(nameof(Vehicle), request.VehicleId);
        }

        return await VehicleFineMapping.LoadAsync(
            context, context.VehicleFines.Where(f => f.VehicleId == request.VehicleId), CompanyTime.LocalDate(clock.UtcNow), cancellationToken);
    }
}

// ---- writing -----------------------------------------------------------------------------------------------------

public abstract record VehicleFineFields
{
    public DateOnly OccurredOn { get; init; }

    public string Description { get; init; } = null!;

    public decimal Amount { get; init; }

    public DateOnly? DueDate { get; init; }

    public Guid? DriverEmployeeId { get; init; }

    public VehicleFineBearer BorneBy { get; init; } = VehicleFineBearer.Firm;

    public VehicleFineStatus Status { get; init; } = VehicleFineStatus.Open;

    public DateOnly? PaidOn { get; init; }

    public string? Note { get; init; }
}

public abstract class VehicleFineFieldsValidator<T> : AbstractValidator<T>
    where T : VehicleFineFields
{
    protected VehicleFineFieldsValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0).LessThanOrEqualTo(1_000_000);
        RuleFor(x => x.Note).MaximumLength(500);
        RuleFor(x => x.BorneBy).IsInEnum();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.DueDate)
            .GreaterThanOrEqualTo(x => x.OccurredOn)
            .WithMessage("The due date cannot be before the day of the offence.")
            .When(x => x.DueDate is not null);
        // A fine the driver bears has to name the driver, or nobody owes it.
        RuleFor(x => x.DriverEmployeeId)
            .NotNull()
            .WithMessage("Name the driver when the driver bears the fine.")
            .When(x => x.BorneBy == VehicleFineBearer.Driver);
    }
}

internal static class VehicleFineRules
{
    /// <summary>A paid fine has a day it was paid; any other has none.</summary>
    public static DateOnly? PaidDay(VehicleFineStatus status, DateOnly? paidOn, DateOnly today) =>
        status == VehicleFineStatus.Paid ? paidOn ?? today : null;

    public static async Task EnsureDriverAsync(IApplicationDbContext context, Guid? driverId, CancellationToken cancellationToken)
    {
        if (driverId is { } id && !await context.Employees.AnyAsync(e => e.Id == id, cancellationToken))
        {
            throw new NotFoundException(nameof(Employee), id);
        }
    }
}

public record AddVehicleFineCommand : VehicleFineFields, IRequest<VehicleFineDto>
{
    /// <summary>Set from the route, never from the body.</summary>
    public Guid VehicleId { get; init; }
}

public class AddVehicleFineCommandValidator : VehicleFineFieldsValidator<AddVehicleFineCommand>;

public class AddVehicleFineCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider clock,
    ICurrentUserService currentUser,
    INotificationService notifications)
    : IRequestHandler<AddVehicleFineCommand, VehicleFineDto>
{
    public async Task<VehicleFineDto> Handle(AddVehicleFineCommand request, CancellationToken cancellationToken)
    {
        var vehicle = await context.Vehicles.AsNoTracking()
            .Where(v => v.Id == request.VehicleId)
            .Select(v => new { v.Id, Name = v.Brand + " " + v.Model + " (" + v.RegistrationNumber + ")" })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Vehicle), request.VehicleId);

        await VehicleFineRules.EnsureDriverAsync(context, request.DriverEmployeeId, cancellationToken);

        var today = CompanyTime.LocalDate(clock.UtcNow);

        var fine = new VehicleFine
        {
            VehicleId = vehicle.Id,
            OccurredOn = request.OccurredOn,
            Description = request.Description.Trim(),
            Amount = request.Amount,
            DueDate = request.DueDate,
            DriverEmployeeId = request.DriverEmployeeId,
            BorneBy = request.BorneBy,
            Status = request.Status,
            PaidOn = VehicleFineRules.PaidDay(request.Status, request.PaidOn, today),
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            RecordedByUserId = currentUser.UserId,
        };

        context.VehicleFines.Add(fine);
        await context.SaveChangesAsync(cancellationToken);

        // The office entered it, so the others in the office are told; the one who entered it knows.
        var recipients = await context.Users
            .Where(u => u.IsActive && (u.Role == UserRole.SuperAdmin || u.Role == UserRole.Admin) && u.Id != currentUser.UserId)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        if (recipients.Count > 0 && fine.Status == VehicleFineStatus.Open)
        {
            await notifications.NotifyUsersAsync(
                recipients,
                NotificationType.VehicleFineAdded,
                "Vehicle fine",
                $"{vehicle.Name}: {fine.Description}, {fine.Amount:0.00}",
                new Dictionary<string, string>
                {
                    ["vehicleId"] = vehicle.Id.ToString(),
                    ["vehicleName"] = vehicle.Name,
                    ["fineId"] = fine.Id.ToString(),
                    ["description"] = fine.Description,
                    ["amount"] = fine.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    ["dueDate"] = fine.DueDate?.ToString("yyyy-MM-dd") ?? "",
                },
                cancellationToken: cancellationToken);
        }

        return (await VehicleFineMapping.LoadAsync(context, context.VehicleFines.Where(f => f.Id == fine.Id), today, cancellationToken)).Single();
    }
}

public record UpdateVehicleFineCommand : VehicleFineFields, IRequest<VehicleFineDto>
{
    /// <summary>Set from the route, never from the body.</summary>
    public Guid Id { get; init; }
}

public class UpdateVehicleFineCommandValidator : VehicleFineFieldsValidator<UpdateVehicleFineCommand>;

public class UpdateVehicleFineCommandHandler(IApplicationDbContext context, IDateTimeProvider clock)
    : IRequestHandler<UpdateVehicleFineCommand, VehicleFineDto>
{
    public async Task<VehicleFineDto> Handle(UpdateVehicleFineCommand request, CancellationToken cancellationToken)
    {
        var fine = await context.VehicleFines.FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(VehicleFine), request.Id);

        await VehicleFineRules.EnsureDriverAsync(context, request.DriverEmployeeId, cancellationToken);

        var today = CompanyTime.LocalDate(clock.UtcNow);

        fine.OccurredOn = request.OccurredOn;
        fine.Description = request.Description.Trim();
        fine.Amount = request.Amount;
        fine.DueDate = request.DueDate;
        fine.DriverEmployeeId = request.DriverEmployeeId;
        fine.BorneBy = request.BorneBy;
        fine.Status = request.Status;
        fine.PaidOn = VehicleFineRules.PaidDay(request.Status, request.PaidOn ?? fine.PaidOn, today);
        fine.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();

        await context.SaveChangesAsync(cancellationToken);

        return (await VehicleFineMapping.LoadAsync(context, context.VehicleFines.Where(f => f.Id == fine.Id), today, cancellationToken)).Single();
    }
}

public record DeleteVehicleFineCommand(Guid Id) : IRequest;

public class DeleteVehicleFineCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteVehicleFineCommand>
{
    public async Task Handle(DeleteVehicleFineCommand request, CancellationToken cancellationToken)
    {
        var fine = await context.VehicleFines.FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(VehicleFine), request.Id);

        context.VehicleFines.Remove(fine);
        await context.SaveChangesAsync(cancellationToken);
    }
}

// ---- reminders ---------------------------------------------------------------------------------------------------

/// <summary>
/// Tells the admins about open fines that are close to their due date, once at a week and once at two days (and once
/// more if the date has passed). Run every day by the reminder service.
/// </summary>
public record SendVehicleFineRemindersCommand : IRequest<int>
{
    public const int EarlyStage = 7;

    public const int LateStage = 2;
}

public class SendVehicleFineRemindersCommandHandler(
    IApplicationDbContext context,
    INotificationService notifications,
    IDateTimeProvider clock)
    : IRequestHandler<SendVehicleFineRemindersCommand, int>
{
    public async Task<int> Handle(SendVehicleFineRemindersCommand request, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var today = CompanyTime.LocalDate(now);
        var horizon = today.AddDays(SendVehicleFineRemindersCommand.EarlyStage);

        var due = await context.VehicleFines
            .AsNoTracking()
            .Where(f => f.Status == VehicleFineStatus.Open && f.DueDate != null && f.DueDate <= horizon)
            .Select(f => new
            {
                f.Id,
                f.VehicleId,
                VehicleName = f.Vehicle.Brand + " " + f.Vehicle.Model + " (" + f.Vehicle.RegistrationNumber + ")",
                f.Description,
                f.Amount,
                DueDate = f.DueDate!.Value,
            })
            .ToListAsync(cancellationToken);

        if (due.Count == 0)
        {
            return 0;
        }

        var recipients = await context.Users
            .Where(u => u.IsActive && (u.Role == UserRole.SuperAdmin || u.Role == UserRole.Admin))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        var sent = 0;

        foreach (var fine in due)
        {
            var daysLeft = fine.DueDate.DayNumber - today.DayNumber;
            var stage = daysLeft <= SendVehicleFineRemindersCommand.LateStage
                ? SendVehicleFineRemindersCommand.LateStage
                : SendVehicleFineRemindersCommand.EarlyStage;

            // The stage being announced, plus the earlier one it makes redundant.
            var claims = stage == SendVehicleFineRemindersCommand.LateStage
                ? new[] { SendVehicleFineRemindersCommand.EarlyStage, SendVehicleFineRemindersCommand.LateStage }
                : new[] { SendVehicleFineRemindersCommand.EarlyStage };

            var announced = false;

            foreach (var daysBefore in claims)
            {
                var claim = new VehicleFineReminder { VehicleFineId = fine.Id, DaysBefore = daysBefore, DueDate = fine.DueDate, SentAt = now };
                context.VehicleFineReminders.Add(claim);

                try
                {
                    await context.SaveChangesAsync(cancellationToken);

                    if (daysBefore == stage)
                    {
                        announced = true;
                    }
                }
                catch (DbUpdateException)
                {
                    context.VehicleFineReminders.Remove(claim);
                }
            }

            if (!announced || recipients.Count == 0)
            {
                continue;
            }

            await notifications.NotifyUsersAsync(
                recipients,
                NotificationType.VehicleFineDue,
                "Vehicle fine due",
                $"{fine.VehicleName}: {fine.Description}, {fine.Amount:0.00}, due {fine.DueDate:dd.MM.yyyy} ({daysLeft} days)",
                new Dictionary<string, string>
                {
                    ["vehicleId"] = fine.VehicleId.ToString(),
                    ["vehicleName"] = fine.VehicleName,
                    ["fineId"] = fine.Id.ToString(),
                    ["description"] = fine.Description,
                    ["amount"] = fine.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    ["dueDate"] = fine.DueDate.ToString("yyyy-MM-dd"),
                    ["daysLeft"] = daysLeft.ToString(),
                },
                cancellationToken: cancellationToken);

            sent++;
        }

        return sent;
    }
}
