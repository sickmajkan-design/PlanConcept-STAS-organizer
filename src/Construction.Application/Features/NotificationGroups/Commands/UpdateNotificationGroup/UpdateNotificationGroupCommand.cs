using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.NotificationGroups.Models;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.NotificationGroups.Commands.UpdateNotificationGroup;

/// <summary>Renames a group and replaces its membership wholesale.</summary>
public record UpdateNotificationGroupCommand : IRequest<NotificationGroupDto>
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public List<Guid> EmployeeIds { get; init; } = [];
}

public class UpdateNotificationGroupCommandValidator : AbstractValidator<UpdateNotificationGroupCommand>
{
    public UpdateNotificationGroupCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("A name is required.")
            .MaximumLength(128);
    }
}

public class UpdateNotificationGroupCommandHandler
    : IRequestHandler<UpdateNotificationGroupCommand, NotificationGroupDto>
{
    private readonly IApplicationDbContext _context;

    public UpdateNotificationGroupCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationGroupDto> Handle(
        UpdateNotificationGroupCommand request,
        CancellationToken cancellationToken)
    {
        var group = await _context.NotificationGroups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(NotificationGroup), request.Id);

        var name = request.Name.Trim();

        var nameTaken = await _context.NotificationGroups
            .AnyAsync(g => g.Id != request.Id && g.Name.ToLower() == name.ToLower(), cancellationToken);

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

        group.Name = name;
        group.Members.Clear();

        foreach (var employeeId in employeeIds)
        {
            group.Members.Add(new NotificationGroupMember { EmployeeId = employeeId });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.NotificationGroups
            .AsNoTracking()
            .Where(g => g.Id == group.Id)
            .Select(NotificationGroupMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
