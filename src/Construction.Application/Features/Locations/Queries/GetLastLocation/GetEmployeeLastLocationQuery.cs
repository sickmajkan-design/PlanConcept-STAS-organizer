using AutoMapper;
using AutoMapper.QueryableExtensions;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Locations.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Locations.Queries.GetLastLocation;

/// <summary>Last known position of one employee.</summary>
public record GetEmployeeLastLocationQuery(Guid EmployeeId) : IRequest<EmployeeLocationDto>;

public class GetEmployeeLastLocationQueryHandler
    : IRequestHandler<GetEmployeeLastLocationQuery, EmployeeLocationDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetEmployeeLastLocationQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<EmployeeLocationDto> Handle(
        GetEmployeeLastLocationQuery request,
        CancellationToken cancellationToken)
    {
        var employeeExists = await _context.Employees
            .AnyAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (!employeeExists)
        {
            throw new NotFoundException(nameof(Employee), request.EmployeeId);
        }

        var location = await _context.LocationRecords
            .AsNoTracking()
            .Where(l => l.EmployeeId == request.EmployeeId)
            .OrderByDescending(l => l.Timestamp)
            .ProjectTo<EmployeeLocationDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        return location
            ?? throw new NotFoundException(
                $"No location has been reported yet for employee '{request.EmployeeId}'.");
    }
}
