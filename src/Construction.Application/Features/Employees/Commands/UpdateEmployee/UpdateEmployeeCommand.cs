using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Employees.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Employees.Commands.UpdateEmployee;

public record UpdateEmployeeCommand : EmployeeCommandBase, IRequest<EmployeeDto>
{
    /// <summary>Set by the API layer from the route, never from the request body.</summary>
    public Guid Id { get; init; }
}

public class UpdateEmployeeCommandValidator : EmployeeCommandBaseValidator<UpdateEmployeeCommand>;

public class UpdateEmployeeCommandHandler : IRequestHandler<UpdateEmployeeCommand, EmployeeDto>
{
    private readonly IApplicationDbContext _context;

    public UpdateEmployeeCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<EmployeeDto> Handle(
        UpdateEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Employee), request.Id);

        var employeeNumber = request.EmployeeNumber.Trim();

        var numberTaken = await _context.Employees
            .AnyAsync(e => e.EmployeeNumber == employeeNumber && e.Id != request.Id, cancellationToken);

        if (numberTaken)
        {
            throw new ConflictException($"Employee number '{employeeNumber}' is already in use.");
        }

        employee.EmployeeNumber = employeeNumber;
        employee.FirstName = request.FirstName.Trim();
        employee.LastName = request.LastName.Trim();
        employee.Phone = request.Phone?.Trim();
        employee.Email = request.Email?.Trim().ToLowerInvariant();
        employee.Address = request.Address?.Trim();
        employee.DateOfBirth = request.DateOfBirth;
        employee.EmploymentDate = request.EmploymentDate;
        employee.Position = request.Position.Trim();
        employee.Status = request.Status;

        await _context.SaveChangesAsync(cancellationToken);

        return EmployeeMapping.ToDto(employee);
    }
}
