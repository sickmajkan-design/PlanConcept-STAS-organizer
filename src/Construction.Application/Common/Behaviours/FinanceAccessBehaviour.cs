using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Finance;
using MediatR;

namespace Construction.Application.Common.Behaviours;

/// <summary>Marks a command that only the full finance grant may send, so it is refused before its body is judged.</summary>
public interface IRequiresFullFinance;

/// <summary>
/// Checks the finance grant ahead of validation for <see cref="IRequiresFullFinance"/> commands. Without it a caller
/// with no right got a 400 naming the fields that were wrong, which tells them the shape of the request they may not send.
/// </summary>
public class FinanceAccessBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public FinanceAccessBehaviour(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is IRequiresFullFinance)
        {
            await FinanceRules.EnsureFullAsync(_context, _currentUserService, cancellationToken);
        }

        return await next();
    }
}
