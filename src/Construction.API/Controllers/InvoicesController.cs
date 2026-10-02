using Construction.API.Authorization;
using Construction.API.Filters;
using Construction.Application.Common.Models;
using Construction.Application.Features.Invoices.Commands.ChangeInvoiceStatus;
using Construction.Application.Features.Invoices.Commands.CreateInvoice;
using Construction.Application.Features.Invoices.Models;
using Construction.Application.Features.Invoices.Queries.GetInvoiceDocument;
using Construction.Application.Features.Invoices.Queries.GetInvoices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

/// <summary>
/// The invoices the firm issued to its clients, recorded here and counted in the billing of the
/// payroll for sites billed by a fixed sum or by measured work.
/// </summary>
/// <remarks>
/// Money: management only, and only with the finance grant. The handlers enforce the grant; the
/// route policy is the office role.
/// </remarks>
public class InvoicesController : ApiControllerBase
{
    /// <summary>Lists invoices by site, client, company, payroll month and status.</summary>
    [HttpGet]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(typeof(PagedList<InvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedList<InvoiceDto>>> GetList(
        [FromQuery] GetInvoicesQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>
    /// The data of a printed copy of one recorded invoice, with the issuing business unit's details.
    /// </summary>
    [HttpGet("{id:guid}/document")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(typeof(InvoiceDocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvoiceDocumentDto>> GetDocument(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetInvoiceDocumentQuery(id), cancellationToken));
    }

    /// <summary>Records an invoice, on one or more companies of the client.</summary>
    [HttpPost]
    [Idempotent]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InvoiceDto>> Create(
        CreateInvoiceCommand command,
        CancellationToken cancellationToken)
    {
        var invoice = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetList), new { search = invoice.Number }, invoice);
    }

    /// <summary>Marks an issued invoice as paid.</summary>
    [HttpPost("{id:guid}/paid")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InvoiceDto>> MarkPaid(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new MarkInvoicePaidCommand(id), cancellationToken));
    }

    /// <summary>Cancels an invoice issued by mistake. A paid one is answered with a credit note instead.</summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InvoiceDto>> Cancel(
        Guid id,
        CancelInvoiceCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }
}
