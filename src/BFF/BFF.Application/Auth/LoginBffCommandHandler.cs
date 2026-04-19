using BFF.Domain.Interfaces;
using MediatR;

namespace BFF.Application.Auth;

public class LoginBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<LoginBffCommand, LoginBffResult>
{
    public async Task<LoginBffResult> Handle(LoginBffCommand request, CancellationToken ct)
    {
        var response = await apiClient.PostAsync<LoginRequest, LoginResponse>(
            "api/auth/login",
            new LoginRequest(request.Email, request.Password),
            ct);

        if (response is null)
            throw new InvalidOperationException("Backend API returned an empty response.");

        return new LoginBffResult(response.Token, response.Email, response.FullName, response.Permissions);
    }
}

// Internal DTOs matching backend API response shape
record LoginRequest(string Email, string Password);
record LoginResponse(string Token, string Email, string FullName, IEnumerable<string> Permissions);
