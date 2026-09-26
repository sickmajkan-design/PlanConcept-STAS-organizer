using System.Linq.Expressions;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.Application.Features.Refunds.Models;

public class RefundDto
{
    public Guid Id { get; init; }

    public Guid EmployeeId { get; init; }

    public string EmployeeName { get; init; } = null!;

    public Guid RequestedByUserId { get; init; }

    public Guid? ProjectId { get; init; }

    public string? ProjectName { get; init; }

    public decimal Amount { get; init; }

    public string Currency { get; init; } = null!;

    public DateOnly ExpenseDate { get; init; }

    public string Description { get; init; } = null!;

    public RefundStatus Status { get; init; }

    public string? ReviewedByName { get; init; }

    public DateTime? ReviewedAt { get; init; }

    public string? ReviewNote { get; init; }

    public int? PayrollYear { get; init; }

    public int? PayrollMonth { get; init; }

    public DateTime CreatedAt { get; init; }
}

/// <summary>One expression, used as the SELECT list. See <c>AbsenceMapping</c>.</summary>
public static class RefundMapping
{
    public static readonly Expression<Func<Refund, RefundDto>> Projection = refund =>
        new RefundDto
        {
            Id = refund.Id,
            EmployeeId = refund.EmployeeId,
            EmployeeName = refund.Employee.FirstName + " " + refund.Employee.LastName,
            RequestedByUserId = refund.RequestedByUserId,
            ProjectId = refund.ProjectId,
            ProjectName = refund.Project != null ? refund.Project.Name : null,
            Amount = refund.Amount,
            Currency = refund.Currency,
            ExpenseDate = refund.ExpenseDate,
            Description = refund.Description,
            Status = refund.Status,
            ReviewedByName = refund.ReviewedByUser != null ? refund.ReviewedByUser.Email : null,
            ReviewedAt = refund.ReviewedAt,
            ReviewNote = refund.ReviewNote,
            PayrollYear = refund.PayrollYear,
            PayrollMonth = refund.PayrollMonth,
            CreatedAt = refund.CreatedAt,
        };
}
