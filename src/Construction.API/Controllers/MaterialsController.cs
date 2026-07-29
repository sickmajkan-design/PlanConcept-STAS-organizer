using Construction.API.Authorization;
using Construction.Application.Common.Models;
using Construction.Application.Features.Materials.Commands.AdjustMaterialQuantity;
using Construction.Application.Features.Materials.Commands.CreateMaterial;
using Construction.Application.Features.Materials.Commands.DeleteMaterial;
using Construction.Application.Features.Materials.Commands.UpdateMaterial;
using Construction.Application.Features.Materials.Models;
using Construction.Application.Features.Materials.Queries.GetMaterialById;
using Construction.Application.Features.Materials.Queries.GetMaterials;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

public class MaterialsController : ApiControllerBase
{
    /// <summary>Lists materials with pagination, search, filtering and sorting.</summary>
    [HttpGet]
    [Authorize(Policy = Policies.ForemanAndAbove)]
    [ProducesResponseType(typeof(PagedList<MaterialDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedList<MaterialDto>>> GetList(
        [FromQuery] GetMaterialsQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Returns one material.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.ForemanAndAbove)]
    [ProducesResponseType(typeof(MaterialDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MaterialDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(new GetMaterialByIdQuery(id), cancellationToken));
    }

    /// <summary>Creates a new material.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.ProjectManagerAndAbove)]
    [ProducesResponseType(typeof(MaterialDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MaterialDto>> Create(
        CreateMaterialCommand command,
        CancellationToken cancellationToken)
    {
        var material = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = material.Id }, material);
    }

    /// <summary>Updates an existing material (absolute values, including quantity).</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.ProjectManagerAndAbove)]
    [ProducesResponseType(typeof(MaterialDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MaterialDto>> Update(
        Guid id,
        UpdateMaterialCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    /// <summary>
    /// Applies a relative stock movement (positive = received, negative =
    /// consumed). Refused with 409 when the stock would go negative.
    /// </summary>
    [HttpPost("{id:guid}/adjust")]
    [Authorize(Policy = Policies.ForemanAndAbove)]
    [ProducesResponseType(typeof(MaterialDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MaterialDto>> Adjust(
        Guid id,
        AdjustMaterialQuantityCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(command with { Id = id }, cancellationToken));
    }

    /// <summary>Soft-deletes a material.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteMaterialCommand(id), cancellationToken);
        return NoContent();
    }
}
