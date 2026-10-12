using System.Linq.Expressions;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.Application.Features.Invoices.Models;

/// <summary>One company's part of an invoice.</summary>
public class InvoiceShareDto
{
    /// <summary>Null means the client itself.</summary>
    public Guid? CustomerCompanyId { get; init; }

    public string? CompanyName { get; init; }

    public decimal Amount { get; init; }
}

/// <summary>One part-payment a client made against an invoice.</summary>
public class InvoicePaymentDto
{
    public Guid Id { get; init; }

    public decimal Amount { get; init; }

    public DateOnly PaidOn { get; init; }

    /// <summary>"Bank" or "Cash", when it was recorded.</summary>
    public string? Method { get; init; }

    public string? Note { get; init; }
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

    /// <summary>The business unit the invoice's project belongs to — the unit that issues it.</summary>
    public Guid? BranchId { get; init; }

    public string? BranchName { get; init; }

    public string? BranchColor { get; init; }

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

    /// <summary>
    /// What the client has paid. For an invoice marked paid before part-payments existed, and so with no
    /// payment recorded, it is the whole amount.
    /// </summary>
    public decimal PaidAmount { get; init; }

    /// <summary>The day of the latest payment, or null when none has been recorded.</summary>
    public DateOnly? LastPaymentOn { get; init; }

    public IReadOnlyList<InvoicePaymentDto> Payments { get; init; } = [];

    /// <summary>What is still to be paid; zero for a paid or cancelled invoice.</summary>
    public decimal RemainingAmount =>
        Status == "Issued" ? Math.Max(0m, Amount - PaidAmount) : 0m;
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
        BranchId = i.Project.BranchId,
        BranchName = i.Project.Branch != null ? i.Project.Branch.Name : null,
        BranchColor = i.Project.Branch != null ? i.Project.Branch.Color : null,
        IssueDate = i.IssueDate,
        DueDate = i.DueDate,
        Description = i.Description,
        Amount = i.Amount,
        PayrollYear = i.PayrollYear,
        PayrollMonth = i.PayrollMonth,
        Status = i.Status.ToString(),
        CancelReason = i.CancelReason,
        CreatedAt = i.CreatedAt,
        PaidAmount = i.Status == InvoiceStatus.Paid && !i.Payments.Any()
            ? i.Amount
            : i.Payments.Sum(p => p.Amount),
        LastPaymentOn = i.Payments.Max(p => (DateOnly?)p.PaidOn),
        Payments = i.Payments
            .OrderBy(p => p.PaidOn)
            .ThenBy(p => p.CreatedAt)
            .Select(p => new InvoicePaymentDto
            {
                Id = p.Id,
                Amount = p.Amount,
                PaidOn = p.PaidOn,
                Method = p.Method == null ? null : p.Method.ToString(),
                Note = p.Note,
            })
            .ToList(),
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
