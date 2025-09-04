using Application.DTOs.Auth.Request;
using Application.DTOs.Auth.Response;
using Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface IAuthService
    {
        Task<Result<LoginResponse>> LoginAsync(LoginRequest request);
        Task<Result<string>> RegisterAsync(RegisterRequest request);
        Task<Result<LoginResponse>> RefreshTokenAsync(ClaimsPrincipal principal, string refreshToken);
        Task<Result<string>> LogoutAsync(ClaimsPrincipal principal);
        Task<Result<string>> CheckSessionAsync(ClaimsPrincipal principal, string refreshToken);
    }
}