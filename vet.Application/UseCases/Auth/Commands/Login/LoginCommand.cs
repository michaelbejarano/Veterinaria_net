using MediatR;
using vet.Domain.DTOs;

namespace vet.Application.UseCases.Auth.Commands.Login;

public record LoginCommand(
    string Email,
    string Password
) : IRequest<AuthResponseDto>;