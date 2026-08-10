using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Tools.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Tools.Queries.GetToolByQrCode;

/// <summary>
/// Looks a tool up by the value encoded in its QR label — the endpoint the
/// mobile app calls after scanning a tool on site.
/// </summary>
public record GetToolByQrCodeQuery(string QrCode) : IRequest<ToolDto>;

public class GetToolByQrCodeQueryValidator : AbstractValidator<GetToolByQrCodeQuery>
{
    public GetToolByQrCodeQueryValidator()
    {
        RuleFor(x => x.QrCode)
            .NotEmpty().WithMessage("QR code is required.")
            .MaximumLength(256);
    }
}

public class GetToolByQrCodeQueryHandler : IRequestHandler<GetToolByQrCodeQuery, ToolDto>
{
    private readonly IApplicationDbContext _context;

    public GetToolByQrCodeQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ToolDto> Handle(GetToolByQrCodeQuery request, CancellationToken cancellationToken)
    {
        var qrCode = request.QrCode.Trim();

        var tool = await _context.Tools
            .AsNoTracking()
            .Where(t => t.QrCode == qrCode)
            .Select(ToolMapping.Projection)
            .FirstOrDefaultAsync(cancellationToken);

        return tool ?? throw new NotFoundException($"No tool found for QR code '{qrCode}'.");
    }
}
