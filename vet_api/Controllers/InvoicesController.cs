using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using vet.Application.UseCases.Invoice.Queries;

namespace vet_api.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize(Roles = "SUPER_ADMIN,ADMIN,RECEPTIONIST")]
public class InvoicesController : ControllerBase
{
    private readonly IMediator _mediator;

    public InvoicesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] GetAllInvoicesQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            query,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetInvoiceByIdQuery(id),
            cancellationToken);

        return Ok(result);
    }
}