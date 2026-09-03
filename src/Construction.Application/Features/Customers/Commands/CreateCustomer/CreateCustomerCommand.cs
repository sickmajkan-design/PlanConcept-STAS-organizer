using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Customers.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Customers.Commands.CreateCustomer;

public record CreateCustomerCommand : CustomerCommandBase, IRequest<CustomerDto>;

public class CreateCustomerCommandValidator : CustomerCommandBaseValidator<CreateCustomerCommand>;

public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, CustomerDto>
{
    private readonly IApplicationDbContext _context;

    public CreateCustomerCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CustomerDto> Handle(
        CreateCustomerCommand request,
        CancellationToken cancellationToken)
    {
        var customer = new Customer
        {
            Name = request.Name.Trim(),
            ContactPerson = request.ContactPerson?.Trim(),
            Phone = request.Phone?.Trim(),
            Email = request.Email?.Trim(),
            Note = request.Note?.Trim(),
        };

        _context.Customers.Add(customer);

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.Customers
            .AsNoTracking()
            .Where(c => c.Id == customer.Id)
            .Select(CustomerMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
