using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.WeeklySiteReports.Commands.CreateWeeklySiteReport;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.WeeklySiteReports.Commands.UpdateWeeklySiteReportLines;

/// <summary>The office corrects the workers and hours of a report against the picture, without approving it yet.</summary>
public record UpdateWeeklySiteReportLinesCommand(Guid Id, IReadOnlyList<ReportLineInput> Lines) : IRequest;

public class UpdateWeeklySiteReportLinesCommandValidator : AbstractValidator<UpdateWeeklySiteReportLinesCommand>
{
    public UpdateWeeklySiteReportLinesCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Lines).Must(ReportLines.HaveNoDuplicates).WithMessage("The same worker is listed twice.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.EmployeeId).NotEmpty();
            line.RuleFor(l => l.Hours).InclusiveBetween(0m, CreateWeeklySiteReportCommandValidator.MaxWeekHours);
            line.RuleFor(l => l.HoursSecondMonth).GreaterThanOrEqualTo(0m);
            line.RuleFor(l => l).Must(l => l.HoursSecondMonth <= l.Hours)
                .WithMessage("More hours in the later month than in the whole week.");
        });
    }
}

public class UpdateWeeklySiteReportLinesCommandHandler : IRequestHandler<UpdateWeeklySiteReportLinesCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateWeeklySiteReportLinesCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateWeeklySiteReportLinesCommand request, CancellationToken cancellationToken)
    {
        var report = await _context.WeeklySiteReports
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(WeeklySiteReport), request.Id);

        if (report.Status == WeeklyReportStatus.Processed)
        {
            throw new ConflictException(
                "An approved report has already been entered in the monthly sheet. Send it back for correction to change it.");
        }

        await ReportLines.ReplaceAsync(_context, report, request.Lines, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
