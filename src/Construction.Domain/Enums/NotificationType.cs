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
    BulletinPosted = 11,

    /// <summary>A change to an approved absence is waiting on this person to confirm or decline it.</summary>
    AbsenceEditProposed = 12,

    /// <summary>A site this person is assigned to has no weekly hours report yet for last week.</summary>
    WeeklyReportDue = 13,

    /// <summary>Someone on a foreman's site just started their shift.</summary>
    EmployeeClockedIn = 14,

    /// <summary>Someone on a foreman's site just ended their shift.</summary>
    EmployeeClockedOut = 15,

    /// <summary>Someone clocked in at a site they have no active posting to.</summary>
    UnassignedProjectClockIn = 16,

    /// <summary>A defect was reported at a site and nobody is assigned to it yet.</summary>
    DefectReported = 17,

    /// <summary>An employee asked for time off — waiting on someone to review it.</summary>
    AbsenceRequested = 18,

    /// <summary>A document's mandatory retention period has ended — it may now be deleted, on someone's own decision.</summary>
    DocumentRetentionEnded = 19,

    /// <summary>A free-typed message sent straight to one employee, rather than fired by a system event.</summary>
    DirectMessage = 20
}
