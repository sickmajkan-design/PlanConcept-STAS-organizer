using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs;
using Construction.Application.Features.PublicHolidays.Models;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.PublicHolidays.Commands.CreatePublicHoliday;

/// <summary>Adds a date to the holiday calendar.</summary>
public record CreatePublicHolidayCommand : IRequest<PublicHolidayDto>
{
    public DateOnly Date { get; init; }

    public string Name { get; init; } = null!;
}

public class CreatePublicHolidayCommandValidator : AbstractValidator<CreatePublicHolidayCommand>
{
    public CreatePublicHolidayCommandValidator()
    {
        RuleFor(x => x.Date).NotEqual(default(DateOnly)).WithMessage("A date is required.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public class CreatePublicHolidayCommandHandler
    : IRequestHandler<CreatePublicHolidayCommand, PublicHolidayDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreatePublicHolidayCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PublicHolidayDto> Handle(
        CreatePublicHolidayCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSetLabourRate(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not manage the holiday calendar.");
        }

        if (await _context.PublicHolidays.AnyAsync(h => h.Date == request.Date, cancellationToken))
        {
            throw new ConflictException("That date is already on the calendar.");
        }

        var holiday = new PublicHoliday
        {
            Date = request.Date,
            Name = request.Name.Trim(),
        };

        _context.PublicHolidays.Add(holiday);
        await _context.SaveChangesAsync(cancellationToken);

        return PublicHolidayMapping.ToDto(holiday);
    }
}
