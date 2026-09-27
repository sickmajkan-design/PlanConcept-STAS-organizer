using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Absences.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Absences.Commands.CreateLeaveAdjustment;

/// <summary>
/// Writes a manual correction of somebody's annual leave for one year.
/// </summary>
/// <remarks>
/// Corrections are never edited or removed: a wrong one is answered with an opposite one, so
/// the history says who changed whose leave, when and why.
/// </remarks>
public record CreateLeaveAdjustmentCommand : IRequest<LeaveAdjustmentDto>
{
    public Guid EmployeeId { get; init; }

    public int Year { get; init; }

    /// <summary>Working days to add (positive) or take away (negative). Never zero.</summary>
    public int Days { get; init; }

    public string Reason { get; init; } = null!;
}

public class CreateLeaveAdjustmentCommandValidator : AbstractValidator<CreateLeaveAdjustmentCommand>
{
    public const int MaxDays = 60;

    public CreateLeaveAdjustmentCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();

        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);

        RuleFor(x => x.Days)
            .NotEqual(0).WithMessage("A correction of zero days changes nothing.")
            .InclusiveBetween(-MaxDays, MaxDays)
            .WithMessage($"A correction is at most {MaxDays} days either way.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("A reason is required: it is what makes the correction defensible.")
            .MaximumLength(500);
    }
}

public class CreateLeaveAdjustmentCommandHandler
    : IRequestHandler<CreateLeaveAdjustmentCommand, LeaveAdjustmentDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateLeaveAdjustmentCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<LeaveAdjustmentDto> Handle(
        CreateLeaveAdjustmentCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.Role is not (UserRole.SuperAdmin or UserRole.Admin))
        {
            throw new ForbiddenAccessException("Only management may correct somebody's leave.");
        }

        var exists = await _context.Employees.AnyAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (!exists)
        {
            throw new NotFoundException(nameof(Employee), request.EmployeeId);
        }

        var adjustment = new LeaveAdjustment
        {
            EmployeeId = request.EmployeeId,
            Year = request.Year,
            Days = request.Days,
            Reason = request.Reason.Trim(),
            CreatedByUserId = _currentUserService.UserId,
        };

        _context.LeaveAdjustments.Add(adjustment);
        await _context.SaveChangesAsync(cancellationToken);

        return await _context.LeaveAdjustments
            .AsNoTracking()
            .Where(a => a.Id == adjustment.Id)
            .Select(LeaveAdjustmentMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
