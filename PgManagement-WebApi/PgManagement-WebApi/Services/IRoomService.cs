using PgManagement_WebApi.DTOs.Pagination;
using PgManagement_WebApi.DTOs.Room;

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

        Task<PageResultsDto<RoomDto>> GetRoomsAsync(
            List<string> pgIds, int page, int pageSize,
            string? search, string? status, string? ac, string? vacancies);
        Task<RoomDto?> GetRoomByIdAsync(string roomId, List<string> pgIds);
        Task<(bool success, object result, int statusCode)> CreateRoomAsync(
            string pgId, string? branchId, CreateRoomDto dto);
        Task<(bool success, object result, int statusCode)> UpdateRoomAsync(
            string roomId, string pgId, UpdateRoomDto dto, string userId);
        Task<(bool success, object result, int statusCode)> DeleteRoomAsync(string roomId, string pgId);
        Task<object> GetTenantsInRoomAsync(string roomId, List<string> pgIds);
    }
}
