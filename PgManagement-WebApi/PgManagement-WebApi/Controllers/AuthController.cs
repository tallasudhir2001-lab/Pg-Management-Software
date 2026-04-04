using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PgManagement_WebApi.DTOs.Auth;
using PgManagement_WebApi.Services;
using System.Security.Claims;

namespace PgManagement_WebApi.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDto request)
        {
            var (success, result, statusCode) = await _authService.LoginAsync(request);
            return StatusCode(statusCode, result);
        }

        [HttpPost("select-pg")]
        [Authorize]
        public async Task<IActionResult> SelectPg(SelectPgDto selectedPg)
        {
            var authLevel = User.FindFirst("auth_level")?.Value;
            if (authLevel != "identity_only")
                return Forbid();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var (success, result, statusCode) = await _authService.SelectPgAsync(userId, selectedPg);
            return StatusCode(statusCode, result);
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto)
        {
            var (success, result, statusCode) = await _authService.RefreshAsync(dto);
            return StatusCode(statusCode, result);
        }

        [HttpPost("logout")]
        [AllowAnonymous]
        public async Task<IActionResult> Logout([FromBody] LogoutRequestDto dto)
        {
            await _authService.LogoutAsync(dto.RefreshToken);
            return NoContent();
        }
    }
}
