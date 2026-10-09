using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Customers;
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

    public BranchKind Kind { get; init; }

    /// <summary>How many projects belong to this branch. A branch with any can only be switched off, not deleted.</summary>
    public int ProjectCount { get; init; }

    /// <summary>How many employees the unit employs now.</summary>
    public int EmployeeCount { get; init; }

    /// <summary>The unit this one stands under, or null when it stands directly under the company.</summary>
    public Guid? ParentBranchId { get; init; }

    /// <summary>The person who runs the unit, if one is named.</summary>
    public Guid? HeadEmployeeId { get; init; }

    public string? HeadEmployeeName { get; init; }

    // Everything below is for management only: anyone else reads a unit to filter by it and gets
    // these as null.

    public string? LegalName { get; init; }

    public string? Address { get; init; }

    public string? City { get; init; }

    public string? PostalCode { get; init; }

    public string? CountryCode { get; init; }

    /// <summary>Tax details — null unless the caller may see them, see <see cref="CustomerRules"/>.</summary>
    public string? TaxId { get; init; }

    /// <summary>Same visibility as <see cref="TaxId"/>.</summary>
    public string? RegistrationNumber { get; init; }

    /// <summary>Same visibility as <see cref="TaxId"/>.</summary>
    public string? VatNumber { get; init; }

    public string? OwnerName { get; init; }

    public string? ContactPerson { get; init; }

    public string? Phone { get; init; }

    public string? Email { get; init; }

    public string? Note { get; init; }
}

/// <summary>Who may manage branches: management. Everyone else only reads them, to filter by.</summary>
public static class BranchRules
{
    public static bool IsManagement(UserRole? role) => role is UserRole.SuperAdmin or UserRole.Admin;

    public static void EnsureManagement(ICurrentUserService currentUserService)
    {
        if (!IsManagement(currentUserService.Role))
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

    /// <summary>Refuses a head of unit who is not an employee.</summary>
    public static async Task EnsureHeadAsync(
        IApplicationDbContext context,
        Guid? employeeId,
        CancellationToken cancellationToken)
    {
        if (employeeId is { } id && !await context.Employees.AnyAsync(e => e.Id == id, cancellationToken))
        {
            throw new NotFoundException(nameof(Employee), id);
        }
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

/// <summary>How a <see cref="Branch"/> becomes a <see cref="BranchDto"/> for the person asking.</summary>
public static class BranchView
{
    public static BranchDto Map(
        Branch branch,
        int projectCount,
        bool details,
        bool taxDetails,
        int employeeCount = 0,
        string? headName = null) => new()
    {
        ParentBranchId = branch.ParentBranchId,
        HeadEmployeeId = branch.HeadEmployeeId,
        HeadEmployeeName = headName,
        Id = branch.Id,
        Name = branch.Name,
        Color = branch.Color,
        IsActive = branch.IsActive,
        Kind = branch.Kind,
        ProjectCount = projectCount,
        EmployeeCount = employeeCount,
        LegalName = details ? branch.LegalName : null,
        Address = details ? branch.Address : null,
        City = details ? branch.City : null,
        PostalCode = details ? branch.PostalCode : null,
        CountryCode = details ? branch.CountryCode : null,
        TaxId = details && taxDetails ? branch.TaxId : null,
        RegistrationNumber = details && taxDetails ? branch.RegistrationNumber : null,
        VatNumber = details && taxDetails ? branch.VatNumber : null,
        OwnerName = details ? branch.OwnerName : null,
        ContactPerson = details ? branch.ContactPerson : null,
        Phone = details ? branch.Phone : null,
        Email = details ? branch.Email : null,
        Note = details ? branch.Note : null,
    };

    public static async Task<BranchDto> ForCallerAsync(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        Branch branch,
        CancellationToken cancellationToken)
    {
        var count = await context.Projects.CountAsync(p => p.BranchId == branch.Id, cancellationToken);
        var employees = await context.EmployeeBranches.CountAsync(
            p => p.BranchId == branch.Id && p.EndDate == null && context.Employees.Any(e => e.Id == p.EmployeeId),
            cancellationToken);
        var (details, tax) = await AccessAsync(context, currentUserService, cancellationToken);

        var headName = branch.HeadEmployeeId is { } head
            ? await context.Employees
                .Where(e => e.Id == head)
                .Select(e => e.FirstName + " " + e.LastName)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        return Map(branch, count, details, tax, employees, headName);
    }

    /// <summary>What the caller may see of a unit: its details at all, and its tax numbers in particular.</summary>
    public static async Task<(bool Details, bool Tax)> AccessAsync(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        CancellationToken cancellationToken)
    {
        var details = BranchRules.IsManagement(currentUserService.Role);

        var tax = details
            && await CustomerRules.ResolveCanViewTaxDetailsAsync(context, currentUserService, cancellationToken);

        return (details, tax);
    }
}

/// <summary>Shared payload for creating and updating a business unit, so the field rules exist once.</summary>
public abstract record BranchCommandBase
{
    public string Name { get; init; } = null!;

    public string Color { get; init; } = "#3457D5";

    public BranchKind Kind { get; init; } = BranchKind.LegalEntity;

    public string? LegalName { get; init; }

    public string? Address { get; init; }

    public string? City { get; init; }

    public string? PostalCode { get; init; }

    /// <summary>ISO 3166-1 alpha-2.</summary>
    public string? CountryCode { get; init; }

    /// <summary>Tax details are written by a SuperAdmin only; from anyone else they are ignored.</summary>
    public string? TaxId { get; init; }

    public string? RegistrationNumber { get; init; }

    public string? VatNumber { get; init; }

    public string? OwnerName { get; init; }

    public string? ContactPerson { get; init; }

    public string? Phone { get; init; }

    public string? Email { get; init; }

    public string? Note { get; init; }

    /// <summary>The unit this one is placed under; null puts it directly under the company.</summary>
    public Guid? ParentBranchId { get; init; }

    /// <summary>The employee who runs the unit.</summary>
    public Guid? HeadEmployeeId { get; init; }
}

public abstract class BranchCommandBaseValidator<T> : AbstractValidator<T>
    where T : BranchCommandBase
{
    protected BranchCommandBaseValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("The business unit name is required.").MaximumLength(200);
        RuleFor(x => x.Color).Matches("^#[0-9A-Fa-f]{6}$").WithMessage("The colour must be a hex value like #3457D5.");
        RuleFor(x => x.Kind).IsInEnum();
        RuleFor(x => x.LegalName).MaximumLength(300);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.City).MaximumLength(120);
        RuleFor(x => x.PostalCode).MaximumLength(20);
        RuleFor(x => x.CountryCode)
            .Length(2).WithMessage("Use the two-letter country code (ISO 3166-1 alpha-2).")
            .When(x => !string.IsNullOrWhiteSpace(x.CountryCode));
        RuleFor(x => x.TaxId).MaximumLength(50);
        RuleFor(x => x.RegistrationNumber).MaximumLength(50);
        RuleFor(x => x.VatNumber).MaximumLength(50);
        RuleFor(x => x.OwnerName).MaximumLength(200);
        RuleFor(x => x.ContactPerson).MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("That is not an email address.")
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Note).MaximumLength(2000);
    }
}

/// <summary>Copies the shared fields onto the entity, trimming the text ones.</summary>
public static class BranchFieldMapper
{
    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static void Apply(Branch target, BranchCommandBase source, bool canEditTaxDetails)
    {
        target.Name = source.Name.Trim();
        target.Color = source.Color.ToUpperInvariant();
        target.Kind = source.Kind;
        target.LegalName = Clean(source.LegalName);
        target.Address = Clean(source.Address);
        target.City = Clean(source.City);
        target.PostalCode = Clean(source.PostalCode);
        target.CountryCode = Clean(source.CountryCode)?.ToUpperInvariant();
        target.OwnerName = Clean(source.OwnerName);
        target.ContactPerson = Clean(source.ContactPerson);
        target.Phone = Clean(source.Phone);
        target.Email = Clean(source.Email);
        target.Note = Clean(source.Note);

        // Tax numbers are the one part with a narrower gate: anyone else's request leaves them as stored.
        if (canEditTaxDetails)
        {
            target.TaxId = Clean(source.TaxId);
            target.RegistrationNumber = Clean(source.RegistrationNumber);
            target.VatNumber = Clean(source.VatNumber);
        }
    }
}

/// <summary>The business units, active first.</summary>
public record GetBranchesQuery : IRequest<IReadOnlyList<BranchDto>>;

public class GetBranchesQueryHandler : IRequestHandler<GetBranchesQuery, IReadOnlyList<BranchDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetBranchesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<BranchDto>> Handle(GetBranchesQuery request, CancellationToken cancellationToken)
    {
        var branches = await _context.Branches
            .AsNoTracking()
            .OrderByDescending(b => b.IsActive)
            .ThenBy(b => b.Name)
            .ToListAsync(cancellationToken);

        var counts = await _context.Projects
            .AsNoTracking()
            .Where(p => p.BranchId != null)
            .GroupBy(p => p.BranchId!.Value)
            .Select(g => new { BranchId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.BranchId, g => g.Count, cancellationToken);

        // People employed now: open periods of employees that still exist.
        var employeeCounts = await _context.EmployeeBranches
            .AsNoTracking()
            .Where(p => p.EndDate == null && _context.Employees.Any(e => e.Id == p.EmployeeId))
            .GroupBy(p => p.BranchId)
            .Select(g => new { BranchId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.BranchId, g => g.Count, cancellationToken);

        var (details, tax) = await BranchView.AccessAsync(_context, _currentUserService, cancellationToken);

        var headIds = branches.Where(b => b.HeadEmployeeId != null).Select(b => b.HeadEmployeeId!.Value).Distinct().ToList();
        var heads = await _context.Employees
            .AsNoTracking()
            .Where(e => headIds.Contains(e.Id))
            .Select(e => new { e.Id, Name = e.FirstName + " " + e.LastName })
            .ToDictionaryAsync(e => e.Id, e => e.Name, cancellationToken);

        return branches
            .Select(b => BranchView.Map(
                b,
                counts.GetValueOrDefault(b.Id),
                details,
                tax,
                employeeCounts.GetValueOrDefault(b.Id),
                b.HeadEmployeeId is { } h ? heads.GetValueOrDefault(h) : null))
            .ToList();
    }
}

/// <summary>Adds a business unit.</summary>
public record CreateBranchCommand : BranchCommandBase, IRequest<BranchDto>;

public class CreateBranchCommandValidator : BranchCommandBaseValidator<CreateBranchCommand>;

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

        var branch = new Branch();
        BranchFieldMapper.Apply(branch, request, CustomerRules.CanEditTaxDetails(_currentUserService.Role));
        await BranchLookup.EnsureHeadAsync(_context, request.HeadEmployeeId, cancellationToken);
        branch.HeadEmployeeId = request.HeadEmployeeId;

        _context.Branches.Add(branch);
        await BranchTree.PlaceAsync(_context, branch, request.ParentBranchId, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return await BranchView.ForCallerAsync(_context, _currentUserService, branch, cancellationToken);
    }
}

/// <summary>Changes a business unit's name, colour and details, or switches it off or on.</summary>
public record UpdateBranchCommand : BranchCommandBase, IRequest<BranchDto>
{
    public Guid Id { get; init; }

    public bool IsActive { get; init; } = true;
}

public class UpdateBranchCommandValidator : BranchCommandBaseValidator<UpdateBranchCommand>
{
    public UpdateBranchCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
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

        BranchFieldMapper.Apply(branch, request, CustomerRules.CanEditTaxDetails(_currentUserService.Role));
        branch.IsActive = request.IsActive;
        await BranchLookup.EnsureHeadAsync(_context, request.HeadEmployeeId, cancellationToken);
        branch.HeadEmployeeId = request.HeadEmployeeId;
        await BranchTree.PlaceAsync(_context, branch, request.ParentBranchId, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return await BranchView.ForCallerAsync(_context, _currentUserService, branch, cancellationToken);
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

        if (await _context.Branches.AnyAsync(b => b.ParentBranchId == branch.Id, cancellationToken))
        {
            throw new ConflictException("This business unit has sub-units. Move or remove them first.");
        }

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

        return await BranchView.ForCallerAsync(_context, _currentUserService, branch, cancellationToken);
    }
}
