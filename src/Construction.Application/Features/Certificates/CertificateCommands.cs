using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Planning;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Certificates;

public class CertificateDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    /// <summary>The last day it is valid. Null for one that does not expire.</summary>
    public DateOnly? ValidUntil { get; init; }

    public string? Note { get; init; }
}

/// <summary>A worker's certificates, by name.</summary>
public record GetEmployeeCertificatesQuery(Guid EmployeeId) : IRequest<List<CertificateDto>>;

public class GetEmployeeCertificatesQueryHandler : IRequestHandler<GetEmployeeCertificatesQuery, List<CertificateDto>>
{
    private readonly IApplicationDbContext _context;

    public GetEmployeeCertificatesQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<List<CertificateDto>> Handle(GetEmployeeCertificatesQuery request, CancellationToken cancellationToken)
    {
        if (!await _context.Employees.AnyAsync(e => e.Id == request.EmployeeId, cancellationToken))
        {
            throw new NotFoundException(nameof(Employee), request.EmployeeId);
        }

        return await _context.EmployeeCertificates
            .AsNoTracking()
            .Where(c => c.EmployeeId == request.EmployeeId)
            .OrderBy(c => c.Name)
            .Select(c => new CertificateDto { Id = c.Id, Name = c.Name, ValidUntil = c.ValidUntil, Note = c.Note })
            .ToListAsync(cancellationToken);
    }
}

/// <summary>Adds a certificate to a worker, or changes one they already have.</summary>
public record SaveEmployeeCertificateCommand : IRequest<CertificateDto>
{
    public Guid EmployeeId { get; init; }

    /// <summary>The certificate to change. Null adds a new one.</summary>
    public Guid? Id { get; init; }

    public string Name { get; init; } = null!;

    public DateOnly? ValidUntil { get; init; }

    public string? Note { get; init; }
}

public class SaveEmployeeCertificateCommandValidator : AbstractValidator<SaveEmployeeCertificateCommand>
{
    public SaveEmployeeCertificateCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Note).MaximumLength(200);
    }
}

public class SaveEmployeeCertificateCommandHandler : IRequestHandler<SaveEmployeeCertificateCommand, CertificateDto>
{
    private readonly IApplicationDbContext _context;

    public SaveEmployeeCertificateCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<CertificateDto> Handle(SaveEmployeeCertificateCommand request, CancellationToken cancellationToken)
    {
        if (!await _context.Employees.AnyAsync(e => e.Id == request.EmployeeId, cancellationToken))
        {
            throw new NotFoundException(nameof(Employee), request.EmployeeId);
        }

        var name = request.Name.Trim();
        var key = PositionKey.Of(name);

        var mine = await _context.EmployeeCertificates
            .Where(c => c.EmployeeId == request.EmployeeId)
            .ToListAsync(cancellationToken);

        if (mine.Any(c => c.Id != request.Id && PositionKey.Of(c.Name) == key))
        {
            throw new ConflictException($"This worker already has a certificate called '{name}'. Change that one instead.");
        }

        EmployeeCertificate certificate;

        if (request.Id is { } id)
        {
            certificate = mine.FirstOrDefault(c => c.Id == id)
                ?? throw new NotFoundException(nameof(EmployeeCertificate), id);
        }
        else
        {
            certificate = new EmployeeCertificate { EmployeeId = request.EmployeeId };
            _context.EmployeeCertificates.Add(certificate);
        }

        certificate.Name = name;
        certificate.ValidUntil = request.ValidUntil;
        certificate.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return new CertificateDto
        {
            Id = certificate.Id,
            Name = certificate.Name,
            ValidUntil = certificate.ValidUntil,
            Note = certificate.Note,
        };
    }
}

public record DeleteEmployeeCertificateCommand(Guid EmployeeId, Guid Id) : IRequest;

public class DeleteEmployeeCertificateCommandHandler : IRequestHandler<DeleteEmployeeCertificateCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteEmployeeCertificateCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(DeleteEmployeeCertificateCommand request, CancellationToken cancellationToken)
    {
        var certificate = await _context.EmployeeCertificates
            .FirstOrDefaultAsync(c => c.Id == request.Id && c.EmployeeId == request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException(nameof(EmployeeCertificate), request.Id);

        _context.EmployeeCertificates.Remove(certificate);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
