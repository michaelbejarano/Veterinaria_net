using MediatR;
using Microsoft.AspNetCore.Mvc;
using vet.Application.UseCases.Auth.Commands.Login;

namespace vet_api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _mediator.Send(
                command, cancellationToken);

            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new
            {
                message = "Correo o contraseña incorrectos."
            });
        }
    }
}