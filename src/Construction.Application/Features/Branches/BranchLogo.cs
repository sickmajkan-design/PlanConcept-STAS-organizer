using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Attachments;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Branches;

/// <summary>Which of a unit's two logos a request is about.</summary>
public enum BranchLogoVariant
{
    /// <summary>The normal logo, for a light background.</summary>
    Light,

    /// <summary>The optional logo for the dark theme.</summary>
    Dark,
}

public static class BranchLogoRules
{
    public const long MaxSizeBytes = LogoStorage.MaxSizeBytes;

    /// <summary>Png, jpg, webp and svg; HEIC is left out because browsers do not show it.</summary>
    public static string? ResolveContentType(string fileName) => LogoStorage.ResolveContentType(fileName);

    public static string StorageKey(Guid branchId, BranchLogoVariant variant) =>
        $"branches/{branchId}/logo-{(variant == BranchLogoVariant.Dark ? "dark" : "light")}";

    public static string RasterKey(Guid branchId) => $"branches/{branchId}/logo-light-raster";
}

/// <summary>Sets or replaces one of a unit's logos. Management only.</summary>
public record UploadBranchLogoCommand : IRequest<BranchDto>
{
    public Guid Id { get; init; }

    public BranchLogoVariant Variant { get; init; }

    public string FileName { get; init; } = null!;

    public long SizeBytes { get; init; }

    public Stream Content { get; init; } = Stream.Null;

    /// <summary>For an SVG, a png copy of the light logo, drawn by the panel. Optional.</summary>
    public Stream? RasterContent { get; init; }
}

public class UploadBranchLogoCommandValidator : AbstractValidator<UploadBranchLogoCommand>
{
    public UploadBranchLogoCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("A file name is required.")
            .MaximumLength(512);

        RuleFor(x => x.FileName)
            .Must(name => BranchLogoRules.ResolveContentType(name) is not null)
            .WithMessage("The logo must be a png, jpg, webp or svg picture.")
            .When(x => !string.IsNullOrWhiteSpace(x.FileName));

        RuleFor(x => x.SizeBytes)
            .GreaterThan(0).WithMessage("The file is empty.")
            .LessThanOrEqualTo(BranchLogoRules.MaxSizeBytes)
            .WithMessage($"The file is larger than the {BranchLogoRules.MaxSizeBytes / (1024 * 1024)} MB limit.");
    }
}

public class UploadBranchLogoCommandHandler : IRequestHandler<UploadBranchLogoCommand, BranchDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorage _storage;

    public UploadBranchLogoCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IFileStorage storage)
    {
        _context = context;
        _currentUserService = currentUserService;
        _storage = storage;
    }

    public async Task<BranchDto> Handle(UploadBranchLogoCommand request, CancellationToken cancellationToken)
    {
        BranchRules.EnsureManagement(_currentUserService);

        var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Branch), request.Id);

        // Saving under the same key replaces the old picture; an SVG is cleaned on the way.
        var key = BranchLogoRules.StorageKey(branch.Id, request.Variant);
        var stored = await LogoStorage.SaveAsync(
            _storage,
            key,
            BranchLogoRules.RasterKey(branch.Id),
            request.FileName,
            request.Content,
            request.Variant == BranchLogoVariant.Light ? request.RasterContent : null,
            cancellationToken);

        if (request.Variant == BranchLogoVariant.Dark)
        {
            branch.LogoDarkStorageKey = stored.Key;
            branch.LogoDarkContentType = stored.ContentType;
        }
        else
        {
            branch.LogoStorageKey = stored.Key;
            branch.LogoContentType = stored.ContentType;
            branch.LogoRasterStorageKey = stored.RasterKey;
        }

        branch.LogoUpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return await BranchView.ForCallerAsync(_context, _currentUserService, branch, cancellationToken);
    }
}

/// <summary>Removes one of a unit's logos. Management only.</summary>
public record DeleteBranchLogoCommand(Guid Id, BranchLogoVariant Variant) : IRequest<BranchDto>;

public class DeleteBranchLogoCommandHandler : IRequestHandler<DeleteBranchLogoCommand, BranchDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorage _storage;

    public DeleteBranchLogoCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IFileStorage storage)
    {
        _context = context;
        _currentUserService = currentUserService;
        _storage = storage;
    }

    public async Task<BranchDto> Handle(DeleteBranchLogoCommand request, CancellationToken cancellationToken)
    {
        BranchRules.EnsureManagement(_currentUserService);

        var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Branch), request.Id);

        var key = BranchLogoRules.StorageKey(branch.Id, request.Variant);
        await _storage.DeleteAsync(key, cancellationToken);

        if (request.Variant == BranchLogoVariant.Dark)
        {
            branch.LogoDarkStorageKey = null;
            branch.LogoDarkContentType = null;
        }
        else
        {
            // Without the light logo the dark one has nothing to stand beside, so both go.
            if (branch.LogoDarkStorageKey is { } dark)
            {
                await _storage.DeleteAsync(dark, cancellationToken);
            }

            await _storage.DeleteAsync(BranchLogoRules.RasterKey(branch.Id), cancellationToken);

            branch.LogoStorageKey = null;
            branch.LogoContentType = null;
            branch.LogoRasterStorageKey = null;
            branch.LogoDarkStorageKey = null;
            branch.LogoDarkContentType = null;
        }

        branch.LogoUpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return await BranchView.ForCallerAsync(_context, _currentUserService, branch, cancellationToken);
    }
}

/// <summary>A logo's bytes, ready to stream.</summary>
public record BranchLogoContent(Stream Content, string ContentType);

/// <summary>
/// Reads a unit's logo. Reached without a token, like the company logo: a picture in the page cannot
/// send one, the address carries an unguessable id and a logo is printed on documents anyway.
/// A dark request falls back to the light logo when the unit has no dark one.
/// </summary>
public record GetBranchLogoQuery(Guid Id, BranchLogoVariant Variant, bool Raster = false) : IRequest<BranchLogoContent?>;

public class GetBranchLogoQueryHandler : IRequestHandler<GetBranchLogoQuery, BranchLogoContent?>
{
    private readonly IApplicationDbContext _context;
    private readonly IFileStorage _storage;

    public GetBranchLogoQueryHandler(IApplicationDbContext context, IFileStorage storage)
    {
        _context = context;
        _storage = storage;
    }

    public async Task<BranchLogoContent?> Handle(GetBranchLogoQuery request, CancellationToken cancellationToken)
    {
        var branch = await _context.Branches
            .AsNoTracking()
            .Where(b => b.Id == request.Id)
            .Select(b => new { b.LogoStorageKey, b.LogoContentType, b.LogoRasterStorageKey, b.LogoDarkStorageKey, b.LogoDarkContentType })
            .FirstOrDefaultAsync(cancellationToken);

        if (branch is null)
        {
            return null;
        }

        if (request.Raster)
        {
            // The picture a phone or a spreadsheet can draw: the logo itself, or the png copy of an SVG.
            var picture = await LogoStorage.ReadPictureAsync(
                _storage, branch.LogoStorageKey, branch.LogoContentType, branch.LogoRasterStorageKey, cancellationToken);

            return picture is null ? null : new BranchLogoContent(new MemoryStream(picture.Value.Bytes), picture.Value.ContentType);
        }

        var (key, contentType) = request.Variant == BranchLogoVariant.Dark && branch.LogoDarkStorageKey is not null
            ? (branch.LogoDarkStorageKey, branch.LogoDarkContentType)
            : (branch.LogoStorageKey, branch.LogoContentType);

        if (key is null)
        {
            return null;
        }

        var content = await _storage.OpenReadAsync(key, cancellationToken);

        return content is null ? null : new BranchLogoContent(content, contentType ?? "application/octet-stream");
    }
}

/// <summary>
/// Makes this unit the one whose logo stands for "all business units", and the previous one stops
/// being it. Management only.
/// </summary>
public record SetMainBranchCommand(Guid Id) : IRequest<IReadOnlyList<BranchDto>>;

public class SetMainBranchCommandHandler : IRequestHandler<SetMainBranchCommand, IReadOnlyList<BranchDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMediator _mediator;

    public SetMainBranchCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IMediator mediator)
    {
        _context = context;
        _currentUserService = currentUserService;
        _mediator = mediator;
    }

    public async Task<IReadOnlyList<BranchDto>> Handle(SetMainBranchCommand request, CancellationToken cancellationToken)
    {
        BranchRules.EnsureManagement(_currentUserService);

        var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Branch), request.Id);

        if (!branch.IsActive)
        {
            throw new ConflictException("A business unit that is switched off cannot be the main one.");
        }

        // The unique index allows one main unit, so the old one is cleared in its own save first.
        var others = await _context.Branches.Where(b => b.IsMain && b.Id != branch.Id).ToListAsync(cancellationToken);

        if (others.Count > 0)
        {
            foreach (var other in others)
            {
                other.IsMain = false;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        branch.IsMain = true;
        await _context.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetBranchesQuery(), cancellationToken);
    }
}
