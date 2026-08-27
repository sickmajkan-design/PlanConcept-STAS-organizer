using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.PublicHolidays.Commands.DeletePublicHoliday;

public record DeletePublicHolidayCommand(Guid Id) : IRequest;

public class DeletePublicHolidayCommandHandler : IRequestHandler<DeletePublicHolidayCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeletePublicHolidayCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeletePublicHolidayCommand request, CancellationToken cancellationToken)
    {
        if (!CostRules.CanSetLabourRate(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not manage the holiday calendar.");
        }

        var holiday = await _context.PublicHolidays
            .FirstOrDefaultAsync(h => h.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(PublicHoliday), request.Id);

        _context.PublicHolidays.Remove(holiday);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
