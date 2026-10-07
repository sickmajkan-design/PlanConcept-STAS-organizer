using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// A certificate everybody posted to a project has to hold, such as work at height on a scaffolding site.
/// </summary>
/// <remarks>
/// Not enforced: the schedule warns when somebody is posted to a site without a valid one, and the
/// office decides. Matched to <see cref="EmployeeCertificate"/> by name, ignoring case and spaces.
/// </remarks>
public class ProjectCertificateRequirement : BaseEntity, IAuditable
{
    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public string Name { get; set; } = null!;
}
