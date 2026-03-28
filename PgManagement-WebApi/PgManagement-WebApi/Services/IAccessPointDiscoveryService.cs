namespace PgManagement_WebApi.Services
{
    public interface IAccessPointDiscoveryService
    {
        Task SyncAccessPointsAsync();
    }
}
