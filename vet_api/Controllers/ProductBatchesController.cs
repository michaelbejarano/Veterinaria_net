using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using vet.Application.UseCases.ProductBatch.Commands;
using vet.Application.UseCases.ProductBatch.Queries;

namespace vet_api.Controllers;

[ApiController]
[Authorize(Roles = "SUPER_ADMIN,ADMIN,RECEPTIONIST")]
[Route("api/product-batches")]
public class ProductBatchesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductBatchesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductBatchCommand command)
    {
        var id = await _mediator.Send(command);

        return StatusCode(
            StatusCodes.Status201Created,
            new { id });
    }

    [HttpGet("product/{productId:long}")]
    public async Task<IActionResult> GetByProduct(long productId)
    {
        var result = await _mediator.Send(
            new GetProductBatchesQuery(productId));

        return Ok(result);
    }
}