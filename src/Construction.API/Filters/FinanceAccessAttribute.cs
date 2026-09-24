using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Finance;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Construction.API.Filters;

/// <summary>
/// Refuses an endpoint to anyone who does not hold the finance right — the
/// grant a SuperAdmin gives an account, read from the database on each call.
/// </summary>
/// <remarks>
/// For the endpoints of a controller that is otherwise open to a wider role
/// (<c>CostsController</c> is <c>ForemanAndAbove</c>): the role policy still
/// runs first, this only narrows it. Put it on an action, not the controller,
/// so the field staff's own endpoints — the ones the mobile app records fuel
/// and rentals through — stay as they were.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class FinanceAccessAttribute : TypeFilterAttribute
{
    public FinanceAccessAttribute()
        : base(typeof(FinanceAccessFilter))
    {
    }
}

internal sealed class FinanceAccessFilter : IAsyncActionFilter
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public FinanceAccessFilter(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Thrown rather than returned, so the refusal has the same 403 shape as every other.
        await FinanceRules.EnsureFullAsync(_context, _currentUserService, context.HttpContext.RequestAborted);

        await next();
    }
}
