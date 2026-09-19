namespace Construction.Application.Features.Materials.Import;

public enum MaterialImportRowStatus
{
    /// <summary>The material exists; the delivery will be recorded against it.</summary>
    Ready,

    /// <summary>No material has this name, but a unit was given, so it will be created.</summary>
    NewMaterial,

    MissingMaterialName,
    UnknownMaterialNoUnit,
    InvalidQuantity,
    InvalidPrice,
    MissingInvoiceNumber,
    InvalidDate,
    AlreadyImported
}

/// <summary>One row of the uploaded delivery list, after reading and matching.</summary>
public class MaterialImportRowResult
{
    /// <summary>1-based, counting the header row: what somebody looking at the file in Excel would call it.</summary>
    public int RowNumber { get; init; }

    public string? MaterialName { get; init; }

    public string? Unit { get; init; }

    public decimal? Quantity { get; init; }

    public decimal? UnitPrice { get; init; }

    public string? InvoiceNumber { get; init; }

    public string? Supplier { get; init; }

    public DateOnly? OccurredOn { get; init; }

    public string? Note { get; init; }

    public Guid? MaterialId { get; init; }

    public MaterialImportRowStatus Status { get; init; }

    public bool WillImport =>
        Status is MaterialImportRowStatus.Ready or MaterialImportRowStatus.NewMaterial;
}

public class MaterialImportPreviewDto
{
    public int TotalRows { get; init; }

    public int ReadyCount { get; init; }

    public int NewMaterialCount { get; init; }

    public int ProblemCount { get; init; }

    public IReadOnlyList<MaterialImportRowResult> Rows { get; init; } = [];
}

public class MaterialImportResultDto
{
    public int TotalRows { get; init; }

    public int CreatedDeliveries { get; init; }

    public int CreatedMaterials { get; init; }

    public int SkippedCount { get; init; }

    public IReadOnlyList<MaterialImportRowResult> Skipped { get; init; } = [];
}
