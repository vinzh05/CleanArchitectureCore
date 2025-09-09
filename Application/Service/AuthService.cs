using Application.Abstractions.Common;
using Application.Abstractions.Infrastructure;
using Application.Abstractions.Services;
using Application.DTOs.Auth.Request;
using Application.DTOs.Auth.Response;
using Domain.Entities.Identity;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Shared.Common;
using System.Net;
using System.Security.Claims;

namespace Application.Service
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly IValidator<LoginRequest> _loginValidator;
        private readonly IValidator<RegisterRequest> _registerValidator;
        private readonly IJwtProvider _jwtProvider;
        private readonly IRedisCacheService _cacheService;
        private readonly ICurrentUserService _userService;

        public AuthService(
            IUnitOfWork unitOfWork,
            IValidator<RegisterRequest> registerValidator,
            IValidator<LoginRequest> loginValidator,
            IJwtProvider jwt,
            IRedisCacheService cacheService,
            ICurrentUserService userService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _registerValidator = registerValidator ?? throw new ArgumentNullException(nameof(registerValidator));
            _loginValidator = loginValidator ?? throw new ArgumentNullException(nameof(loginValidator));
            _passwordHasher = new PasswordHasher<User>();
            _jwtProvider = jwt ?? throw new ArgumentNullException(nameof(jwt));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        // ===================== Private helper =====================

        private async Task<LoginResponse> GenerateAndCacheTokenAsync(User user)
        {
            var tokenPair = await _jwtProvider.GenerateToken(user);

            // Dùng Hash cho refresh token
            await _cacheService.HashSetAsync(
                key: $"refresh:{user.Id}",
                field: tokenPair.RefreshToken,
                value: "valid",
                expiry: TimeSpan.FromDays(7)
            );

            return new LoginResponse
            {
                AccessToken = tokenPair.AccessToken,
                RefreshToken = tokenPair.RefreshToken
            };
        }

        private async Task CacheUserAsync(User user)
        {
            if (user == null) return;

            await _cacheService.SetAsync($"user:id:{user.Id}", user, TimeSpan.FromDays(7));
            await _cacheService.SetAsync($"user:email:{user.Email}", user.Id, TimeSpan.FromDays(7));
        }

        private async Task<User?> GetUserByEmailAsync(string email)
        {
            // 1. Check mapping email->id
            var userId = await _cacheService.GetAsync<Guid?>($"user:email:{email}");
            if (userId.HasValue)
            {
                var cachedUser = await _cacheService.GetAsync<User>($"user:id:{userId.Value}");
                if (cachedUser != null) return cachedUser;
            }

            // 2. Fallback DB
            var user = await _unitOfWork.Auths.GetByEmail(email);
            if (user != null) await CacheUserAsync(user);
            return user;
        }

        private async Task<User?> GetUserByIdAsync(Guid userId)
        {
            var cachedUser = await _cacheService.GetAsync<User>($"user:id:{userId}");
            if (cachedUser != null) return cachedUser;

            var user = await _unitOfWork.Auths.GetByIdAsync(userId);
            if (user != null) await CacheUserAsync(user);
            return user;
        }

        // ===================== Public methods =====================

        public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request)
        {
            var validationResult = await _loginValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
                return Result<LoginResponse>.FailureResult("Missing parameters", "VALIDATION_FAILED", HttpStatusCode.BadRequest);

            var user = await GetUserByEmailAsync(request.Email);
            if (user == null)
                return Result<LoginResponse>.FailureResult("Email does not exist", "EMAIL_NOT_FOUND", HttpStatusCode.NotFound);

            if (!user.EmailConfirmed)
                return Result<LoginResponse>.FailureResult("Email not verified", "EMAIL_NOT_VERIFIED", HttpStatusCode.Unauthorized);

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

            if (result == PasswordVerificationResult.Success)
            {
                var tokenPair = await GenerateAndCacheTokenAsync(user);

                return Result<LoginResponse>.SuccessResult(tokenPair, "Login successful");
            }

            return Result<LoginResponse>.FailureResult("Incorrect password", "PASSWORD_INCORRECT", HttpStatusCode.Unauthorized);
        }

        public async Task<Result<string>> RegisterAsync(RegisterRequest request)
        {
            var validationResult = await _registerValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
                return Result<string>.FailureResult(validationResult.Errors[0].ErrorMessage, "VALIDATION_FAILED", HttpStatusCode.BadRequest);

            var existingUser = await GetUserByEmailAsync(request.Email);
            if (existingUser != null)
                return Result<string>.FailureResult("Email is already registered", "EMAIL_ALREADY_EXISTS", HttpStatusCode.Conflict);

            var user = new User(request.Email, request.FullName, "user")
            {
                EmailConfirmed = true // normal flow: send email confirmation
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            try
            {
                await _unitOfWork.BeginTransactionAsync();
                await _unitOfWork.Auths.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                // Cache ngay sau khi đăng ký
                await CacheUserAsync(user);

                return Result<string>.SuccessResult("Registration successful", "SUCCESS");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Result<string>.FailureResult($"Registration failed: {ex.Message}", "REGISTRATION_FAILED", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<Result<LoginResponse>> RefreshTokenAsync(ClaimsPrincipal principal, string refreshToken)
        {
            var hashKey = $"refresh:{_userService.UserId}";
            var exists = await _cacheService.HashGetAsync(hashKey, refreshToken);

            if (string.IsNullOrEmpty(exists))
                return Result<LoginResponse>.FailureResult("Invalid or expired refresh token", "INVALID_REFRESH_TOKEN", HttpStatusCode.Unauthorized);

            var user = await GetUserByIdAsync(_userService.UserId.Value);
            if (user == null)
                return Result<LoginResponse>.FailureResult("User not found", "USER_NOT_FOUND", HttpStatusCode.NotFound);

            var tokenPair = await GenerateAndCacheTokenAsync(user);

            return Result<LoginResponse>.SuccessResult(tokenPair, "Token refreshed");
        }

        public async Task<Result<string>> LogoutAsync(ClaimsPrincipal principal)
        {
            await _cacheService.RemoveAsync($"refresh:{_userService.UserId}");
            return Result<string>.SuccessResult("Logged out successfully", "LOGOUT_SUCCESS");
        }

        public async Task<Result<string>> CheckSessionAsync(ClaimsPrincipal principal, string refreshToken)
        {
            var hashKey = $"refresh:{_userService.UserId}";
            var exists = await _cacheService.HashGetAsync(hashKey, refreshToken);
            if (string.IsNullOrEmpty(exists))
                return Result<string>.FailureResult("Session expired or invalid", "SESSION_INVALID", HttpStatusCode.Unauthorized);

            return Result<string>.SuccessResult("Session is active", "SESSION_ACTIVE");
        }
    }
}