using Construction.Application.Common;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Tools.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Tools.Kits;

/// <summary>
/// Makes copies of a tool or a case of tools. A copy starts free and in the stock room: no holder, no project, no
/// serial number (that belongs to the one physical piece) and none of the history. A case keeps what is inside it.
/// </summary>
public record CopyToolCommand : IRequest<IReadOnlyList<ToolDto>>
{
    public Guid ToolId { get; init; }

    public int Count { get; init; } = 1;

    /// <summary>The name of each copy. <c>{n}</c> becomes its number; without it the number is added at the end.</summary>
    public string NameTemplate { get; init; } = null!;

    public int StartNumber { get; init; } = 1;

    /// <summary>Digits the number is padded to: 3 gives 007.</summary>
    public int NumberWidth { get; init; } = 3;

    public bool CopyItems { get; init; } = true;

    public bool CopyPrices { get; init; } = true;

    public bool CopyBranch { get; init; }

    /// <summary>One QR code per copy, typed or scanned. Left out, each copy gets a new one.</summary>
    public IReadOnlyList<string>? QrCodes { get; init; }
}

public class CopyToolCommandValidator : AbstractValidator<CopyToolCommand>
{
    public CopyToolCommandValidator()
    {
        RuleFor(x => x.Count).InclusiveBetween(1, 50);
        RuleFor(x => x.NameTemplate).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StartNumber).InclusiveBetween(0, 999_999);
        RuleFor(x => x.NumberWidth).InclusiveBetween(0, 6);
        RuleFor(x => x.QrCodes)
            .Must((c, qr) => qr is null || qr.Count == c.Count)
            .WithMessage("Give one QR code for each copy, or none.");
    }
}

public class CopyToolCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CopyToolCommand, IReadOnlyList<ToolDto>>
{
    public async Task<IReadOnlyList<ToolDto>> Handle(CopyToolCommand request, CancellationToken cancellationToken)
    {
        var source = await context.Tools
            .Include(t => t.Branch)
            .FirstOrDefaultAsync(t => t.Id == request.ToolId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tool), request.ToolId);

        var items = source.IsKit && request.CopyItems
            ? await context.ToolKitItems.AsNoTracking()
                .Where(i => i.ToolId == source.Id)
                .OrderBy(i => i.SortOrder).ThenBy(i => i.CreatedAt)
                .ToListAsync(cancellationToken)
            : [];

        var typed = request.QrCodes?.Select(q => q.Trim()).ToList();

        if (typed is not null && (typed.Any(string.IsNullOrEmpty) || typed.Distinct().Count() != typed.Count))
        {
            throw new ConflictException("The QR codes have to be filled in and different from each other.");
        }

        var template = request.NameTemplate.Contains("{n}") ? request.NameTemplate : request.NameTemplate.TrimEnd() + " {n}";
        var created = new List<Tool>();

        for (var index = 0; index < request.Count; index++)
        {
            var name = template.Replace("{n}", (request.StartNumber + index).ToString().PadLeft(request.NumberWidth, '0')).Trim();

            if (await context.Tools.AnyAsync(t => t.Name == name, cancellationToken) || created.Any(t => t.Name == name))
            {
                throw new ConflictException($"A tool named '{name}' already exists.");
            }

            string qr;

            if (typed is not null)
            {
                qr = typed[index];

                if (await context.Tools.AnyAsync(t => t.QrCode == qr, cancellationToken))
                {
                    throw new ConflictException($"QR code '{qr}' is already in use.");
                }
            }
            else
            {
                var taken = created.Select(t => t.QrCode).ToHashSet();
                qr = await QrCodeGenerator.GenerateUniqueAsync(
                    "TL",
                    async (candidate, ct) => taken.Contains(candidate) || await context.Tools.AnyAsync(t => t.QrCode == candidate, ct),
                    cancellationToken);
            }

            var copy = new Tool
            {
                Name = name,
                Category = source.Category,
                OwnershipType = source.OwnershipType,
                IsKit = source.IsKit,
                QrCode = qr,
                Status = ToolStatus.Available,
                BranchId = request.CopyBranch ? source.BranchId : null,
            };

            var order = 0;

            foreach (var item in items)
            {
                copy.KitItems.Add(new ToolKitItem
                {
                    Name = item.Name,
                    Quantity = item.Quantity,
                    UnitPrice = request.CopyPrices ? item.UnitPrice : 0,
                    SortOrder = ++order,
                });
            }

            created.Add(copy);
        }

        context.Tools.AddRange(created);
        await context.SaveChangesAsync(cancellationToken);

        return created.Select(ToolMapping.ToDto).ToList();
    }
}
