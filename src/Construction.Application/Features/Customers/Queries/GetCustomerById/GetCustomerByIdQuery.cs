using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Customers.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Construction.Application.Features.Customers;

namespace Construction.Application.Features.Customers.Queries.GetCustomerById;

public record GetCustomerByIdQuery(Guid Id) : IRequest<CustomerDto>;

public class GetCustomerByIdQueryHandler : IRequestHandler<GetCustomerByIdQuery, CustomerDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetCustomerByIdQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<CustomerDto> Handle(
        GetCustomerByIdQuery request,
        CancellationToken cancellationToken)
    {
        var canViewTaxDetails = await CustomerRules.ResolveCanViewTaxDetailsAsync(
            _context, _currentUserService, cancellationToken);

        var customer = await _context.Customers
            .AsNoTracking()
            .Where(c => c.Id == request.Id)
            .Select(CustomerMapping.ProjectionFor(canViewTaxDetails))
            .FirstOrDefaultAsync(cancellationToken);

        return customer ?? throw new NotFoundException(nameof(Customer), request.Id);
    }
}
