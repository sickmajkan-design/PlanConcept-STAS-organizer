using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Authentication.Commands.UpdatePreferredLanguage;

/// <summary>
/// Records which language the current user reads the app in, so a push
/// notification — sent before the app is even open, unlike the in-app inbox —
/// can be rendered in that language instead of always English. The mobile
/// app calls this whenever its own in-app language switcher changes,
/// alongside applying the change locally.
/// </summary>
public record UpdatePreferredLanguageCommand : IRequest
{
    /// <summary>ISO 639-1 code — only the languages the app ships are accepted.</summary>
    public string LanguageCode { get; init; } = null!;
}

public class UpdatePreferredLanguageCommandValidator : AbstractValidator<UpdatePreferredLanguageCommand>
{
    /// <summary>Kept in step with the mobile app's own `supportedLocales`.</summary>
    public static readonly string[] SupportedLanguages = ["sr", "en"];

    public UpdatePreferredLanguageCommandValidator()
    {
        RuleFor(x => x.LanguageCode)
            .NotEmpty()
            .Must(code => SupportedLanguages.Contains(code))
            .WithMessage($"Language must be one of: {string.Join(", ", SupportedLanguages)}.");
    }
}

public class UpdatePreferredLanguageCommandHandler : IRequestHandler<UpdatePreferredLanguageCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdatePreferredLanguageCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(UpdatePreferredLanguageCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
                     ?? throw new UnauthorizedException("User is not authenticated.");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedException("User is not authenticated.");

        user.PreferredLanguage = request.LanguageCode;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
