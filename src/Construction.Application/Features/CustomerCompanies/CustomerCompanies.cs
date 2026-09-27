using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.CustomerCompanies;

/// <summary>One legal entity of a client.</summary>
public class CustomerCompanyDto
{
    public Guid Id { get; init; }

    public Guid CustomerId { get; init; }

    public string Name { get; init; } = null!;

    public string? Address { get; init; }

    public bool IsActive { get; init; }

    /// <summary>How many invoices name this company. A company with any can only be switched off, not deleted.</summary>
    public int InvoiceCount { get; init; }
}

/// <summary>Who may manage a client's companies: management. The names are not money.</summary>
public static class CustomerCompanyRules
{
    public static void EnsureManagement(ICurrentUserService currentUserService)
    {
        if (currentUserService.Role is not (UserRole.SuperAdmin or UserRole.Admin))
        {
            throw new ForbiddenAccessException("Only management may change a client's companies.");
        }
    }
}

/// <summary>The companies of a client, active first.</summary>
public record GetCustomerCompaniesQuery(Guid CustomerId) : IRequest<IReadOnlyList<CustomerCompanyDto>>;

public class GetCustomerCompaniesQueryHandler
    : IRequestHandler<GetCustomerCompaniesQuery, IReadOnlyList<CustomerCompanyDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCustomerCompaniesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CustomerCompanyDto>> Handle(
        GetCustomerCompaniesQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.CustomerCompanies
            .AsNoTracking()
            .Where(c => c.CustomerId == request.CustomerId)
            .OrderByDescending(c => c.IsActive)
            .ThenBy(c => c.Name)
            .Select(c => new CustomerCompanyDto
            {
                Id = c.Id,
                CustomerId = c.CustomerId,
                Name = c.Name,
                Address = c.Address,
                IsActive = c.IsActive,
                InvoiceCount = _context.InvoiceShares.Count(s => s.CustomerCompanyId == c.Id),
            })
            .ToListAsync(cancellationToken);
    }
}

/// <summary>Adds a company to a client.</summary>
public record CreateCustomerCompanyCommand : IRequest<CustomerCompanyDto>
{
    public Guid CustomerId { get; init; }

    public string Name { get; init; } = null!;

    public string? Address { get; init; }
}

public class CreateCustomerCompanyCommandValidator : AbstractValidator<CreateCustomerCompanyCommand>
{
    public CreateCustomerCompanyCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().WithMessage("The company name is required.").MaximumLength(200);
        RuleFor(x => x.Address).MaximumLength(500);
    }
}

public class CreateCustomerCompanyCommandHandler : IRequestHandler<CreateCustomerCompanyCommand, CustomerCompanyDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateCustomerCompanyCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<CustomerCompanyDto> Handle(CreateCustomerCompanyCommand request, CancellationToken cancellationToken)
    {
        CustomerCompanyRules.EnsureManagement(_currentUserService);

        if (!await _context.Customers.AnyAsync(c => c.Id == request.CustomerId, cancellationToken))
        {
            throw new NotFoundException(nameof(Customer), request.CustomerId);
        }

        var name = request.Name.Trim();

        if (await _context.CustomerCompanies.AnyAsync(
                c => c.CustomerId == request.CustomerId && c.Name == name, cancellationToken))
        {
            throw new ConflictException($"This client already has a company named '{name}'.");
        }

        var company = new CustomerCompany
        {
            CustomerId = request.CustomerId,
            Name = name,
            Address = request.Address?.Trim(),
        };

        _context.CustomerCompanies.Add(company);
        await _context.SaveChangesAsync(cancellationToken);

        return new CustomerCompanyDto
        {
            Id = company.Id,
            CustomerId = company.CustomerId,
            Name = company.Name,
            Address = company.Address,
            IsActive = true,
        };
    }
}

/// <summary>Renames a company, changes its address, or switches it off or on.</summary>
public record UpdateCustomerCompanyCommand : IRequest<CustomerCompanyDto>
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public string? Address { get; init; }

    public bool IsActive { get; init; } = true;
}

public class UpdateCustomerCompanyCommandValidator : AbstractValidator<UpdateCustomerCompanyCommand>
{
    public UpdateCustomerCompanyCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().WithMessage("The company name is required.").MaximumLength(200);
        RuleFor(x => x.Address).MaximumLength(500);
    }
}

public class UpdateCustomerCompanyCommandHandler : IRequestHandler<UpdateCustomerCompanyCommand, CustomerCompanyDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateCustomerCompanyCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<CustomerCompanyDto> Handle(UpdateCustomerCompanyCommand request, CancellationToken cancellationToken)
    {
        CustomerCompanyRules.EnsureManagement(_currentUserService);

        var company = await _context.CustomerCompanies.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(CustomerCompany), request.Id);

        var name = request.Name.Trim();

        if (await _context.CustomerCompanies.AnyAsync(
                c => c.CustomerId == company.CustomerId && c.Name == name && c.Id != company.Id, cancellationToken))
        {
            throw new ConflictException($"This client already has a company named '{name}'.");
        }

        company.Name = name;
        company.Address = request.Address?.Trim();
        company.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        return new CustomerCompanyDto
        {
            Id = company.Id,
            CustomerId = company.CustomerId,
            Name = company.Name,
            Address = company.Address,
            IsActive = company.IsActive,
            InvoiceCount = await _context.InvoiceShares.CountAsync(s => s.CustomerCompanyId == company.Id, cancellationToken),
        };
    }
}
