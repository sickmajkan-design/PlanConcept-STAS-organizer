using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.NotificationGroups.Models;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.NotificationGroups.Commands.CreateNotificationGroup;

/// <summary>Creates a named group and sets its full membership in one call.</summary>
public record CreateNotificationGroupCommand : IRequest<NotificationGroupDto>
{
    public string Name { get; init; } = null!;

    public List<Guid> EmployeeIds { get; init; } = [];
}

public class CreateNotificationGroupCommandValidator : AbstractValidator<CreateNotificationGroupCommand>
{
    public CreateNotificationGroupCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("A name is required.")
            .MaximumLength(128);
    }
}

public class CreateNotificationGroupCommandHandler
    : IRequestHandler<CreateNotificationGroupCommand, NotificationGroupDto>
{
    private readonly IApplicationDbContext _context;

    public CreateNotificationGroupCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationGroupDto> Handle(
        CreateNotificationGroupCommand request,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();

        var nameTaken = await _context.NotificationGroups
            .AnyAsync(g => g.Name.ToLower() == name.ToLower(), cancellationToken);

        if (nameTaken)
        {
            throw new ConflictException($"A group named '{name}' already exists.");
        }

        var employeeIds = request.EmployeeIds.Distinct().ToList();

        if (employeeIds.Count > 0)
        {
            var foundCount = await _context.Employees
                .CountAsync(e => employeeIds.Contains(e.Id), cancellationToken);

            if (foundCount != employeeIds.Count)
            {
                throw new NotFoundException(nameof(Employee), "one or more employee ids");
            }
        }

        var group = new NotificationGroup
        {
            Name = name,
            Members = employeeIds
                .Select(id => new NotificationGroupMember { EmployeeId = id })
                .ToList(),
        };

        _context.NotificationGroups.Add(group);

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.NotificationGroups
            .AsNoTracking()
            .Where(g => g.Id == group.Id)
            .Select(NotificationGroupMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
