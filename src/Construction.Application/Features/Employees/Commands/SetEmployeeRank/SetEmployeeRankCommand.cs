using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Employees.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Employees.Commands.SetEmployeeRank;

/// <summary>Places someone on the org chart, or takes them off it.</summary>
/// <remarks>
/// Its own command rather than a field on <c>UpdateEmployeeCommand</c>, on
/// purpose. That command replaces every field it carries, and the mobile app
/// edits employees through it without knowing this field exists — a rank
/// riding along would be silently cleared by every edit made from a phone.
/// </remarks>
public record SetEmployeeRankCommand : IRequest<EmployeeDto>
{
    public Guid Id { get; init; }

    /// <summary>Null clears it, returning the person to the chart's default placement.</summary>
    public OrganizationRank? Rank { get; init; }
}

public class SetEmployeeRankCommandValidator : AbstractValidator<SetEmployeeRankCommand>
{
    public SetEmployeeRankCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Rank).IsInEnum().When(x => x.Rank is not null);
    }
}

public class SetEmployeeRankCommandHandler : IRequestHandler<SetEmployeeRankCommand, EmployeeDto>
{
    private readonly IApplicationDbContext _context;

    public SetEmployeeRankCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<EmployeeDto> Handle(
        SetEmployeeRankCommand request,
        CancellationToken cancellationToken)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Employee), request.Id);

        employee.Rank = request.Rank;

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.Employees
            .AsNoTracking()
            .Where(e => e.Id == employee.Id)
            .Select(EmployeeMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
