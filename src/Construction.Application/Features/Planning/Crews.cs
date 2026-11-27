using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Planning;

public class CrewDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public Guid? LeaderEmployeeId { get; init; }

    public IReadOnlyList<CrewMemberDto> Members { get; init; } = [];
}

public class CrewMemberDto
{
    public Guid EmployeeId { get; init; }

    public string FullName { get; init; } = null!;

    public string Position { get; init; } = null!;
}

/// <summary>Crews are kept by the planners who move everybody; a foreman limited to one unit does not manage them.</summary>
internal static class CrewRules
{
    public static async Task EnsureCanManageAsync(IApplicationDbContext context, ICurrentUserService currentUser, CancellationToken cancellationToken)
    {
        var access = await PlanningRules.ResolveAsync(context, currentUser, cancellationToken);
        if (!access.CanEdit) throw new ForbiddenAccessException("You may not change the schedule.");
        PlanningRules.EnsureNotScoped(access);
    }
}

public record GetCrewsQuery : IRequest<IReadOnlyList<CrewDto>>;

public class GetCrewsQueryHandler : IRequestHandler<GetCrewsQuery, IReadOnlyList<CrewDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetCrewsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<CrewDto>> Handle(GetCrewsQuery request, CancellationToken cancellationToken)
    {
        PlanningRules.EnsureNotScoped(await PlanningRules.ResolveAsync(_context, _currentUser, cancellationToken));

        var crews = await _context.Crews.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CrewDto
            {
                Id = c.Id,
                Name = c.Name,
                LeaderEmployeeId = c.LeaderEmployeeId,
                Members = c.Members
                    .Where(m => m.Employee.Status == EmployeeStatus.Active)
                    .OrderBy(m => m.Employee.LastName).ThenBy(m => m.Employee.FirstName)
                    .Select(m => new CrewMemberDto { EmployeeId = m.EmployeeId, FullName = m.Employee.FirstName + " " + m.Employee.LastName, Position = m.Employee.Position })
                    .ToList(),
            })
            .ToListAsync(cancellationToken);

        return crews;
    }
}

/// <summary>Creates a crew, or replaces the name, leader and members of one.</summary>
public record SaveCrewCommand : IRequest<Guid>
{
    /// <summary>Null creates a new crew.</summary>
    public Guid? Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public Guid? LeaderEmployeeId { get; init; }

    public List<Guid> EmployeeIds { get; init; } = [];
}

public class SaveCrewCommandValidator : AbstractValidator<SaveCrewCommand>
{
    public SaveCrewCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.EmployeeIds).NotEmpty().WithMessage("A crew needs at least one member.");
        RuleFor(x => x.EmployeeIds).Must(ids => ids.Count <= 200).WithMessage("A crew can have at most 200 members.");
        RuleFor(x => x).Must(x => x.LeaderEmployeeId is null || x.EmployeeIds.Contains(x.LeaderEmployeeId.Value))
            .WithMessage("The leader has to be one of the members.")
            .OverridePropertyName(nameof(SaveCrewCommand.LeaderEmployeeId));
    }
}

public class SaveCrewCommandHandler : IRequestHandler<SaveCrewCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SaveCrewCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(SaveCrewCommand request, CancellationToken cancellationToken)
    {
        await CrewRules.EnsureCanManageAsync(_context, _currentUser, cancellationToken);

        var name = request.Name.Trim();
        var ids = request.EmployeeIds.Distinct().ToList();

        if (await _context.Crews.AnyAsync(c => c.Name == name && c.Id != request.Id, cancellationToken))
        {
            throw Fail(nameof(SaveCrewCommand.Name), "A crew with this name already exists.");
        }

        var found = await _context.Employees.AsNoTracking()
            .Where(e => ids.Contains(e.Id) && e.Status == EmployeeStatus.Active)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        if (found.Count != ids.Count)
        {
            throw Fail(nameof(SaveCrewCommand.EmployeeIds), "Only active employees can be in a crew.");
        }

        var elsewhere = await _context.CrewMembers.AsNoTracking()
            .Where(m => ids.Contains(m.EmployeeId) && m.CrewId != request.Id)
            .Select(m => m.Employee.FirstName + " " + m.Employee.LastName + " (" + m.Crew.Name + ")")
            .ToListAsync(cancellationToken);

        if (elsewhere.Count > 0)
        {
            throw Fail(nameof(SaveCrewCommand.EmployeeIds), $"Already in another crew: {string.Join(", ", elsewhere)}.");
        }

        Crew crew;

        if (request.Id is { } id)
        {
            crew = await _context.Crews.Include(c => c.Members).FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
                ?? throw new NotFoundException(nameof(Crew), id);

            foreach (var gone in crew.Members.Where(m => !ids.Contains(m.EmployeeId)).ToList())
            {
                _context.CrewMembers.Remove(gone);
            }
        }
        else
        {
            crew = new Crew { CreatedByUserId = _currentUser.UserId };
            _context.Crews.Add(crew);
        }

        crew.Name = name;
        crew.LeaderEmployeeId = request.LeaderEmployeeId;

        var have = crew.Members.Select(m => m.EmployeeId).ToHashSet();

        foreach (var employeeId in ids.Where(i => !have.Contains(i)))
        {
            _context.CrewMembers.Add(new CrewMember { Crew = crew, EmployeeId = employeeId });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return crew.Id;
    }

    private static Construction.Application.Common.Exceptions.ValidationException Fail(string property, string message) =>
        new([new ValidationFailure(property, message)]);
}

public record DeleteCrewCommand(Guid Id) : IRequest;

public class DeleteCrewCommandHandler : IRequestHandler<DeleteCrewCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DeleteCrewCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteCrewCommand request, CancellationToken cancellationToken)
    {
        await CrewRules.EnsureCanManageAsync(_context, _currentUser, cancellationToken);

        var crew = await _context.Crews.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Crew), request.Id);

        _context.Crews.Remove(crew);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Puts every member of a crew on a site for a range of days, as one move per member.</summary>
public record AssignCrewCommand : IRequest<int>
{
    public Guid CrewId { get; init; }

    /// <summary>The site. Null frees the whole crew for the range.</summary>
    public Guid? ProjectId { get; init; }

    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    /// <summary>Fill only the days each member has nothing on.</summary>
    public bool OnlyFreeDays { get; init; }
}

public class AssignCrewCommandValidator : AbstractValidator<AssignCrewCommand>
{
    public AssignCrewCommandValidator()
    {
        RuleFor(x => x.CrewId).NotEmpty();
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From).WithMessage("The end cannot be before the start.");
        RuleFor(x => x).Must(x => x.To.DayNumber - x.From.DayNumber < PlanningLimits.MaxDays)
            .WithMessage($"A planning action can cover at most {PlanningLimits.MaxDays} days.")
            .OverridePropertyName(nameof(AssignCrewCommand.To));
    }
}

public class AssignCrewCommandHandler : IRequestHandler<AssignCrewCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ISender _sender;

    public AssignCrewCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, ISender sender)
    {
        _context = context;
        _currentUser = currentUser;
        _sender = sender;
    }

    /// <returns>How many members were moved.</returns>
    public async Task<int> Handle(AssignCrewCommand request, CancellationToken cancellationToken)
    {
        await CrewRules.EnsureCanManageAsync(_context, _currentUser, cancellationToken);

        var members = await _context.CrewMembers.AsNoTracking()
            .Where(m => m.CrewId == request.CrewId && m.Employee.Status == EmployeeStatus.Active)
            .Select(m => m.EmployeeId)
            .ToListAsync(cancellationToken);

        if (members.Count == 0)
        {
            throw new NotFoundException(nameof(Crew), request.CrewId);
        }

        foreach (var employeeId in members)
        {
            await _sender.Send(
                new SetEmployeeScheduleCommand { EmployeeId = employeeId, ProjectId = request.ProjectId, From = request.From, To = request.To, OnlyFreeDays = request.OnlyFreeDays },
                cancellationToken);
        }

        return members.Count;
    }
}
