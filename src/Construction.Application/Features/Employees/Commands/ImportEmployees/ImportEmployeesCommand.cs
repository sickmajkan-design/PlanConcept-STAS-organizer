using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Employees.Commands.ImportEmployees;

/// <summary>One line of the file, already read into plain text by the client.</summary>
public record ImportEmployeeRow
{
    /// <summary>1-based line in the source file, so an error can say where.</summary>
    public int Line { get; init; }

    public string? EmployeeNumber { get; init; }

    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? Phone { get; init; }

    public string? Email { get; init; }

    public string? Address { get; init; }

    public string? Position { get; init; }

    /// <summary>ISO date (yyyy-MM-dd); the client normalises whatever the file used.</summary>
    public string? EmploymentDate { get; init; }

    public string? DateOfBirth { get; init; }

    /// <summary>"Employee" or "Subcontractor" — anything else is refused.</summary>
    public string? Type { get; init; }
}

public enum ImportDuplicateHandling
{
    /// <summary>Leave a person who already exists exactly as they are.</summary>
    Skip = 1,

    /// <summary>Fill in what the file has and the record lacks; never blank out an existing value.</summary>
    Update = 2,
}

public enum ImportRowOutcome
{
    Create = 1,
    Update = 2,
    Skip = 3,
    Error = 4,
}

public record ImportRowResult(
    int Line,
    ImportRowOutcome Outcome,
    string FullName,
    string? EmployeeNumber,
    string? Message);

public record ImportEmployeesResult(
    bool DryRun,
    int Created,
    int Updated,
    int Skipped,
    int Errors,
    IReadOnlyList<ImportRowResult> Rows);

/// <summary>
/// Creates or updates employees from a spreadsheet, in two steps: a dry run that
/// reports exactly what would happen, and the real run once the person has
/// looked at it.
/// </summary>
/// <remarks>
/// A row with an error never blocks the others — it is reported and left out.
/// The real run applies every good row in one save, so a failure halfway cannot
/// leave a half-imported file behind. A person already in the system is found by
/// employee number, then by email, then by full name; what to do with them is
/// the caller's choice, and the default is to leave them alone.
/// </remarks>
public record ImportEmployeesCommand : IRequest<ImportEmployeesResult>
{
    public IReadOnlyList<ImportEmployeeRow> Rows { get; init; } = [];

    public bool DryRun { get; init; } = true;

    public ImportDuplicateHandling OnDuplicate { get; init; } = ImportDuplicateHandling.Skip;
}

public class ImportEmployeesCommandValidator : AbstractValidator<ImportEmployeesCommand>
{
    public const int MaxRows = 1000;

    public ImportEmployeesCommandValidator()
    {
        RuleFor(x => x.Rows)
            .NotEmpty().WithMessage("The file has no rows to import.")
            .Must(rows => rows.Count <= MaxRows)
            .WithMessage($"At most {MaxRows} rows can be imported at once.");

        RuleFor(x => x.OnDuplicate).IsInEnum();
    }
}

public class ImportEmployeesCommandHandler : IRequestHandler<ImportEmployeesCommand, ImportEmployeesResult>
{
    private const string DefaultPosition = "Radnik";

    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ImportEmployeesCommandHandler(IApplicationDbContext context, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ImportEmployeesResult> Handle(
        ImportEmployeesCommand request,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);

        var existing = await _context.Employees.ToListAsync(cancellationToken);

        var byNumber = existing.ToDictionary(e => e.EmployeeNumber, StringComparer.OrdinalIgnoreCase);
        var byEmail = existing
            .Where(e => !string.IsNullOrWhiteSpace(e.Email))
            .GroupBy(e => e.Email!.ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First());
        var byName = existing
            .GroupBy(e => NameKey(e.FirstName, e.LastName))
            .ToDictionary(g => g.Key, g => g.First());

        var takenNumbers = new HashSet<string>(byNumber.Keys, StringComparer.OrdinalIgnoreCase);
        var results = new List<ImportRowResult>();
        var toAdd = new List<Employee>();
        var seenInFile = new HashSet<string>();

        foreach (var row in request.Rows)
        {
            var problem = Validate(row, today, out var parsed);
            var first = row.FirstName?.Trim() ?? string.Empty;
            var last = row.LastName?.Trim() ?? string.Empty;
            var fullName = $"{first} {last}".Trim();

            if (problem is not null)
            {
                results.Add(new ImportRowResult(row.Line, ImportRowOutcome.Error, fullName, row.EmployeeNumber, problem));
                continue;
            }

            // The same person twice in one file is a mistake in the file.
            var identity = NameKey(first, last);
            if (!seenInFile.Add(identity))
            {
                results.Add(new ImportRowResult(
                    row.Line, ImportRowOutcome.Skip, fullName, row.EmployeeNumber,
                    "Appears more than once in the file."));
                continue;
            }

            var match = FindExisting(row, byNumber, byEmail, byName);

            if (match is not null)
            {
                if (request.OnDuplicate == ImportDuplicateHandling.Skip)
                {
                    results.Add(new ImportRowResult(
                        row.Line, ImportRowOutcome.Skip, fullName, match.EmployeeNumber,
                        "Already exists."));
                    continue;
                }

                var changed = FillGaps(match, row, parsed);
                results.Add(new ImportRowResult(
                    row.Line,
                    changed ? ImportRowOutcome.Update : ImportRowOutcome.Skip,
                    fullName,
                    match.EmployeeNumber,
                    changed ? null : "Nothing new to add."));
                continue;
            }

            var number = string.IsNullOrWhiteSpace(row.EmployeeNumber)
                ? NextNumber(takenNumbers)
                : row.EmployeeNumber.Trim();

            if (!takenNumbers.Add(number))
            {
                results.Add(new ImportRowResult(
                    row.Line, ImportRowOutcome.Error, fullName, number,
                    $"Employee number '{number}' is already in use."));
                continue;
            }

            toAdd.Add(new Employee
            {
                EmployeeNumber = number,
                FirstName = first,
                LastName = last,
                Phone = Clean(row.Phone),
                Email = Clean(row.Email)?.ToLowerInvariant(),
                Address = Clean(row.Address),
                DateOfBirth = parsed.DateOfBirth,
                EmploymentDate = parsed.EmploymentDate ?? today,
                Position = Clean(row.Position) ?? DefaultPosition,
                Type = parsed.Type,
                Status = EmployeeStatus.Active,
            });

            results.Add(new ImportRowResult(row.Line, ImportRowOutcome.Create, fullName, number, null));
        }

        if (!request.DryRun)
        {
            _context.Employees.AddRange(toAdd);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new ImportEmployeesResult(
            request.DryRun,
            results.Count(r => r.Outcome == ImportRowOutcome.Create),
            results.Count(r => r.Outcome == ImportRowOutcome.Update),
            results.Count(r => r.Outcome == ImportRowOutcome.Skip),
            results.Count(r => r.Outcome == ImportRowOutcome.Error),
            results.OrderBy(r => r.Line).ToList());
    }

    private record Parsed(DateOnly? EmploymentDate, DateOnly? DateOfBirth, EmployeeType Type);

    private static string? Validate(ImportEmployeeRow row, DateOnly today, out Parsed parsed)
    {
        parsed = new Parsed(null, null, EmployeeType.Employee);

        if (string.IsNullOrWhiteSpace(row.FirstName)) return "First name is missing.";
        if (string.IsNullOrWhiteSpace(row.LastName)) return "Last name is missing.";
        if (row.FirstName.Trim().Length > 100 || row.LastName.Trim().Length > 100) return "Name is too long.";
        if ((row.EmployeeNumber?.Trim().Length ?? 0) > 32) return "Employee number is too long (32 characters at most).";
        if ((row.Phone?.Trim().Length ?? 0) > 32) return "Phone number is too long.";
        if ((row.Position?.Trim().Length ?? 0) > 128) return "Position is too long.";
        if ((row.Address?.Trim().Length ?? 0) > 512) return "Address is too long.";

        if (!string.IsNullOrWhiteSpace(row.Email))
        {
            var email = row.Email.Trim();
            var at = email.IndexOf('@');

            if (email.Length > 256 || at < 1 || at != email.LastIndexOf('@') || !email[(at + 1)..].Contains('.'))
            {
                return $"'{email}' is not a valid email address.";
            }
        }

        DateOnly? employment = null;
        DateOnly? birth = null;

        if (!string.IsNullOrWhiteSpace(row.EmploymentDate))
        {
            if (!DateOnly.TryParseExact(row.EmploymentDate.Trim(), "yyyy-MM-dd", out var d)) return "Employment date is not a valid date.";
            employment = d;
        }

        if (!string.IsNullOrWhiteSpace(row.DateOfBirth))
        {
            if (!DateOnly.TryParseExact(row.DateOfBirth.Trim(), "yyyy-MM-dd", out var d)) return "Date of birth is not a valid date.";
            if (d >= today) return "Date of birth must be in the past.";
            if (employment is { } e && d >= e) return "Date of birth must be before the employment date.";
            birth = d;
        }

        var type = EmployeeType.Employee;

        if (!string.IsNullOrWhiteSpace(row.Type) && !Enum.TryParse(row.Type.Trim(), ignoreCase: true, out type))
        {
            return $"Type '{row.Type.Trim()}' is not known (Employee or Subcontractor).";
        }

        parsed = new Parsed(employment, birth, type);
        return null;
    }

    private static Employee? FindExisting(
        ImportEmployeeRow row,
        Dictionary<string, Employee> byNumber,
        Dictionary<string, Employee> byEmail,
        Dictionary<string, Employee> byName)
    {
        if (!string.IsNullOrWhiteSpace(row.EmployeeNumber)
            && byNumber.TryGetValue(row.EmployeeNumber.Trim(), out var byNo))
        {
            return byNo;
        }

        if (!string.IsNullOrWhiteSpace(row.Email)
            && byEmail.TryGetValue(row.Email.Trim().ToLowerInvariant(), out var byMail))
        {
            return byMail;
        }

        return byName.TryGetValue(NameKey(row.FirstName!, row.LastName!), out var byFullName) ? byFullName : null;
    }

    /// <summary>Fills fields the record has no value for. Never overwrites and never blanks.</summary>
    private static bool FillGaps(Employee target, ImportEmployeeRow row, Parsed parsed)
    {
        var changed = false;

        void Fill(string? current, string? incoming, Action<string> set)
        {
            if (string.IsNullOrWhiteSpace(current) && !string.IsNullOrWhiteSpace(incoming))
            {
                set(incoming.Trim());
                changed = true;
            }
        }

        Fill(target.Phone, row.Phone, v => target.Phone = v);
        Fill(target.Email, row.Email, v => target.Email = v.ToLowerInvariant());
        Fill(target.Address, row.Address, v => target.Address = v);

        if (target.DateOfBirth is null && parsed.DateOfBirth is { } dob && dob < target.EmploymentDate)
        {
            target.DateOfBirth = dob;
            changed = true;
        }

        return changed;
    }

    private static string NameKey(string first, string last) =>
        $"{first.Trim().ToLowerInvariant()}|{last.Trim().ToLowerInvariant()}";

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>The next free "R-0001"-style number.</summary>
    private static string NextNumber(HashSet<string> taken)
    {
        var n = taken.Count + 1;

        while (true)
        {
            var candidate = $"R-{n:D4}";

            if (!taken.Contains(candidate))
            {
                return candidate;
            }

            n++;
        }
    }
}
