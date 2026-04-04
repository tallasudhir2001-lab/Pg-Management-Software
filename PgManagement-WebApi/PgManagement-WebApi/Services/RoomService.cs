using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Pagination;
using PgManagement_WebApi.DTOs.Room;
using PgManagement_WebApi.Models;
using System.Text.Json;

namespace PgManagement_WebApi.Services
{
    public class RoomService : IRoomService
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

        public async Task<PageResultsDto<RoomDto>> GetRoomsAsync(
            List<string> pgIds, int page, int pageSize,
            string? search, string? status, string? ac, string? vacancies)
        {
            IQueryable<Room> query = context.Rooms
                .Where(r => pgIds.Contains(r.PgId));

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(r => r.RoomNumber.Contains(search));

            if (!string.IsNullOrWhiteSpace(ac))
            {
                if (ac.Equals("ac", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(r =>
                        context.RoomRentHistories.Any(rrh =>
                            rrh.RoomId == r.RoomId && rrh.EffectiveTo == null && rrh.IsAc));
                }
                else if (ac.Equals("non-ac", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(r =>
                        context.RoomRentHistories.Any(rrh =>
                            rrh.RoomId == r.RoomId && rrh.EffectiveTo == null && !rrh.IsAc));
                }
            }

            var projectedQuery = query.Select(r => new
            {
                Room = r,
                OccupiedBeds = context.TenantRooms.Count(tr =>
                    tr.RoomId == r.RoomId && pgIds.Contains(tr.PgId) && tr.ToDate == null),
                CurrentRent = context.RoomRentHistories
                    .Where(rrh => rrh.RoomId == r.RoomId && rrh.EffectiveTo == null)
                    .Select(rrh => new { rrh.RentAmount, rrh.IsAc })
                    .FirstOrDefault()
            });

            if (!string.IsNullOrWhiteSpace(status))
            {
                projectedQuery = status.ToLower() switch
                {
                    "available" => projectedQuery.Where(x => x.OccupiedBeds == 0),
                    "full" => projectedQuery.Where(x => x.OccupiedBeds >= x.Room.Capacity),
                    "partial" => projectedQuery.Where(x => x.OccupiedBeds > 0 && x.OccupiedBeds < x.Room.Capacity),
                    _ => projectedQuery
                };
            }

            if (!string.IsNullOrWhiteSpace(vacancies))
            {
                projectedQuery = vacancies switch
                {
                    "0" => projectedQuery.Where(x => x.Room.Capacity - x.OccupiedBeds == 0),
                    "1" => projectedQuery.Where(x => x.Room.Capacity - x.OccupiedBeds == 1),
                    "2" => projectedQuery.Where(x => x.Room.Capacity - x.OccupiedBeds == 2),
                    "3+" => projectedQuery.Where(x => x.Room.Capacity - x.OccupiedBeds >= 3),
                    _ => projectedQuery
                };
            }

            var totalCount = await projectedQuery.CountAsync();

            var rooms = await projectedQuery
                .OrderBy(x => x.Room.RoomNumber)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new RoomDto
                {
                    RoomId = x.Room.RoomId,
                    RoomNumber = x.Room.RoomNumber,
                    Capacity = x.Room.Capacity,
                    Occupied = x.OccupiedBeds,
                    Vacancies = x.Room.Capacity - x.OccupiedBeds,
                    RentAmount = x.CurrentRent != null ? x.CurrentRent.RentAmount : 0,
                    isAc = x.CurrentRent != null && x.CurrentRent.IsAc,
                    Status = x.OccupiedBeds == 0 ? "Available" :
                        x.OccupiedBeds >= x.Room.Capacity ? "Full" : "Partial"
                })
                .ToListAsync();

            return new PageResultsDto<RoomDto> { Items = rooms, TotalCount = totalCount };
        }

        public async Task<RoomDto?> GetRoomByIdAsync(string roomId, List<string> pgIds)
        {
            return await context.Rooms
                .Where(r => r.RoomId == roomId && pgIds.Contains(r.PgId))
                .Select(r => new
                {
                    r.RoomId, r.RoomNumber, r.Capacity,
                    CurrentRent = context.RoomRentHistories
                        .Where(rrh => rrh.RoomId == r.RoomId && rrh.EffectiveTo == null)
                        .Select(rrh => new { rrh.RentAmount, rrh.IsAc })
                        .FirstOrDefault(),
                    OccupiedBeds = context.TenantRooms.Count(tr =>
                        tr.RoomId == r.RoomId && pgIds.Contains(tr.PgId) && tr.ToDate == null)
                })
                .Select(r => new RoomDto
                {
                    RoomId = r.RoomId,
                    RoomNumber = r.RoomNumber,
                    Capacity = r.Capacity,
                    Occupied = r.OccupiedBeds,
                    Vacancies = r.Capacity - r.OccupiedBeds,
                    RentAmount = r.CurrentRent != null ? r.CurrentRent.RentAmount : 0,
                    isAc = r.CurrentRent != null && r.CurrentRent.IsAc,
                    Status = r.OccupiedBeds == 0 ? "Available" :
                        r.OccupiedBeds >= r.Capacity ? "Full" : "Partial"
                })
                .FirstOrDefaultAsync();
        }

        public async Task<(bool success, object result, int statusCode)> CreateRoomAsync(
            string pgId, string? branchId, CreateRoomDto dto)
        {
            var roomExists = await context.Rooms
                .AnyAsync(r => r.PgId == pgId && r.RoomNumber == dto.RoomNumber);

            if (roomExists)
                return (false, "Room number already exists in this PG.", 409);

            using var transaction = await context.Database.BeginTransactionAsync();

            var room = new Room
            {
                RoomId = Guid.NewGuid().ToString(),
                PgId = pgId,
                BranchId = branchId,
                RoomNumber = dto.RoomNumber,
                Capacity = dto.Capacity
            };

            context.Rooms.Add(room);
            await context.SaveChangesAsync();

            var roomRentHistory = new RoomRentHistory
            {
                RoomId = room.RoomId,
                RentAmount = dto.RentAmount,
                IsAc = dto.IsAc,
                EffectiveFrom = new DateTime(2000, 1, 1),
                EffectiveTo = null
            };

            context.RoomRentHistories.Add(roomRentHistory);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, new { room.RoomId }, 201);
        }

        public async Task<(bool success, object result, int statusCode)> UpdateRoomAsync(
            string roomId, string pgId, UpdateRoomDto dto, string userId)
        {
            using var transaction = await context.Database.BeginTransactionAsync();

            var room = await context.Rooms
                .FirstOrDefaultAsync(r => r.RoomId == roomId && r.PgId == pgId);

            if (room == null)
            {
                var otherPgName = await context.Rooms
                    .Where(r => r.RoomId == roomId)
                    .Join(context.PGs, r => r.PgId, pg => pg.PgId, (r, pg) => pg.Name)
                    .FirstOrDefaultAsync();
                if (otherPgName != null)
                    return (false, $"This room belongs to {otherPgName}. Please login to {otherPgName} to modify it.", 403);
                return (false, "Room not found.", 404);
            }

            var duplicateRoom = await context.Rooms.AnyAsync(r =>
                r.PgId == pgId && r.RoomNumber == dto.RoomNumber && r.RoomId != roomId);

            if (duplicateRoom)
                return (false, "Another room with same number already exists.", 409);

            room.RoomNumber = dto.RoomNumber;
            room.Capacity = dto.Capacity;

            var currentRent = await context.RoomRentHistories
                .FirstOrDefaultAsync(rr => rr.RoomId == roomId && rr.EffectiveTo == null);

            if (currentRent == null)
                return (false, "Room has no active rent configuration.", 400);

            var isRentChanged = currentRent.RentAmount != dto.RentAmount || currentRent.IsAc != dto.isAc;

            if (isRentChanged)
            {
                var oldRentAmount = currentRent.RentAmount;
                var oldIsAc = currentRent.IsAc;

                currentRent.EffectiveTo = DateTime.Now;

                var newRent = new RoomRentHistory
                {
                    RoomId = roomId,
                    RentAmount = dto.RentAmount,
                    IsAc = dto.isAc,
                    EffectiveFrom = DateTime.Now
                };

                context.RoomRentHistories.Add(newRent);
                await context.SaveChangesAsync();

                var activeTenantRents = await context.TenantRentHistories
                    .Include(trh => trh.RoomRentHistory)
                    .Where(trh => trh.ToDate == null && trh.RoomRentHistory.RoomId == roomId)
                    .ToListAsync();

                foreach (var tenantRent in activeTenantRents)
                {
                    tenantRent.ToDate = DateTime.Now;
                    context.TenantRentHistories.Add(new TenantRentHistory
                    {
                        TenantId = tenantRent.TenantId,
                        RoomRentHistoryId = newRent.RoomRentHistoryId,
                        FromDate = DateTime.Now
                    });
                }

                context.AuditEvents.Add(new AuditEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    PgId = pgId,
                    BranchId = room.BranchId,
                    EventType = "ROOM_RENT_CHANGED",
                    EntityType = "Room",
                    EntityId = roomId,
                    Description = $"Room {room.RoomNumber} rent changed from ₹{oldRentAmount} to ₹{dto.RentAmount}",
                    OldValue = JsonSerializer.Serialize(new { RentAmount = oldRentAmount, IsAc = oldIsAc }),
                    NewValue = JsonSerializer.Serialize(new { RentAmount = dto.RentAmount, IsAc = dto.isAc }),
                    PerformedByUserId = userId,
                    PerformedAt = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, "OK", 204);
        }

        public async Task<(bool success, object result, int statusCode)> DeleteRoomAsync(string roomId, string pgId)
        {
            var room = await context.Rooms
                .Include(r => r.Tenants)
                .FirstOrDefaultAsync(r => r.RoomId == roomId && r.PgId == pgId);

            if (room == null)
            {
                var otherPgName = await context.Rooms
                    .Where(r => r.RoomId == roomId)
                    .Join(context.PGs, r => r.PgId, pg => pg.PgId, (r, pg) => pg.Name)
                    .FirstOrDefaultAsync();
                if (otherPgName != null)
                    return (false, $"This room belongs to {otherPgName}. Please login to {otherPgName} to modify it.", 403);
                return (false, "Not found", 404);
            }

            var hasAnyTenantHistory = await context.TenantRooms.AnyAsync(tr =>
                tr.RoomId == roomId && tr.PgId == pgId);

            if (hasAnyTenantHistory)
                return (false, "Room cannot be deleted because tenants are or were assigned to it.", 400);

            using var tx = await context.Database.BeginTransactionAsync();

            var rentHistories = await context.RoomRentHistories
                .Where(rrh => rrh.RoomId == roomId)
                .ToListAsync();

            context.RoomRentHistories.RemoveRange(rentHistories);
            context.Rooms.Remove(room);

            await context.SaveChangesAsync();
            await tx.CommitAsync();

            return (true, "OK", 204);
        }

        public async Task<object> GetTenantsInRoomAsync(string roomId, List<string> pgIds)
        {
            return await context.TenantRooms
                .AsNoTracking()
                .Where(tr =>
                    tr.RoomId == roomId &&
                    pgIds.Contains(tr.PgId) &&
                    tr.ToDate == null)
                .Select(tr => new
                {
                    tr.Tenant.TenantId,
                    tr.Tenant.Name,
                    tr.Tenant.ContactNumber,
                    CheckedInAt = tr.FromDate,
                    Status = "Active"
                })
                .ToListAsync();
        }
    }
}
