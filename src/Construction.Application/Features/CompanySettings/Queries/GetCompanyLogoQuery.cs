using Construction.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.CompanySettings.Queries;

/// <summary>The logo's bytes, ready to stream. No authorization check — reached anonymously.</summary>
public record CompanyLogoContent(Stream Content, string ContentType);

public record GetCompanyLogoQuery(bool Raster = false) : IRequest<CompanyLogoContent?>;

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
            .Select(c => new { c.LogoStorageKey, c.LogoContentType, c.LogoRasterStorageKey })
            .FirstOrDefaultAsync(cancellationToken);

        if (settings?.LogoStorageKey is null)
        {
            return null;
        }

        if (request.Raster)
        {
            var picture = await Branches.LogoStorage.ReadPictureAsync(
                _storage, settings.LogoStorageKey, settings.LogoContentType, settings.LogoRasterStorageKey, cancellationToken);

            return picture is null ? null : new CompanyLogoContent(new MemoryStream(picture.Value.Bytes), picture.Value.ContentType);
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
