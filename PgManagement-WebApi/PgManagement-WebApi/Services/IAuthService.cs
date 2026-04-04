using PgManagement_WebApi.DTOs.Auth;

namespace PgManagement_WebApi.Services
{
    public interface IAuthService
    {
        Task<(bool success, object result, int statusCode)> LoginAsync(LoginRequestDto request);
        Task<(bool success, object result, int statusCode)> SelectPgAsync(string userId, SelectPgDto selectedPg);
        Task<(bool success, object result, int statusCode)> RefreshAsync(RefreshTokenRequestDto dto);
        Task LogoutAsync(string refreshToken);
    }
}
