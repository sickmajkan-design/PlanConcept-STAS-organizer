using Construction.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Attachments.Queries.GetAttachmentTags;

/// <summary>
/// The labels already in use on documents, most used first, so a new upload can pick one instead of
/// spelling it a second way. Only the words, never the documents they sit on.
/// </summary>
public record GetAttachmentTagsQuery : IRequest<IReadOnlyList<string>>;

public class GetAttachmentTagsQueryHandler : IRequestHandler<GetAttachmentTagsQuery, IReadOnlyList<string>>
{
    private readonly IApplicationDbContext _context;

    public GetAttachmentTagsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<string>> Handle(GetAttachmentTagsQuery request, CancellationToken cancellationToken)
    {
        // Soft-deleted documents are already filtered out by the context. Counted in memory: the tag
        // arrays are short and the documents that carry any tag are a small share of the table.
        var arrays = await _context.Attachments
            .AsNoTracking()
            .Where(a => a.Tags.Length > 0)
            .Select(a => a.Tags)
            .ToListAsync(cancellationToken);

        return arrays
            .SelectMany(t => t)
            .GroupBy(t => t.ToLowerInvariant())
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .Select(g => g.First())
            .Take(100)
            .ToList();
    }
}
