using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.CustomerCompanies;
using Construction.Domain.Entities;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Employees.Commands.SetEmployeeProjectCompany;

/// <summary>
/// Changes which of the client's companies an employee's current posting on a project is worked
/// for — B13's day-two action, once the posting itself already exists. Only ever touches the
/// open posting (the one <see cref="EmployeeProject.EndDate"/> is null on); a closed posting is
/// history, and history does not get reassigned to a different company after the fact.
/// </summary>
public record SetEmployeeProjectCompanyCommand(
    Guid EmployeeId,
    Guid ProjectId,
    Guid? CustomerCompanyId) : IRequest;

public class SetEmployeeProjectCompanyCommandValidator
    : AbstractValidator<SetEmployeeProjectCompanyCommand>
{
    public SetEmployeeProjectCompanyCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.ProjectId).NotEmpty();
    }
}

public class SetEmployeeProjectCompanyCommandHandler : IRequestHandler<SetEmployeeProjectCompanyCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SetEmployeeProjectCompanyCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(SetEmployeeProjectCompanyCommand request, CancellationToken cancellationToken)
    {
        CustomerCompanyRules.EnsureManagement(_currentUserService);

        var posting = await _context.EmployeeProjects
            .Include(ep => ep.Project)
            .FirstOrDefaultAsync(
                ep => ep.EmployeeId == request.EmployeeId
                    && ep.ProjectId == request.ProjectId
                    && ep.EndDate == null,
                cancellationToken)
            ?? throw new NotFoundException(nameof(EmployeeProject), request.EmployeeId);

        if (request.CustomerCompanyId is { } customerCompanyId)
        {
            var companyCustomerId = await _context.CustomerCompanies
                .Where(c => c.Id == customerCompanyId)
                .Select(c => (Guid?)c.CustomerId)
                .FirstOrDefaultAsync(cancellationToken);

            if (companyCustomerId is null)
            {
                throw new NotFoundException(nameof(CustomerCompany), customerCompanyId);
            }

            if (posting.Project.CustomerId != companyCustomerId)
            {
                throw new Construction.Application.Common.Exceptions.ValidationException(
                [
                    new ValidationFailure(
                        nameof(SetEmployeeProjectCompanyCommand.CustomerCompanyId),
                        "That company does not belong to this project's client.")
                ]);
            }
        }

        posting.CustomerCompanyId = request.CustomerCompanyId;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
