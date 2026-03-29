using System.Security.Claims;

namespace PgManagement_WebApi.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? UserId
            => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        public string? PgId
            => _httpContextAccessor.HttpContext?.User?.FindFirstValue("pgId");

        public string? BranchId
            => _httpContextAccessor.HttpContext?.User?.FindFirstValue("branchId");
    }
}
