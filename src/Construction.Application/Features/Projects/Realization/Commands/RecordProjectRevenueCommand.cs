using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Projects.Realization.Models;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Projects.Realization.Commands;

/// <summary>Records money that has come in against a project's contract.</summary>
public record RecordProjectRevenueCommand : IRequest<ProjectRevenueDto>
{
    public Guid ProjectId { get; init; }

    public decimal Amount { get; init; }

    /// <summary>Defaults to today.</summary>
    public DateOnly? OccurredOn { get; init; }

    public string? Note { get; init; }
}

public class RecordProjectRevenueCommandValidator : AbstractValidator<RecordProjectRevenueCommand>
{
    public RecordProjectRevenueCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);

        RuleFor(x => x.ProjectId).NotEmpty();

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("A payment of nothing is not a payment.");

        RuleFor(x => x.OccurredOn)
            .LessThanOrEqualTo(today)
            .WithMessage("Revenue cannot be recorded for the future.")
            .When(x => x.OccurredOn is not null);

        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public class RecordProjectRevenueCommandHandler
    : IRequestHandler<RecordProjectRevenueCommand, ProjectRevenueDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RecordProjectRevenueCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ProjectRevenueDto> Handle(
        RecordProjectRevenueCommand request,
        CancellationToken cancellationToken)
    {
        if (!await _context.Projects.AnyAsync(p => p.Id == request.ProjectId, cancellationToken))
        {
            throw new NotFoundException(nameof(Project), request.ProjectId);
        }

        var revenue = new ProjectRevenue
        {
            ProjectId = request.ProjectId,
            Amount = request.Amount,
            OccurredOn = request.OccurredOn
                ?? DateOnly.FromDateTime(_dateTimeProvider.UtcNow),
            Note = request.Note?.Trim(),
            RecordedByUserId = _currentUserService.UserId
        };

        _context.ProjectRevenues.Add(revenue);
        await _context.SaveChangesAsync(cancellationToken);

        return await _context.ProjectRevenues
            .AsNoTracking()
            .Where(r => r.Id == revenue.Id)
            .Select(ProjectRevenueMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}

public record DeleteProjectRevenueCommand(Guid Id) : IRequest;

public class DeleteProjectRevenueCommandHandler : IRequestHandler<DeleteProjectRevenueCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteProjectRevenueCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteProjectRevenueCommand request, CancellationToken cancellationToken)
    {
        var revenue = await _context.ProjectRevenues
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ProjectRevenue), request.Id);

        _context.ProjectRevenues.Remove(revenue);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
