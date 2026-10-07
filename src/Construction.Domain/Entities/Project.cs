using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

public class Project : BaseEntity, ISoftDeletable, IAuditable
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public Guid? CustomerId { get; set; }

    public Customer? Customer { get; set; }

    /// <summary>
    /// Set when this project is a sub-project of a Main project. Null means
    /// this project is itself a Main project. A project whose parent has a
    /// parent of its own is never created — hierarchy is only ever two levels
    /// deep, enforced in <c>CreateProjectCommand</c>/<c>UpdateProjectCommand</c>.
    /// </summary>
    public Guid? ParentProjectId { get; set; }

    public Project? ParentProject { get; set; }

    public ICollection<Project> SubProjects { get; set; } = new List<Project>();

    /// <summary>The operator's own business unit this project belongs to. Null means none is set.</summary>
    public Guid? BranchId { get; set; }

    public Branch? Branch { get; set; }

    public string? Address { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    /// <summary>
    /// ISO 3166-1 alpha-2 (e.g. "BA", "HR", "DE") — which country's public
    /// holiday calendar applies to shifts on this site. Null means none does:
    /// a shift here never gets the holiday hourly rate, regardless of what is
    /// on the calendar for any country, until someone sets this.
    /// </summary>
    public string? CountryCode { get; set; }

    /// <summary>
    /// The site's expected daily clock-in time, in UTC, when one is set.
    /// Used only to flag a time entry as clocked in outside the expected
    /// window — it is not a schedule and does not gate clock-in itself.
    /// </summary>
    public TimeOnly? ShiftStartTime { get; set; }

    /// <summary>
    /// Whether the crew works Saturdays here. Weekdays are always working days. Used by the planning
    /// board to decide which days an open-ended posting actually covers; it does not stop anyone
    /// from clocking in on a day off.
    /// </summary>
    public bool WorksSaturdays { get; set; }

    /// <summary>Whether the crew works Sundays here. See <see cref="WorksSaturdays"/>.</summary>
    public bool WorksSundays { get; set; }

    /// <summary>Whether this site's crew works on <paramref name="day"/>.</summary>
    public bool WorksOn(DateOnly day) => day.DayOfWeek switch
    {
        DayOfWeek.Saturday => WorksSaturdays,
        DayOfWeek.Sunday => WorksSundays,
        _ => true,
    };

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public ProjectStatus Status { get; set; } = ProjectStatus.Planned;

    /// <summary>
    /// How the client is billed: by the hour, a fixed sum, or measured work. For the last two
    /// the billing in the payroll is the sum of the invoices issued, not hours times a price.
    /// </summary>
    public ProjectBillingMode BillingMode { get; set; } = ProjectBillingMode.Hourly;

    /// <summary>The total agreed value of the contract, in the system's single currency.</summary>
    public decimal? ContractValue { get; set; }

    /// <summary>
    /// What the office plans to spend on the site, when it has set a figure —
    /// distinct from <see cref="ContractValue"/>, which is what the customer
    /// pays. Money the company holds back from anyone without the finance
    /// right, so it is deliberately absent from the project DTOs and read and
    /// written only through the finance endpoints.
    /// </summary>
    public decimal? Budget { get; set; }

    /// <summary>
    /// What spending is measured against when the office asks to be warned —
    /// the budget or the contract value. Null lets the budget win when there is
    /// one and the contract otherwise. Kept off the project DTOs with
    /// <see cref="Budget"/>.
    /// </summary>
    public BudgetAlertBasis? BudgetAlertBasis { get; set; }

    /// <summary>The share of the limit, in percent, at which to start warning. Null means the default.</summary>
    public int? BudgetWarnPercent { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public ICollection<EmployeeProject> EmployeeAssignments { get; set; } = new List<EmployeeProject>();

    /// <summary>How many people of each position the site needs. See <see cref="ProjectStaffingNeed"/>.</summary>
    public ICollection<ProjectStaffingNeed> StaffingNeeds { get; set; } = new List<ProjectStaffingNeed>();

    public ICollection<Tool> AssignedTools { get; set; } = new List<Tool>();

    public ICollection<Vehicle> AssignedVehicles { get; set; } = new List<Vehicle>();

    public ICollection<Material> Materials { get; set; } = new List<Material>();

    public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();

    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    public ICollection<WorkItem> WorkItems { get; set; } = new List<WorkItem>();

    public ICollection<MaterialMovement> MaterialMovements { get; set; } = new List<MaterialMovement>();

    public ICollection<FinanceEntry> FinanceEntries { get; set; } = new List<FinanceEntry>();

    public ICollection<ProjectRevenue> Revenues { get; set; } = new List<ProjectRevenue>();
}
