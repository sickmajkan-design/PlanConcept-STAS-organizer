using Construction.Application.Common.Interfaces;
using Construction.Application.Features.CompanySettings.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.CompanySettings.Queries;

/// <summary>
/// What the pre-login screen needs. No authorization check at all — reached
/// through an <c>[AllowAnonymous]</c> route — so this must never select
/// anything beyond <see cref="PublicCompanyBrandingDto"/>'s two fields.
/// </summary>
public record GetPublicCompanyBrandingQuery : IRequest<PublicCompanyBrandingDto>;

public class GetPublicCompanyBrandingQueryHandler
    : IRequestHandler<GetPublicCompanyBrandingQuery, PublicCompanyBrandingDto>
{
    private readonly IApplicationDbContext _context;

    public GetPublicCompanyBrandingQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PublicCompanyBrandingDto> Handle(
        GetPublicCompanyBrandingQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.CompanySettings
            .AsNoTracking()
            .Select(CompanySettingsMapping.BrandingProjection)
            .FirstOrDefaultAsync(cancellationToken)
            ?? CompanySettingsMapping.EmptyBranding;
    }
}
