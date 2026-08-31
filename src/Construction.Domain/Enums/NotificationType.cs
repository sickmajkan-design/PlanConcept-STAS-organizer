namespace Construction.Domain.Enums;

public enum NotificationType
{
    ProjectAssigned = 1,
    EmployeeAssigned = 2,
    VehicleAssigned = 3,
    ToolAssigned = 4,
    GeneralAnnouncement = 5,

    /// <summary>A document is about to lapse, or already has.</summary>
    DocumentExpiring = 6,

    TaskAssigned = 7,

    DefectAssigned = 8,

    /// <summary>Work is due soon, or already overdue.</summary>
    WorkItemDue = 9,

    /// <summary>A shift was closed by the nightly sweep, not by the employee.</summary>
    ShiftAutoClosed = 10,

    /// <summary>A new notice went up on the bulletin board.</summary>
    BulletinPosted = 11
}
