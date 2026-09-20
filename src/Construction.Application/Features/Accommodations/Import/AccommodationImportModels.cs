using Construction.Domain.Enums;

namespace Construction.Application.Features.Accommodations.Import;

public enum AccommodationImportRowStatus
{
    /// <summary>Everything in the row checks out; it will create an accommodation, a stay, or both.</summary>
    Ready,

    MissingAddress,
    InvalidType,
    InvalidNumber,
    InvalidDate,
    UnknownEmployee,
    UnknownProject,

    /// <summary>The person already lives somewhere else during those dates.</summary>
    StayConflict,

    /// <summary>The accommodation and the stay are already there; the row adds nothing.</summary>
    AlreadyImported
}

/// <summary>One row of the uploaded list, after reading and matching.</summary>
public class AccommodationImportRowResult
{
    /// <summary>1-based, counting the header row: what somebody looking at the file in Excel would call it.</summary>
    public int RowNumber { get; init; }

    public string? Address { get; init; }

    public string? Name { get; init; }

    public string? City { get; init; }

    public AccommodationType Type { get; init; } = AccommodationType.Apartment;

    public string? Floor { get; init; }

    public int? Rooms { get; init; }

    public int? Beds { get; init; }

    public decimal? MonthlyRent { get; init; }

    public string? EmployeeText { get; init; }

    public Guid? EmployeeId { get; init; }

    public string? EmployeeName { get; init; }

    public string? ProjectText { get; init; }

    public Guid? ProjectId { get; init; }

    public DateOnly? MoveIn { get; init; }

    public DateOnly? MoveOut { get; init; }

    /// <summary>No accommodation has this address yet, so the row creates one.</summary>
    public bool CreatesAccommodation { get; init; }

    public bool CreatesStay { get; init; }

    public AccommodationImportRowStatus Status { get; init; }

    public bool WillImport => Status == AccommodationImportRowStatus.Ready;
}

public class AccommodationImportPreviewDto
{
    public int TotalRows { get; init; }

    public int NewAccommodationCount { get; init; }

    public int NewStayCount { get; init; }

    public int ProblemCount { get; init; }

    public IReadOnlyList<AccommodationImportRowResult> Rows { get; init; } = [];
}

public class AccommodationImportResultDto
{
    public int TotalRows { get; init; }

    public int CreatedAccommodations { get; init; }

    public int CreatedStays { get; init; }

    public int SkippedCount { get; init; }

    public IReadOnlyList<AccommodationImportRowResult> Skipped { get; init; } = [];
}
