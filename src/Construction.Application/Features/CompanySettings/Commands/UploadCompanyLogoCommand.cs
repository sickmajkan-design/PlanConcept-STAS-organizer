using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Attachments;
using Construction.Application.Features.CompanySettings.Models;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.CompanySettings.Commands;

/// <summary>
/// Replaces the company logo. Stored under a fixed, well-known key rather
/// than the generic <c>Attachment</c> system — there is only ever one logo,
/// and it must be readable with no auth token at all, which the generic
/// system deliberately never allows.
/// </summary>
public record UploadCompanyLogoCommand : IRequest<CompanySettingsDto>
{
    public string FileName { get; init; } = null!;

    public long SizeBytes { get; init; }

    public Stream Content { get; init; } = Stream.Null;
}

public class UploadCompanyLogoCommandValidator : AbstractValidator<UploadCompanyLogoCommand>
{
    public UploadCompanyLogoCommandValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("A file name is required.")
            .MaximumLength(512);

        RuleFor(x => x.FileName)
            .Must(name => ResolveImageContentType(name) is not null)
            .WithMessage("The logo must be an image (jpg, png, webp or heic).")
            .When(x => !string.IsNullOrWhiteSpace(x.FileName));

        RuleFor(x => x.SizeBytes)
            .GreaterThan(0).WithMessage("The file is empty.")
            .LessThanOrEqualTo(AttachmentRules.MaxSizeBytes)
            .WithMessage(
                $"The file is larger than the {AttachmentRules.MaxSizeBytes / (1024 * 1024)} MB limit.");
    }

    internal static string? ResolveImageContentType(string fileName)
    {
        var contentType = AttachmentRules.ResolveContentType(fileName);

        return contentType is not null && contentType.StartsWith("image/", StringComparison.Ordinal)
            ? contentType
            : null;
    }
}

public class UploadCompanyLogoCommandHandler
    : IRequestHandler<UploadCompanyLogoCommand, CompanySettingsDto>
{
    public const string StorageKey = "company/logo";

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorage _storage;

    public UploadCompanyLogoCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IFileStorage storage)
    {
        _context = context;
        _currentUserService = currentUserService;
        _storage = storage;
    }

    public async Task<CompanySettingsDto> Handle(
        UploadCompanyLogoCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.Role is not UserRole.SuperAdmin)
        {
            throw new ForbiddenAccessException("Only a SuperAdmin may change the company logo.");
        }

        var contentType = UploadCompanyLogoCommandValidator.ResolveImageContentType(request.FileName)
            ?? throw new ConflictException("The logo must be an image (jpg, png, webp or heic).");

        // SaveAsync already replaces whatever is under the key, so there is
        // nothing to delete first.
        await _storage.SaveAsync(StorageKey, request.Content, contentType, cancellationToken);

        var settings = await _context.CompanySettings.FirstOrDefaultAsync(cancellationToken);

        if (settings is null)
        {
            settings = new Domain.Entities.CompanySettings { Name = string.Empty };
            _context.CompanySettings.Add(settings);
        }

        settings.LogoStorageKey = StorageKey;
        settings.LogoContentType = contentType;

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.CompanySettings
            .AsNoTracking()
            .Where(c => c.Id == settings.Id)
            .Select(CompanySettingsMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
