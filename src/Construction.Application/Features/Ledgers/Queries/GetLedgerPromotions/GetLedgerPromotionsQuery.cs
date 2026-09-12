using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Ledgers.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Ledgers.Queries.GetLedgerPromotions;

/// <summary>
/// Every row in this ledger that has been pushed through to a real General
/// Expense or Accommodation rate — the "promoted this month" overview, read
/// in one query rather than requiring every section to be opened to find
/// them one at a time.
/// </summary>
public record GetLedgerPromotionsQuery(Guid LedgerId) : IRequest<IReadOnlyList<LedgerPromotionDto>>;

public class GetLedgerPromotionsQueryHandler
    : IRequestHandler<GetLedgerPromotionsQuery, IReadOnlyList<LedgerPromotionDto>>
{
    private readonly IApplicationDbContext _context;

    public GetLedgerPromotionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<LedgerPromotionDto>> Handle(
        GetLedgerPromotionsQuery request,
        CancellationToken cancellationToken)
    {
        var generalExpenses = await _context.LedgerRows
            .AsNoTracking()
            .Where(r => r.Section.LedgerId == request.LedgerId && r.PromotedGeneralExpenseId != null)
            .Select(r => new LedgerPromotionDto
            {
                RowId = r.Id,
                RowLabel = r.Label,
                SectionName = r.Section.Name,
                Target = "GeneralExpense",
                TargetId = r.PromotedGeneralExpenseId!.Value,
                Amount = r.PromotedGeneralExpense!.Amount,
                OccurredOn = r.PromotedGeneralExpense!.OccurredOn,
            })
            .ToListAsync(cancellationToken);

        var accommodationRates = await _context.LedgerRows
            .AsNoTracking()
            .Where(r => r.Section.LedgerId == request.LedgerId && r.PromotedAccommodationRateId != null)
            .Select(r => new LedgerPromotionDto
            {
                RowId = r.Id,
                RowLabel = r.Label,
                SectionName = r.Section.Name,
                Target = "AccommodationRate",
                TargetId = r.PromotedAccommodationRateId!.Value,
                Amount = r.PromotedAccommodationRate!.MonthlyAmount,
                OccurredOn = r.PromotedAccommodationRate!.StartDate,
            })
            .ToListAsync(cancellationToken);

        return generalExpenses
            .Concat(accommodationRates)
            .OrderBy(p => p.SectionName)
            .ThenBy(p => p.RowLabel)
            .ToList();
    }
}
