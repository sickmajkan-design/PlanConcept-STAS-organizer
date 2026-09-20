using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Accommodations.Stays;

/// <summary>Puts a person into an accommodation from a date, optionally until another.</summary>
public record AddAccommodationStayCommand : IRequest<AccommodationStayDto>
{
    /// <summary>Set from the route, never from the body.</summary>
    public Guid AccommodationId { get; init; }

    public Guid EmployeeId { get; init; }

    /// <summary>Defaults to today.</summary>
    public DateOnly? StartDate { get; init; }

    /// <summary>Null while they still live there.</summary>
    public DateOnly? EndDate { get; init; }

    /// <summary>The project the housing is charged to. Optional.</summary>
    public Guid? ProjectId { get; init; }

    public string? Note { get; init; }
}

/// <summary>Corrects a stay: moves the dates, ends it, or changes what it is charged to.</summary>
public record UpdateAccommodationStayCommand : IRequest<AccommodationStayDto>
{
    /// <summary>Set from the route, never from the body.</summary>
    public Guid Id { get; init; }

    public DateOnly StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    public Guid? ProjectId { get; init; }

    public string? Note { get; init; }
}

public record DeleteAccommodationStayCommand(Guid Id) : IRequest;

public class AddAccommodationStayCommandValidator : AbstractValidator<AddAccommodationStayCommand>
{
    public AddAccommodationStayCommandValidator()
    {
        RuleFor(x => x.AccommodationId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate!.Value)
            .WithMessage("The stay cannot end before it starts.")
            .When(x => x.StartDate is not null && x.EndDate is not null);

        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public class UpdateAccommodationStayCommandValidator : AbstractValidator<UpdateAccommodationStayCommand>
{
    public UpdateAccommodationStayCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("The stay cannot end before it starts.")
            .When(x => x.EndDate is not null);

        RuleFor(x => x.Note).MaximumLength(500);
    }
}

internal static class StayRules
{
    /// <summary>
    /// A person lives in one place at a time. Says where they already are, so the
    /// office can end that stay instead of guessing.
    /// </summary>
    public static async Task EnsureNoOverlapAsync(
        IApplicationDbContext context,
        Guid employeeId,
        Guid? excludeStayId,
        DateOnly start,
        DateOnly? end,
        CancellationToken cancellationToken)
    {
        var clash = await context.AccommodationStays
            .AsNoTracking()
            .Where(s => s.EmployeeId == employeeId
                && (excludeStayId == null || s.Id != excludeStayId)
                && s.StartDate <= (end ?? DateOnly.MaxValue)
                && (s.EndDate == null || s.EndDate >= start))
            .Select(s => new { Where = s.Accommodation.Name ?? s.Accommodation.Address })
            .FirstOrDefaultAsync(cancellationToken);

        if (clash is not null)
        {
            throw new ConflictException(
                $"This person already has a stay at {clash.Where} on those dates. End that one first.");
        }
    }
}

public class AddAccommodationStayCommandHandler
    : IRequestHandler<AddAccommodationStayCommand, AccommodationStayDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationService _notifications;

    public AddAccommodationStayCommandHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        INotificationService notifications)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _notifications = notifications;
    }

    public async Task<AccommodationStayDto> Handle(
        AddAccommodationStayCommand request,
        CancellationToken cancellationToken)
    {
        if (!await _context.Accommodations.AnyAsync(a => a.Id == request.AccommodationId, cancellationToken))
        {
            throw new NotFoundException(nameof(Accommodation), request.AccommodationId);
        }

        if (!await _context.Employees.AnyAsync(e => e.Id == request.EmployeeId, cancellationToken))
        {
            throw new NotFoundException(nameof(Employee), request.EmployeeId);
        }

        if (request.ProjectId is { } projectId
            && !await _context.Projects.AnyAsync(p => p.Id == projectId, cancellationToken))
        {
            throw new NotFoundException(nameof(Project), projectId);
        }

        var start = request.StartDate ?? DateOnly.FromDateTime(_dateTimeProvider.UtcNow);

        await StayRules.EnsureNoOverlapAsync(
            _context, request.EmployeeId, null, start, request.EndDate, cancellationToken);

        var stay = new AccommodationStay
        {
            AccommodationId = request.AccommodationId,
            EmployeeId = request.EmployeeId,
            StartDate = start,
            EndDate = request.EndDate,
            ProjectId = request.ProjectId,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim()
        };

        _context.AccommodationStays.Add(stay);
        await _context.SaveChangesAsync(cancellationToken);

        await NotifyPersonAsync(stay, cancellationToken);

        return await _context.AccommodationStays
            .AsNoTracking()
            .Where(s => s.Id == stay.Id)
            .Select(AccommodationStayMapping.Projection)
            .FirstAsync(cancellationToken);
    }

    /// <summary>Tells the person where they now live; a person with no account has nobody to tell.</summary>
    private async Task NotifyPersonAsync(AccommodationStay stay, CancellationToken cancellationToken)
    {
        var userIds = await _context.Users
            .Where(u => u.IsActive && u.EmployeeId == stay.EmployeeId)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        if (userIds.Count == 0)
        {
            return;
        }

        var place = await _context.Accommodations
            .AsNoTracking()
            .Where(a => a.Id == stay.AccommodationId)
            .Select(a => new { a.Name, a.Address })
            .FirstAsync(cancellationToken);

        var name = string.IsNullOrWhiteSpace(place.Name) ? place.Address : place.Name;

        await _notifications.NotifyUsersAsync(
            userIds,
            NotificationType.AccommodationAssigned,
            "New accommodation",
            $"You have been housed at {name} from {stay.StartDate:dd.MM.yyyy}.",
            new Dictionary<string, string>
            {
                ["accommodationId"] = stay.AccommodationId.ToString(),
                ["accommodationName"] = name,
                ["startDate"] = stay.StartDate.ToString("yyyy-MM-dd")
            },
            cancellationToken: cancellationToken);
    }
}

public class UpdateAccommodationStayCommandHandler
    : IRequestHandler<UpdateAccommodationStayCommand, AccommodationStayDto>
{
    private readonly IApplicationDbContext _context;

    public UpdateAccommodationStayCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AccommodationStayDto> Handle(
        UpdateAccommodationStayCommand request,
        CancellationToken cancellationToken)
    {
        var stay = await _context.AccommodationStays
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(AccommodationStay), request.Id);

        if (request.ProjectId is { } projectId
            && !await _context.Projects.AnyAsync(p => p.Id == projectId, cancellationToken))
        {
            throw new NotFoundException(nameof(Project), projectId);
        }

        await StayRules.EnsureNoOverlapAsync(
            _context, stay.EmployeeId, stay.Id, request.StartDate, request.EndDate, cancellationToken);

        stay.StartDate = request.StartDate;
        stay.EndDate = request.EndDate;
        stay.ProjectId = request.ProjectId;
        stay.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.AccommodationStays
            .AsNoTracking()
            .Where(s => s.Id == stay.Id)
            .Select(AccommodationStayMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}

public class DeleteAccommodationStayCommandHandler : IRequestHandler<DeleteAccommodationStayCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteAccommodationStayCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteAccommodationStayCommand request, CancellationToken cancellationToken)
    {
        var stay = await _context.AccommodationStays
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(AccommodationStay), request.Id);

        _context.AccommodationStays.Remove(stay);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
