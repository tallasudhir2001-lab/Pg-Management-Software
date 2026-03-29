using PgManagement_WebApi.Services;

namespace PgManagement_WebApi.Data
{
    public class DesignTimeCurrentUserService : ICurrentUserService
    {
        public string? UserId => "SYSTEM";
        public string? PgId => null;
        public string? BranchId => null;
    }
}
