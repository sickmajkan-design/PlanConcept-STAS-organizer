using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Customers.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Construction.Application.Features.Customers;

namespace Construction.Application.Features.Customers.Commands.UpdateCustomer;

public record UpdateCustomerCommand : CustomerCommandBase, IRequest<CustomerDto>
{
    /// <summary>Set by the API layer from the route, never from the request body.</summary>
    public Guid Id { get; init; }
}

public class UpdateCustomerCommandValidator : CustomerCommandBaseValidator<UpdateCustomerCommand>;

public class UpdateCustomerCommandHandler : IRequestHandler<UpdateCustomerCommand, CustomerDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateCustomerCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<CustomerDto> Handle(
        UpdateCustomerCommand request,
        CancellationToken cancellationToken)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), request.Id);

        var canEditTaxDetails = CustomerRules.CanEditTaxDetails(_currentUserService.Role);

        customer.Name = request.Name.Trim();
        customer.ContactPerson = request.ContactPerson?.Trim();
        customer.Phone = request.Phone?.Trim();
        customer.Email = request.Email?.Trim();
        customer.Note = request.Note?.Trim();

        if (canEditTaxDetails)
        {
            customer.TaxId = request.TaxId?.Trim();
            customer.RegistrationNumber = request.RegistrationNumber?.Trim();
            customer.VatNumber = request.VatNumber?.Trim();
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.Customers
            .AsNoTracking()
            .Where(c => c.Id == customer.Id)
            .Select(CustomerMapping.ProjectionFor(canEditTaxDetails))
            .FirstAsync(cancellationToken);
    }
}
