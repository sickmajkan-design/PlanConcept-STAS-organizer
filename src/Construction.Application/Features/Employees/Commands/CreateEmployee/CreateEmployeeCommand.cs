using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Employees.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Employees.Commands.CreateEmployee;

public record CreateEmployeeCommand : EmployeeCommandBase, IRequest<EmployeeDto>
{
    /// <summary>The business unit that employs them, from their employment date. Optional.</summary>
    public Guid? BranchId { get; init; }
}

public class CreateEmployeeCommandValidator : EmployeeCommandBaseValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
        RuleFor(x => x.ContractorCompany)
            .NotEmpty().WithMessage("The firm the subcontractor works for is required.")
            .When(x => x.Type == EmployeeType.Subcontractor);

        RuleFor(x => x.BranchId)
            .NotNull().WithMessage("The business unit the subcontractor works with is required.")
            .When(x => x.Type == EmployeeType.Subcontractor);
    }
}

public class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, EmployeeDto>
{
    private readonly IApplicationDbContext _context;

    public CreateEmployeeCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<EmployeeDto> Handle(
        CreateEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        var employeeNumber = request.EmployeeNumber.Trim();

        var numberTaken = await _context.Employees
            .AnyAsync(e => e.EmployeeNumber == employeeNumber, cancellationToken);

        if (numberTaken)
        {
            throw new ConflictException($"Employee number '{employeeNumber}' is already in use.");
        }

        var employee = new Employee
        {
            EmployeeNumber = employeeNumber,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Phone = request.Phone?.Trim(),
            Email = request.Email?.Trim().ToLowerInvariant(),
            Address = request.Address?.Trim(),
            DateOfBirth = request.DateOfBirth,
            EmploymentDate = request.EmploymentDate,
            Position = request.Position.Trim(),
            Status = request.Status,
            Type = request.Type,
            ContractorCompany = request.Type == EmployeeType.Subcontractor ? request.ContractorCompany?.Trim() : null,
            EngagementEndDate = request.Type == EmployeeType.Subcontractor ? request.EngagementEndDate : null
        };

        var branch = await Branches.BranchLookup.LoadAsync(_context, request.BranchId, cancellationToken);

        _context.Employees.Add(employee);

        if (branch is not null)
        {
            // The unit is set as well as its id: the DTO is built from this object, not read back.
            employee.BranchPeriods.Add(new EmployeeBranch { BranchId = branch.Id, Branch = branch, StartDate = request.EmploymentDate });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return EmployeeMapping.ToDto(employee);
    }
}
