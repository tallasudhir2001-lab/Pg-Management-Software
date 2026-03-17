namespace PgManagement_WebApi.Services
{
    public interface IRoomService
    {
        Task<(bool success, string? error, int statusCode)> ValidateRoomAsync(
        string roomId,
        string pgId
    );

        Task<(bool success, Guid roomRentHistoryId, string? error, int statusCode)>
            GetActiveRentAsync(string roomId);
    }
}
