using Construction.API.Authorization;
using Construction.API.Filters;
using Construction.Application.Common.Models;
using Construction.Application.Features.Book;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>
/// The book of money: entries, the money other tables already hold, and the categories they are filed under.
/// </summary>
/// <remarks>
/// As for <see cref="FinanceController"/> the route policy is only "internal staff"; who may read or
/// write is decided in the handlers (<see cref="BookRules"/>) from the finance grant on the account.
/// </remarks>
[Authorize(Policy = Policies.AllEmployees)]
public class BookController : ApiControllerBase
{
    /// <summary>The book: entries of its own and the money other tables hold, newest first.</summary>
    [HttpGet("entries")]
    [ProducesResponseType(typeof(PagedList<BookRowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedList<BookRowDto>>> GetEntries(
        [FromQuery] GetBookEntriesQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>What the whole filtered book adds up to: money in, and money out.</summary>
    [HttpGet("entries/totals")]
    [ProducesResponseType(typeof(BookTotalsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BookTotalsDto>> GetTotals(
        [FromQuery] GetBookTotalsQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Writes a line into the book.</summary>
    [HttpPost("entries")]
    [Idempotent]
    [ProducesResponseType(typeof(BookEntryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookEntryDto>> CreateEntry(
        CreateBookEntryCommand command,
        CancellationToken cancellationToken)
    {
        var entry = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetEntries), new { search = entry.Description }, entry);
    }

    /// <summary>Corrects an entry of the book. Lines read from other tables are changed where they were recorded.</summary>
    [HttpPut("entries/{id:guid}")]
    [ProducesResponseType(typeof(BookEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookEntryDto>> UpdateEntry(
        Guid id,
        UpdateBookEntryCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    /// <summary>Marks an income or a cost of the book as paid.</summary>
    [HttpPost("entries/{id:guid}/mark-paid")]
    [ProducesResponseType(typeof(BookEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookEntryDto>> MarkEntryPaid(
        Guid id,
        MarkBookEntryPaidCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    /// <summary>Removes an entry of the book (it is hidden, not erased).</summary>
    [HttpDelete("entries/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEntry(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteBookEntryCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>The category the rules would suggest for a description.</summary>
    [HttpGet("entries/suggest-category")]
    [ProducesResponseType(typeof(BookCategorySuggestionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BookCategorySuggestionDto>> SuggestCategory(
        [FromQuery] string? text,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new SuggestBookCategoryQuery(text), cancellationToken));
    }

    [HttpGet("categories")]
    [ProducesResponseType(typeof(IReadOnlyList<BookCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<BookCategoryDto>>> GetCategories(
        [FromQuery] GetBookCategoriesQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    [HttpPost("categories")]
    [ProducesResponseType(typeof(BookCategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookCategoryDto>> CreateCategory(
        CreateBookCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var category = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetCategories), null, category);
    }

    [HttpPut("categories/{id:guid}")]
    [ProducesResponseType(typeof(BookCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookCategoryDto>> UpdateCategory(
        Guid id,
        UpdateBookCategoryCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    [HttpGet("category-rules")]
    [ProducesResponseType(typeof(IReadOnlyList<BookCategoryRuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<BookCategoryRuleDto>>> GetCategoryRules(
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetBookCategoryRulesQuery(), cancellationToken));
    }

    [HttpPost("category-rules")]
    [ProducesResponseType(typeof(BookCategoryRuleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookCategoryRuleDto>> CreateCategoryRule(
        CreateBookCategoryRuleCommand command,
        CancellationToken cancellationToken)
    {
        var rule = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetCategoryRules), null, rule);
    }

    [HttpDelete("category-rules/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCategoryRule(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteBookCategoryRuleCommand(id), cancellationToken);
        return NoContent();
    }
}
