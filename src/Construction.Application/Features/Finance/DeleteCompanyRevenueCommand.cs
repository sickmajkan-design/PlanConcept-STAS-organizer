using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Finance;

public record DeleteCompanyRevenueCommand(Guid Id) : IRequest;

public class DeleteCompanyRevenueCommandHandler : IRequestHandler<DeleteCompanyRevenueCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteCompanyRevenueCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteCompanyRevenueCommand request, CancellationToken cancellationToken)
    {
        await FinanceRules.EnsureFullAsync(_context, _currentUserService, cancellationToken);

        var revenue = await _context.CompanyRevenues
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(CompanyRevenue), request.Id);

        _context.CompanyRevenues.Remove(revenue);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
