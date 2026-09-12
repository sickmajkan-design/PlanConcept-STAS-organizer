using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.CompanySettings.Models;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.CompanySettings.Commands;

/// <summary>
/// Creates the singleton row the first time a SuperAdmin saves the profile,
/// updates it every time after. There is only ever one, so unlike every
/// other command in this codebase this one carries no id of its own.
/// </summary>
public record UpdateCompanySettingsCommand : IRequest<CompanySettingsDto>
{
    public string Name { get; init; } = null!;

    public string? Address { get; init; }

    public string? TaxId { get; init; }

    public string? RegistrationNumber { get; init; }

    public string? VatNumber { get; init; }

    public string? Phone { get; init; }

    public string? Email { get; init; }

    public string? WeeklyReportsForwardEmail { get; init; }
}

public class UpdateCompanySettingsCommandValidator : AbstractValidator<UpdateCompanySettingsCommand>
{
    public UpdateCompanySettingsCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Address).MaximumLength(500);

        RuleFor(x => x.TaxId).MaximumLength(64);
        RuleFor(x => x.RegistrationNumber).MaximumLength(64);
        RuleFor(x => x.VatNumber).MaximumLength(64);

        RuleFor(x => x.Phone).MaximumLength(32);

        RuleFor(x => x.Email)
            .MaximumLength(256)
            .EmailAddress().WithMessage("Not a valid email address.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.WeeklyReportsForwardEmail)
            .MaximumLength(256)
            .EmailAddress().WithMessage("Not a valid email address.")
            .When(x => !string.IsNullOrWhiteSpace(x.WeeklyReportsForwardEmail));
    }
}

public class UpdateCompanySettingsCommandHandler
    : IRequestHandler<UpdateCompanySettingsCommand, CompanySettingsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateCompanySettingsCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<CompanySettingsDto> Handle(
        UpdateCompanySettingsCommand request,
        CancellationToken cancellationToken)
    {
        // Belt and braces: the route is already SuperAdminOnly, but this
        // command entirely rewrites the platform's own branding, so the
        // handler checks too rather than trusting routing alone.
        if (_currentUserService.Role is not UserRole.SuperAdmin)
        {
            throw new ForbiddenAccessException("Only a SuperAdmin may edit the company profile.");
        }

        var settings = await _context.CompanySettings.FirstOrDefaultAsync(cancellationToken);

        if (settings is null)
        {
            settings = new Domain.Entities.CompanySettings();
            _context.CompanySettings.Add(settings);
        }

        settings.Name = request.Name.Trim();
        settings.Address = request.Address?.Trim();
        settings.TaxId = request.TaxId?.Trim();
        settings.RegistrationNumber = request.RegistrationNumber?.Trim();
        settings.VatNumber = request.VatNumber?.Trim();
        settings.Phone = request.Phone?.Trim();
        settings.Email = request.Email?.Trim();
        settings.WeeklyReportsForwardEmail = request.WeeklyReportsForwardEmail?.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.CompanySettings
            .AsNoTracking()
            .Where(c => c.Id == settings.Id)
            .Select(CompanySettingsMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
