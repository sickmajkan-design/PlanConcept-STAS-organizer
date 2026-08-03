namespace Construction.Domain.Enums;

public enum NotificationType
{
    ProjectAssigned = 1,
    EmployeeAssigned = 2,
    VehicleAssigned = 3,
    ToolAssigned = 4,
    GeneralAnnouncement = 5,

    /// <summary>A document is about to lapse, or already has.</summary>
    DocumentExpiring = 6
}
