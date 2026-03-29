namespace PgManagement_WebApi.Services
{
    public interface ICurrentUserService
    {
        string? UserId { get; }
        string? PgId { get; }
        string? BranchId { get; }
    }
}
