using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;

namespace PgManagement_WebApi.Services
{
    public class RoomService:IRoomService
    {
        private readonly ApplicationDbContext context;

        public RoomService(ApplicationDbContext context)
        {
            this.context = context;
        }
        public async Task<(bool success, string? error, int statusCode)> ValidateRoomAsync(
        string roomId,
        string pgId)
        {
            var room = await context.Rooms
                .Where(r => r.RoomId == roomId && r.PgId == pgId)
                .Select(r => new
                {
                    r.RoomId,
                    r.Capacity,
                    OccupiedBeds = context.TenantRooms.Count(tr =>
                        tr.RoomId == r.RoomId &&
                        tr.PgId == pgId &&
                        tr.ToDate == null)
                })
                .FirstOrDefaultAsync();

            if (room == null)
                return (false, "Invalid room.", 400);

            if (room.OccupiedBeds >= room.Capacity)
                return (false, "Room is already full.", 400);

            return (true, null, 200);
        }

        public async Task<(bool success, Guid roomRentHistoryId, string? error, int statusCode)>
            GetActiveRentAsync(string roomId)
        {
            var rent = await context.RoomRentHistories
                .FirstOrDefaultAsync(rrh =>
                    rrh.RoomId == roomId &&
                    rrh.EffectiveTo == null);

            if (rent == null)
                return (false, Guid.Empty, "Room has no active rent configuration.", 400);

            return (true, rent.RoomRentHistoryId, null, 200);
        }
    }
}
