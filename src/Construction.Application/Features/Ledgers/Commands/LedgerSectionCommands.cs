using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Ledgers.Models;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Ledgers.Commands;

public record AddLedgerSectionCommand : IRequest<LedgerSectionDto>
{
    public Guid LedgerId { get; init; }

    public string Name { get; init; } = null!;

    public Guid? ProjectId { get; init; }
}

public class AddLedgerSectionCommandValidator : AbstractValidator<AddLedgerSectionCommand>
{
    public AddLedgerSectionCommandValidator()
    {
        RuleFor(x => x.LedgerId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
    }
}

public class AddLedgerSectionCommandHandler : IRequestHandler<AddLedgerSectionCommand, LedgerSectionDto>
{
    private readonly IApplicationDbContext _context;

    public AddLedgerSectionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LedgerSectionDto> Handle(
        AddLedgerSectionCommand request,
        CancellationToken cancellationToken)
    {
        if (!await _context.Ledgers.AnyAsync(l => l.Id == request.LedgerId, cancellationToken))
        {
            throw new NotFoundException(nameof(Ledger), request.LedgerId);
        }

        string? projectName = null;
        if (request.ProjectId is { } projectId)
        {
            projectName = await _context.Projects
                .Where(p => p.Id == projectId)
                .Select(p => p.Name)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException(nameof(Project), projectId);
        }

        var nextOrder = await _context.LedgerSections
            .Where(s => s.LedgerId == request.LedgerId)
            .Select(s => (int?)s.SortOrder)
            .MaxAsync(cancellationToken) ?? -1;

        var section = new LedgerSection
        {
            LedgerId = request.LedgerId,
            Name = request.Name.Trim(),
            ProjectId = request.ProjectId,
            SortOrder = nextOrder + 1,
        };

        _context.LedgerSections.Add(section);
        await _context.SaveChangesAsync(cancellationToken);

        return new LedgerSectionDto
        {
            Id = section.Id,
            Name = section.Name,
            ProjectId = section.ProjectId,
            ProjectName = projectName,
            SortOrder = section.SortOrder,
            Rows = Array.Empty<LedgerRowDto>(),
        };
    }
}

public record UpdateLedgerSectionCommand : IRequest<LedgerSectionDto>
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public Guid? ProjectId { get; init; }
}

public class UpdateLedgerSectionCommandValidator : AbstractValidator<UpdateLedgerSectionCommand>
{
    public UpdateLedgerSectionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
    }
}

public class UpdateLedgerSectionCommandHandler
    : IRequestHandler<UpdateLedgerSectionCommand, LedgerSectionDto>
{
    private readonly IApplicationDbContext _context;

    public UpdateLedgerSectionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LedgerSectionDto> Handle(
        UpdateLedgerSectionCommand request,
        CancellationToken cancellationToken)
    {
        var section = await _context.LedgerSections
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(LedgerSection), request.Id);

        string? projectName = null;
        if (request.ProjectId is { } projectId)
        {
            projectName = await _context.Projects
                .Where(p => p.Id == projectId)
                .Select(p => p.Name)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException(nameof(Project), projectId);
        }

        section.Name = request.Name.Trim();
        section.ProjectId = request.ProjectId;

        await _context.SaveChangesAsync(cancellationToken);

        return new LedgerSectionDto
        {
            Id = section.Id,
            Name = section.Name,
            ProjectId = section.ProjectId,
            ProjectName = projectName,
            SortOrder = section.SortOrder,
            Rows = Array.Empty<LedgerRowDto>(),
        };
    }
}

/// <summary>Removes a section and every row/cell recorded under it.</summary>
public record DeleteLedgerSectionCommand(Guid Id) : IRequest;

public class DeleteLedgerSectionCommandHandler : IRequestHandler<DeleteLedgerSectionCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteLedgerSectionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteLedgerSectionCommand request, CancellationToken cancellationToken)
    {
        var section = await _context.LedgerSections
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(LedgerSection), request.Id);

        _context.LedgerSections.Remove(section);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Reorders every section of one ledger in one move.</summary>
public record ReorderLedgerSectionsCommand : IRequest
{
    public Guid LedgerId { get; init; }

    public IReadOnlyList<Guid> OrderedSectionIds { get; init; } = Array.Empty<Guid>();
}

public class ReorderLedgerSectionsCommandHandler : IRequestHandler<ReorderLedgerSectionsCommand>
{
    private readonly IApplicationDbContext _context;

    public ReorderLedgerSectionsCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(ReorderLedgerSectionsCommand request, CancellationToken cancellationToken)
    {
        var sections = await _context.LedgerSections
            .Where(s => s.LedgerId == request.LedgerId)
            .ToDictionaryAsync(s => s.Id, cancellationToken);

        for (var i = 0; i < request.OrderedSectionIds.Count; i++)
        {
            if (sections.TryGetValue(request.OrderedSectionIds[i], out var section))
            {
                section.SortOrder = i;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
