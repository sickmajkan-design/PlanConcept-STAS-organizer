using AutoMapper;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Tools.Models;
using Construction.Domain.Entities;
using MediatR;

namespace Construction.Application.Features.Tools.Commands.CreateTool;

public record CreateToolCommand : ToolCommandBase, IRequest<ToolDto>;

public class CreateToolCommandValidator : ToolCommandBaseValidator<CreateToolCommand>;

public class CreateToolCommandHandler : IRequestHandler<CreateToolCommand, ToolDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public CreateToolCommandHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ToolDto> Handle(CreateToolCommand request, CancellationToken cancellationToken)
    {
        var serialNumber = string.IsNullOrWhiteSpace(request.SerialNumber)
            ? null
            : request.SerialNumber.Trim();
        var qrCode = string.IsNullOrWhiteSpace(request.QrCode) ? null : request.QrCode.Trim();

        await ToolRules.EnsureUniqueAsync(
            _context, serialNumber, qrCode, excludeToolId: null, cancellationToken);

        var tool = new Tool
        {
            Name = request.Name.Trim(),
            Category = request.Category?.Trim(),
            SerialNumber = serialNumber,
            QrCode = qrCode,
            Status = request.Status
        };

        _context.Tools.Add(tool);

        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ToolDto>(tool);
    }
}
