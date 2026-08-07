using AutoMapper;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Employees.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Employees.Commands.CreateEmployee;

public record CreateEmployeeCommand : EmployeeCommandBase, IRequest<EmployeeDto>;

public class CreateEmployeeCommandValidator : EmployeeCommandBaseValidator<CreateEmployeeCommand>;

public class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, EmployeeDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public CreateEmployeeCommandHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
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
            Status = request.Status
        };

        _context.Employees.Add(employee);

        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<EmployeeDto>(employee);
    }
}
