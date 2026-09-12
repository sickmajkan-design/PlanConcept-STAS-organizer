using System.Text.Json;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Dashboard.Models;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Dashboard.Commands;

/// <summary>
/// Replaces the signed-in user's dashboard layout wholesale — creating the
/// row the first time they save, same singleton-per-user upsert shape as
/// <c>UpdateCompanySettingsCommand</c> uses for its one shared row.
/// </summary>
public record SaveDashboardLayoutCommand : IRequest<DashboardLayoutDto>
{
    public IReadOnlyList<DashboardWidgetDto> Widgets { get; init; } = [];
}

public class SaveDashboardLayoutCommandValidator : AbstractValidator<SaveDashboardLayoutCommand>
{
    public SaveDashboardLayoutCommandValidator()
    {
        RuleForEach(x => x.Widgets).ChildRules(widget =>
        {
            widget.RuleFor(w => w.Type)
                .Must(type => DashboardWidgetTypes.All.Contains(type))
                .WithMessage("Unknown widget type.");
        });
    }
}

public class SaveDashboardLayoutCommandHandler
    : IRequestHandler<SaveDashboardLayoutCommand, DashboardLayoutDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SaveDashboardLayoutCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<DashboardLayoutDto> Handle(
        SaveDashboardLayoutCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new ForbiddenAccessException("No signed-in user.");

        var layout = await _context.DashboardLayouts
            .FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);

        if (layout is null)
        {
            layout = new DashboardLayout { UserId = userId };
            _context.DashboardLayouts.Add(layout);
        }

        layout.WidgetsJson = JsonSerializer.Serialize(request.Widgets);

        await _context.SaveChangesAsync(cancellationToken);

        return new DashboardLayoutDto { Widgets = request.Widgets };
    }
}
