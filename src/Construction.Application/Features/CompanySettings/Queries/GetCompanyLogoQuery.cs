using Construction.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.CompanySettings.Queries;

/// <summary>The logo's bytes, ready to stream. No authorization check — reached anonymously.</summary>
public record CompanyLogoContent(Stream Content, string ContentType);

public record GetCompanyLogoQuery : IRequest<CompanyLogoContent?>;

public class GetCompanyLogoQueryHandler : IRequestHandler<GetCompanyLogoQuery, CompanyLogoContent?>
{
    private readonly IApplicationDbContext _context;
    private readonly IFileStorage _storage;

    public GetCompanyLogoQueryHandler(IApplicationDbContext context, IFileStorage storage)
    {
        _context = context;
        _storage = storage;
    }

    public async Task<CompanyLogoContent?> Handle(
        GetCompanyLogoQuery request,
        CancellationToken cancellationToken)
    {
        var settings = await _context.CompanySettings
            .AsNoTracking()
            .Where(c => c.LogoStorageKey != null)
            .Select(c => new { c.LogoStorageKey, c.LogoContentType })
            .FirstOrDefaultAsync(cancellationToken);

        if (settings?.LogoStorageKey is null)
        {
            return null;
        }

        var content = await _storage.OpenReadAsync(settings.LogoStorageKey, cancellationToken);

        if (content is null)
        {
            return null;
        }

        return new CompanyLogoContent(
            content,
            settings.LogoContentType ?? "application/octet-stream");
    }
}
