using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Branches;

/// <summary>One of the operator's own business units (poslovna jedinica).</summary>
public class BranchDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public string Color { get; init; } = null!;

    public bool IsActive { get; init; }

    /// <summary>How many projects belong to this branch. A branch with any can only be switched off, not deleted.</summary>
    public int ProjectCount { get; init; }
}

/// <summary>Who may manage branches: management. Everyone else only reads them, to filter by.</summary>
public static class BranchRules
{
    public static void EnsureManagement(ICurrentUserService currentUserService)
    {
        if (currentUserService.Role is not (UserRole.SuperAdmin or UserRole.Admin))
        {
            throw new ForbiddenAccessException("Only management may change business units.");
        }
    }
}

public static class BranchLookup
{
    /// <summary>
    /// Loads the business unit a record names, so the record it is saved on can be returned with the
    /// unit's name and colour. Null when none is named; refuses one that does not exist.
    /// </summary>
    public static async Task<Branch?> LoadAsync(
        IApplicationDbContext context,
        Guid? branchId,
        CancellationToken cancellationToken)
    {
        if (branchId is not { } id)
        {
            return null;
        }

        return await context.Branches.FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Branch), id);
    }

    /// <summary>Refuses a record that names a business unit that does not exist.</summary>
    public static async Task EnsureExistsAsync(
        IApplicationDbContext context,
        Guid? branchId,
        CancellationToken cancellationToken)
    {
        if (branchId is { } id && !await context.Branches.AnyAsync(b => b.Id == id, cancellationToken))
        {
            throw new NotFoundException(nameof(Branch), id);
        }
    }
}

/// <summary>The business units, active first.</summary>
public record GetBranchesQuery : IRequest<IReadOnlyList<BranchDto>>;

public class GetBranchesQueryHandler : IRequestHandler<GetBranchesQuery, IReadOnlyList<BranchDto>>
{
    private readonly IApplicationDbContext _context;

    public GetBranchesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<BranchDto>> Handle(GetBranchesQuery request, CancellationToken cancellationToken)
    {
        return await _context.Branches
            .AsNoTracking()
            .OrderByDescending(b => b.IsActive)
            .ThenBy(b => b.Name)
            .Select(b => new BranchDto
            {
                Id = b.Id,
                Name = b.Name,
                Color = b.Color,
                IsActive = b.IsActive,
                ProjectCount = _context.Projects.Count(p => p.BranchId == b.Id),
            })
            .ToListAsync(cancellationToken);
    }
}

/// <summary>Adds a business unit.</summary>
public record CreateBranchCommand : IRequest<BranchDto>
{
    public string Name { get; init; } = null!;

    public string Color { get; init; } = "#3457D5";
}

public class CreateBranchCommandValidator : AbstractValidator<CreateBranchCommand>
{
    public CreateBranchCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("The business unit name is required.").MaximumLength(200);
        RuleFor(x => x.Color).Matches("^#[0-9A-Fa-f]{6}$").WithMessage("The colour must be a hex value like #3457D5.");
    }
}

public class CreateBranchCommandHandler : IRequestHandler<CreateBranchCommand, BranchDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateBranchCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<BranchDto> Handle(CreateBranchCommand request, CancellationToken cancellationToken)
    {
        BranchRules.EnsureManagement(_currentUserService);

        var name = request.Name.Trim();

        if (await _context.Branches.AnyAsync(b => b.Name == name, cancellationToken))
        {
            throw new ConflictException($"A business unit named '{name}' already exists.");
        }

        var branch = new Branch { Name = name, Color = request.Color.ToUpperInvariant() };

        _context.Branches.Add(branch);
        await _context.SaveChangesAsync(cancellationToken);

        return new BranchDto { Id = branch.Id, Name = branch.Name, Color = branch.Color, IsActive = true };
    }
}

/// <summary>Renames a business unit, recolours it, or switches it off or on.</summary>
public record UpdateBranchCommand : IRequest<BranchDto>
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public string Color { get; init; } = "#3457D5";

    public bool IsActive { get; init; } = true;
}

public class UpdateBranchCommandValidator : AbstractValidator<UpdateBranchCommand>
{
    public UpdateBranchCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().WithMessage("The business unit name is required.").MaximumLength(200);
        RuleFor(x => x.Color).Matches("^#[0-9A-Fa-f]{6}$").WithMessage("The colour must be a hex value like #3457D5.");
    }
}

public class UpdateBranchCommandHandler : IRequestHandler<UpdateBranchCommand, BranchDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateBranchCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<BranchDto> Handle(UpdateBranchCommand request, CancellationToken cancellationToken)
    {
        BranchRules.EnsureManagement(_currentUserService);

        var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Branch), request.Id);

        var name = request.Name.Trim();

        if (await _context.Branches.AnyAsync(b => b.Name == name && b.Id != branch.Id, cancellationToken))
        {
            throw new ConflictException($"A business unit named '{name}' already exists.");
        }

        branch.Name = name;
        branch.Color = request.Color.ToUpperInvariant();
        branch.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        return new BranchDto
        {
            Id = branch.Id,
            Name = branch.Name,
            Color = branch.Color,
            IsActive = branch.IsActive,
            ProjectCount = await _context.Projects.CountAsync(p => p.BranchId == branch.Id, cancellationToken),
        };
    }
}

/// <summary>Deletes a business unit that no project belongs to.</summary>
public record DeleteBranchCommand(Guid Id) : IRequest;

public class DeleteBranchCommandHandler : IRequestHandler<DeleteBranchCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteBranchCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteBranchCommand request, CancellationToken cancellationToken)
    {
        BranchRules.EnsureManagement(_currentUserService);

        var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Branch), request.Id);

        if (await _context.Projects.IgnoreQueryFilters().AnyAsync(p => p.BranchId == branch.Id, cancellationToken))
        {
            throw new ConflictException("This business unit has projects and can only be switched off, not deleted.");
        }

        _context.Branches.Remove(branch);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Makes exactly these Main projects the projects of a business unit. Projects named here move
/// into it (with their sub-projects); projects that were in it and are not named are released.
/// </summary>
public record SetBranchProjectsCommand : IRequest<BranchDto>
{
    public Guid Id { get; init; }

    public IReadOnlyList<Guid> ProjectIds { get; init; } = [];
}

public class SetBranchProjectsCommandHandler : IRequestHandler<SetBranchProjectsCommand, BranchDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SetBranchProjectsCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<BranchDto> Handle(SetBranchProjectsCommand request, CancellationToken cancellationToken)
    {
        BranchRules.EnsureManagement(_currentUserService);

        var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Branch), request.Id);

        var chosen = request.ProjectIds.ToHashSet();

        // Main projects and their sub-projects are handled together: the sub-project follows its parent.
        var projects = await _context.Projects
            .Where(p => p.BranchId == branch.Id
                || chosen.Contains(p.Id)
                || (p.ParentProjectId != null && chosen.Contains(p.ParentProjectId.Value)))
            .ToListAsync(cancellationToken);

        foreach (var project in projects)
        {
            var mainId = project.ParentProjectId ?? project.Id;
            project.BranchId = chosen.Contains(mainId) ? branch.Id : null;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new BranchDto
        {
            Id = branch.Id,
            Name = branch.Name,
            Color = branch.Color,
            IsActive = branch.IsActive,
            ProjectCount = await _context.Projects.CountAsync(p => p.BranchId == branch.Id, cancellationToken),
        };
    }
}
