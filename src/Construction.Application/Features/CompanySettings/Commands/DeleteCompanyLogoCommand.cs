using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.CompanySettings.Commands;

public record DeleteCompanyLogoCommand : IRequest;

public class DeleteCompanyLogoCommandHandler : IRequestHandler<DeleteCompanyLogoCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorage _storage;

    public DeleteCompanyLogoCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IFileStorage storage)
    {
        _context = context;
        _currentUserService = currentUserService;
        _storage = storage;
    }

    public async Task Handle(DeleteCompanyLogoCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.Role is not UserRole.SuperAdmin)
        {
            throw new ForbiddenAccessException("Only a SuperAdmin may remove the company logo.");
        }

        await _storage.DeleteAsync(UploadCompanyLogoCommandHandler.StorageKey, cancellationToken);

        var settings = await _context.CompanySettings.FirstOrDefaultAsync(cancellationToken);

        if (settings is not null)
        {
            settings.LogoStorageKey = null;
            settings.LogoContentType = null;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
