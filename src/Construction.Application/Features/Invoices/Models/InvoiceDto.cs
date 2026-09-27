using System.Linq.Expressions;
using Construction.Domain.Entities;

namespace Construction.Application.Features.Invoices.Models;

/// <summary>One company's part of an invoice.</summary>
public class InvoiceShareDto
{
    /// <summary>Null means the client itself.</summary>
    public Guid? CustomerCompanyId { get; init; }

    public string? CompanyName { get; init; }

    public decimal Amount { get; init; }
}

/// <summary>An issued invoice with its parts. Money: only served to the finance grant.</summary>
public class InvoiceDto
{
    public Guid Id { get; init; }

    public string Number { get; init; } = null!;

    public Guid ProjectId { get; init; }

    public string ProjectName { get; init; } = null!;

    public Guid? CustomerId { get; init; }

    public string? CustomerName { get; init; }

    public DateOnly IssueDate { get; init; }

    public DateOnly? DueDate { get; init; }

    public string? Description { get; init; }

    public decimal Amount { get; init; }

    public int PayrollYear { get; init; }

    public int PayrollMonth { get; init; }

    /// <summary>"Issued", "Paid" or "Cancelled".</summary>
    public string Status { get; init; } = null!;

    public string? CancelReason { get; init; }

    public DateTime CreatedAt { get; init; }

    public IReadOnlyList<InvoiceShareDto> Shares { get; init; } = [];
}

public static class InvoiceMapping
{
    public static readonly Expression<Func<Invoice, InvoiceDto>> Projection = i => new InvoiceDto
    {
        Id = i.Id,
        Number = i.Number,
        ProjectId = i.ProjectId,
        ProjectName = i.Project.Name,
        CustomerId = i.Project.CustomerId,
        CustomerName = i.Project.Customer != null ? i.Project.Customer.Name : null,
        IssueDate = i.IssueDate,
        DueDate = i.DueDate,
        Description = i.Description,
        Amount = i.Amount,
        PayrollYear = i.PayrollYear,
        PayrollMonth = i.PayrollMonth,
        Status = i.Status.ToString(),
        CancelReason = i.CancelReason,
        CreatedAt = i.CreatedAt,
        Shares = i.Shares
            .OrderBy(s => s.CustomerCompany != null ? s.CustomerCompany.Name : "")
            .Select(s => new InvoiceShareDto
            {
                CustomerCompanyId = s.CustomerCompanyId,
                CompanyName = s.CustomerCompany != null ? s.CustomerCompany.Name : null,
                Amount = s.Amount,
            })
            .ToList(),
    };
}
