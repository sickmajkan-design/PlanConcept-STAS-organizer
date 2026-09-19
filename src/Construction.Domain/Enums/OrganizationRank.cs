namespace Construction.Domain.Enums;

/// <summary>
/// Where someone sits on the company org chart. Purely a chart position —
/// unrelated to <see cref="UserRole"/>, which decides what somebody may do in
/// the system, and to <c>Employee.Position</c>, the free-text job title.
/// </summary>
/// <remarks>
/// Values are spaced by ten and run senior to junior, so sorting by value is
/// sorting by seniority, and a new title can be slotted between two existing
/// ones later without renumbering anything already stored.
/// </remarks>
public enum OrganizationRank
{
    Owner = 10,
    ExecutiveDirector = 20,
    Director = 30,
    DeputyDirector = 40,
    FinanceManager = 50,
    ProcurementManager = 60,
    LogisticsManager = 70,
    HrManager = 80,
    ProjectManager = 90,
    Foreman = 100,
    Worker = 110
}
