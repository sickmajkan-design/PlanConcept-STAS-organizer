using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Customers.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Construction.Application.Features.Customers;

namespace Construction.Application.Features.Customers.Commands.CreateCustomer;

public record CreateCustomerCommand : CustomerCommandBase, IRequest<CustomerDto>;

public class CreateCustomerCommandValidator : CustomerCommandBaseValidator<CreateCustomerCommand>;

public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, CustomerDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateCustomerCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<CustomerDto> Handle(
        CreateCustomerCommand request,
        CancellationToken cancellationToken)
    {
        var canEditTaxDetails = CustomerRules.CanEditTaxDetails(_currentUserService.Role);

        var customer = new Customer
        {
            Name = request.Name.Trim(),
            ContactPerson = request.ContactPerson?.Trim(),
            Phone = request.Phone?.Trim(),
            Email = request.Email?.Trim(),
            Note = request.Note?.Trim(),
            TaxId = canEditTaxDetails ? request.TaxId?.Trim() : null,
            RegistrationNumber = canEditTaxDetails ? request.RegistrationNumber?.Trim() : null,
            VatNumber = canEditTaxDetails ? request.VatNumber?.Trim() : null,
        };

        _context.Customers.Add(customer);

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.Customers
            .AsNoTracking()
            .Where(c => c.Id == customer.Id)
            .Select(CustomerMapping.ProjectionFor(canEditTaxDetails))
            .FirstAsync(cancellationToken);
    }
}
