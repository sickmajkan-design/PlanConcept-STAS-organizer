using Construction.Application.Common.Interfaces;
using Construction.Application.Features.CompanySettings.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.CompanySettings.Queries;

/// <summary>
/// The full company profile. Any authenticated role may read it — writing
/// is what's SuperAdmin-gated, at the controller.
/// </summary>
public record GetCompanySettingsQuery : IRequest<CompanySettingsDto>;

public class GetCompanySettingsQueryHandler
    : IRequestHandler<GetCompanySettingsQuery, CompanySettingsDto>
{
    private readonly IApplicationDbContext _context;

    public GetCompanySettingsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CompanySettingsDto> Handle(
        GetCompanySettingsQuery request,
        CancellationToken cancellationToken)
    {
        // No row yet is a real, valid state — first run, nobody has
        // configured the profile — so the frontend gets an empty shape to
        // fill in rather than a 404 it would have to special-case.
        return await _context.CompanySettings
            .AsNoTracking()
            .Select(CompanySettingsMapping.Projection)
            .FirstOrDefaultAsync(cancellationToken)
            ?? CompanySettingsMapping.Empty;
    }
}
