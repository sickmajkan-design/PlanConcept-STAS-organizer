using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Customers.Commands.DeleteCustomer;

/// <summary>Soft-deletes a customer. Refused while a project still points to it.</summary>
public record DeleteCustomerCommand(Guid Id) : IRequest;

public class DeleteCustomerCommandHandler : IRequestHandler<DeleteCustomerCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteCustomerCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), request.Id);

        var hasProjects = await _context.Projects
            .AnyAsync(p => p.CustomerId == request.Id, cancellationToken);

        if (hasProjects)
        {
            throw new ConflictException(
                "This customer still has projects. Reassign or remove them first.");
        }

        _context.Customers.Remove(customer);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
