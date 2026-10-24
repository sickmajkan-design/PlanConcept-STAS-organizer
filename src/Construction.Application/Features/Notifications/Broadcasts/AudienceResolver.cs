using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Branches;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Notifications.Broadcasts;

/// <summary>What one chip of the audience stands for.</summary>
public enum AudienceKind
{
    /// <summary>Everybody who works for the firm.</summary>
    Everyone = 1,

    /// <summary>One employee.</summary>
    Employee = 2,

    /// <summary>The members of a notification group.</summary>
    Group = 3,

    /// <summary>The crew posted to a site now.</summary>
    Project = 4,

    /// <summary>The people a business unit employs now, with the units under it.</summary>
    Branch = 5,

    /// <summary>All accounts with one role.</summary>
    Role = 6,
}

/// <summary>One choice of the audience. Several can be given: the people they name are the sum, each counted once.</summary>
public record AudienceTarget
{
    public AudienceKind Kind { get; init; }

    /// <summary>The employee, group, project or business unit chosen.</summary>
    public Guid? Id { get; init; }

    /// <summary>For <see cref="AudienceKind.Role"/>: the role.</summary>
    public UserRole? Role { get; init; }
}

/// <summary>A choice as it is shown: its name and how many people it stands for.</summary>
public record AudienceChip(AudienceKind Kind, Guid? Id, UserRole? Role, string Label, int Count);

public record AudiencePerson
{
    /// <summary>The employee, or null for an account that belongs to nobody on the payroll.</summary>
    public Guid? EmployeeId { get; init; }

    /// <summary>The account that would receive the notice; null when the person has none.</summary>
    public Guid? UserId { get; init; }

    public string Name { get; init; } = null!;

    public string Role { get; init; } = null!;

    public bool HasAccount => UserId is not null;

    /// <summary>The choices that reach this person.</summary>
    public IReadOnlyList<string> Via { get; init; } = [];
}

public record ResolvedAudience(IReadOnlyList<AudiencePerson> People, IReadOnlyList<AudienceChip> Chips)
{
    /// <summary>People who can be sent something: they have an account.</summary>
    public IReadOnlyList<AudiencePerson> Reachable => People.Where(p => p.HasAccount).ToList();

    public int WithoutAccount => People.Count(p => !p.HasAccount);
}

/// <summary>
/// Turns the choices of an announcement into the people they name. A person named by several choices is one person; one
/// who has no account is still listed, so the sender sees that nothing can reach them.
/// </summary>
public static class AudienceResolver
{
    public static string RoleLabel(UserRole role) => role switch
    {
        UserRole.SuperAdmin => "Super admin",
        UserRole.Admin => "Admin",
        UserRole.ProjectManager => "Voditelj projekta",
        UserRole.Foreman => "Poslovođa",
        UserRole.Worker => "Radnik",
        UserRole.Customer => "Kupac",
        _ => role.ToString(),
    };

    public static async Task<ResolvedAudience> ResolveAsync(
        IApplicationDbContext context,
        DateOnly today,
        IReadOnlyList<AudienceTarget> targets,
        CancellationToken cancellationToken)
    {
        // Every active account that is not a customer's: an announcement is for the firm's own people.
        var users = await context.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.Role != UserRole.Customer)
            .Select(u => new { u.Id, u.EmployeeId, u.Role, u.Email })
            .ToListAsync(cancellationToken);

        var userByEmployee = users
            .Where(u => u.EmployeeId != null)
            .GroupBy(u => u.EmployeeId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        // A person is the employee, or the account when it belongs to nobody on the payroll.
        static string KeyOfEmployee(Guid id) => $"e:{id:N}";
        static string KeyOfUser(Guid id) => $"u:{id:N}";

        var via = new Dictionary<string, List<string>>();
        var chips = new List<AudienceChip>();
        var employeeIdsSeen = new HashSet<Guid>();
        var userOnly = new Dictionary<string, (Guid UserId, string Email, UserRole Role)>();

        // The people a choice names, as keys: an employee, or an account that belongs to nobody on the payroll.
        void Collect(HashSet<string> keys, IEnumerable<Guid> employeeIds, IEnumerable<Guid> userIds)
        {
            foreach (var id in employeeIds)
            {
                keys.Add(KeyOfEmployee(id));
                employeeIdsSeen.Add(id);
            }

            foreach (var userId in userIds)
            {
                var user = users.First(u => u.Id == userId);

                if (user.EmployeeId is { } employeeId)
                {
                    keys.Add(KeyOfEmployee(employeeId));
                    employeeIdsSeen.Add(employeeId);
                }
                else
                {
                    var key = KeyOfUser(userId);
                    keys.Add(key);
                    userOnly[key] = (userId, user.Email, user.Role);
                }
            }
        }

        void Register(HashSet<string> keys, string label, AudienceTarget target)
        {
            foreach (var key in keys)
            {
                (via.TryGetValue(key, out var list) ? list : via[key] = []).Add(label);
            }

            chips.Add(new AudienceChip(target.Kind, target.Id, target.Role, label, keys.Count));
        }

        foreach (var target in targets)
        {
            var keys = new HashSet<string>();

            switch (target.Kind)
            {
                case AudienceKind.Everyone:
                {
                    var employees = await context.Employees
                        .AsNoTracking()
                        .Where(e => e.Status == EmployeeStatus.Active)
                        .Select(e => e.Id)
                        .ToListAsync(cancellationToken);

                    Collect(keys, employees, users.Select(u => u.Id));
                    Register(keys, "Svi korisnici", target);
                    break;
                }

                case AudienceKind.Employee when target.Id is { } employeeId:
                {
                    var name = await context.Employees
                        .AsNoTracking()
                        .Where(e => e.Id == employeeId)
                        .Select(e => e.FirstName + " " + e.LastName)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (name is not null)
                    {
                        Collect(keys, [employeeId], []);
                        Register(keys, $"Radnik: {name}", target);
                    }

                    break;
                }

                case AudienceKind.Group when target.Id is { } groupId:
                {
                    var group = await context.NotificationGroups
                        .AsNoTracking()
                        .Where(g => g.Id == groupId)
                        .Select(g => new { g.Name })
                        .FirstOrDefaultAsync(cancellationToken);

                    if (group is not null)
                    {
                        var members = await context.NotificationGroupMembers
                            .AsNoTracking()
                            .Where(m => m.NotificationGroupId == groupId)
                            .Select(m => m.EmployeeId)
                            .ToListAsync(cancellationToken);

                        Collect(keys, members, []);
                        Register(keys, $"Grupa: {group.Name}", target);
                    }

                    break;
                }

                case AudienceKind.Project when target.Id is { } projectId:
                {
                    var projectName = await context.Projects
                        .AsNoTracking()
                        .Where(p => p.Id == projectId)
                        .Select(p => p.Name)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (projectName is not null)
                    {
                        var crew = await context.EmployeeProjects
                            .AsNoTracking()
                            .Where(a => a.ProjectId == projectId
                                && a.StartDate <= today
                                && (a.EndDate == null || a.EndDate >= today))
                            .Select(a => a.EmployeeId)
                            .ToListAsync(cancellationToken);

                        Collect(keys, crew, []);
                        Register(keys, $"Projekat: {projectName}", target);
                    }

                    break;
                }

                case AudienceKind.Branch when target.Id is { } branchId:
                {
                    var branchName = await context.Branches
                        .AsNoTracking()
                        .Where(b => b.Id == branchId)
                        .Select(b => b.Name)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (branchName is not null)
                    {
                        var token = BranchTree.Token(branchId);
                        var staff = await context.EmployeeBranches
                            .AsNoTracking()
                            .Where(p => p.EndDate == null && p.Branch.Path.Contains(token))
                            .Select(p => p.EmployeeId)
                            .ToListAsync(cancellationToken);

                        Collect(keys, staff, []);
                        Register(keys, $"Jedinica: {branchName}", target);
                    }

                    break;
                }

                case AudienceKind.Role when target.Role is { } role:
                    Collect(keys, [], users.Where(u => u.Role == role).Select(u => u.Id));
                    Register(keys, $"Uloga: {RoleLabel(role)}", target);
                    break;
            }
        }

        var employeeIds = employeeIdsSeen.ToList();

        var employees2 = await context.Employees
            .AsNoTracking()
            .Where(e => employeeIds.Contains(e.Id))
            .Select(e => new { e.Id, Name = e.FirstName + " " + e.LastName, e.Position })
            .ToListAsync(cancellationToken);

        var people = new List<AudiencePerson>();

        foreach (var employee in employees2)
        {
            var key = KeyOfEmployee(employee.Id);
            var user = userByEmployee.GetValueOrDefault(employee.Id);

            people.Add(new AudiencePerson
            {
                EmployeeId = employee.Id,
                UserId = user?.Id,
                Name = employee.Name,
                Role = user is null ? employee.Position : RoleLabel(user.Role),
                Via = via[key],
            });
        }

        foreach (var (key, info) in userOnly)
        {
            people.Add(new AudiencePerson
            {
                UserId = info.UserId,
                Name = info.Email,
                Role = RoleLabel(info.Role),
                Via = via[key],
            });
        }

        return new ResolvedAudience(
            people.OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase).ToList(),
            chips);
    }
}
