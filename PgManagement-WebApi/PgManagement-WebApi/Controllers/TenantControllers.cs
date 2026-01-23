using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Pagination;
using PgManagement_WebApi.DTOs.Tenant;
using PgManagement_WebApi.Identity;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/tenants")]
    [ApiController]
    public class TenantController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IConfiguration configuration;
        public TenantController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            this.context = context;
            this.userManager = userManager;
            this.configuration = configuration;
        }
        [HttpGet]
        public async Task<IActionResult> GetTenants(
            int page = 1,
            int pageSize = 10,
            string? search = null,
            string? status = null,
            string? roomId = null,
            string sortBy = "updated",
            string sortDir ="desc"
            )
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            var query=context.Tenants.Where(t=>t.PgId== pgId);

            //search (Name / Contact)
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(t=>t.Name.Contains(search) || t.ContactNumber.Contains(search));
            }

            // Join with TenantRoom (LEFT JOIN to filter active and moved-out tenants)
            var projectedQuery = query.Select(t => new
            {
                Tenant = t,
                ActiveAssignment = context.TenantRooms
                    .Where(tr =>
                        tr.TenantId == t.TenantId &&
                        tr.PgId == pgId &&
                        tr.ToDate == null)
                    .Select(tr => new
                    {
                        tr.RoomId,
                        tr.Room.RoomNumber,
                        tr.FromDate
                    })
                    .FirstOrDefault()
            });

            //status filter, Above will have assignment = null for Inactive tenants
            if (!string.IsNullOrEmpty(status))
            {
                status = status.ToLower();
                projectedQuery = status switch
                {
                    "active" => projectedQuery.Where(x=>x.ActiveAssignment != null),
                    "movedout" =>projectedQuery.Where(x=>x.ActiveAssignment == null),
                    _=>projectedQuery//default switch case syntax
                };
            }

            //room filter
            if (!string.IsNullOrEmpty(roomId))
            {
                projectedQuery = projectedQuery
                    .Where(x => x.ActiveAssignment != null //room filter can only be applied on active users(who has room)
                    && x.ActiveAssignment.RoomId==roomId);
            }

            // Total count (after filters)
            var totalCount =await projectedQuery.CountAsync();

            //Sorting
            projectedQuery = (sortBy.ToLower(), sortDir.ToLower()) switch
            {
                ("updated", "desc") => projectedQuery.OrderByDescending(x => x.Tenant.UpdatedAt ?? x.Tenant.CreatedAt),
                ("updated", "asc") => projectedQuery.OrderBy(x => x.Tenant.UpdatedAt ?? x.Tenant.CreatedAt),
                ("name","desc") => projectedQuery.OrderByDescending(x => x.Tenant.Name),
                ("room", "asc") => projectedQuery.OrderBy(x => x.ActiveAssignment!.RoomNumber),
                ("room", "desc") => projectedQuery.OrderByDescending(x => x.ActiveAssignment!.RoomNumber),
                ("checkedin", "desc") => projectedQuery.OrderByDescending(x => x.ActiveAssignment!.FromDate),
                ("checkedin", "asc") => projectedQuery.OrderBy(x => x.ActiveAssignment!.FromDate),
                _ => projectedQuery.OrderBy(x => x.Tenant.Name)//default name sort so 
            };

            //Pagination + Final Projection
            var tenants = await projectedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new TenantListDto
                {
                    TenantId = x.Tenant.TenantId,
                    Name = x.Tenant.Name,
                    ContactNumber = x.Tenant.ContactNumber,

                    RoomId = x.ActiveAssignment != null ? x.ActiveAssignment.RoomId : null,
                    RoomNumber = x.ActiveAssignment !=null ? x.ActiveAssignment.RoomNumber : null,

                    CheckedInAt = x.ActiveAssignment.FromDate,
                    Status = x.ActiveAssignment == null ? "MovedOut" : "Active"
                })
                .ToListAsync();

            return Ok(new PageResultsDto<TenantListDto>
            {
                Items = tenants,
                TotalCount = totalCount
            });
        }
        [HttpPost("{tenantId}/change-room")]
        public async Task<IActionResult> ChangeRoom(string tenantId, ChangeRoomDto dto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            var activeAssignment = await context.TenantRooms
        .FirstOrDefaultAsync(tr =>
            tr.TenantId == tenantId &&
            tr.PgId == pgId &&
            tr.ToDate == null);

            if (activeAssignment == null)
                return BadRequest("Tenant does not have an active room.");

            if (activeAssignment.RoomId == dto.newRoomId)
                return BadRequest("Tenant is already in this room.");

            var room = await context.Rooms
        .Where(r => r.RoomId == dto.newRoomId && r.PgId == pgId)
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
                return BadRequest("Invalid room.");

            if (room.OccupiedBeds >= room.Capacity)
                return BadRequest("Room is already full.");

            //  Transaction: close old + create new
            using var tx = await context.Database.BeginTransactionAsync();

            activeAssignment.ToDate = DateTime.Now;

            context.TenantRooms.Add(new TenantRoom
            {
                TenantRoomId = Guid.NewGuid(),
                TenantId = tenantId,
                RoomId = dto.newRoomId,
                PgId = pgId,
                FromDate = DateTime.Now
            });

            await context.SaveChangesAsync();
            await tx.CommitAsync();

            return NoContent();
        }
        [HttpPost("${tenantId}/move-out")]
        public async Task<IActionResult> MoveOutTenant(string tenantId)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (pgId == null) return Unauthorized();

            var activeAssignment = await context.TenantRooms.FirstOrDefaultAsync(tr => tr.TenantId == tenantId
                                    && tr.PgId == pgId && tr.ToDate==null);
            if (activeAssignment == null) return BadRequest("Tenant Does not have an active room");

            activeAssignment.ToDate = DateTime.Now;
            
            await context.SaveChangesAsync();
            return NoContent();                                        
        }
        [HttpPost("create-tenant")]
        public async Task<IActionResult> CreateTenant(CreateTenantDto createTenantDto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            var room = await context.Rooms.Where(r => r.RoomId == createTenantDto.RoomId && r.PgId == pgId)
                        .Select(r => new
                        {
                            r.RoomId,
                            r.Capacity,
                            occupiedBeds = context.TenantRooms.Count(tr => tr.RoomId == createTenantDto.RoomId
                                && tr.RoomId == r.RoomId && tr.PgId == pgId && tr.ToDate == null)
                        }).FirstOrDefaultAsync();

            if (room == null)
                return BadRequest("Invalid Room");
            if (room.occupiedBeds >= room.Capacity)
                return BadRequest("Room is already full");

            using var tx = await context.Database.BeginTransactionAsync();

            var tenant = new Tenant
            {
                TenantId = Guid.NewGuid().ToString(),
                PgId=pgId,
                RoomId = createTenantDto.RoomId,
                Name=createTenantDto.Name,
                ContactNumber=createTenantDto.ContactNumber,
                AadharNumber=createTenantDto.AadharNumber,
                AdvanceAmount = createTenantDto.AdvanceAmount,
                RentPaidUpto = createTenantDto.RentPaidUpto,
                Notes=createTenantDto.Notes,
                CreatedAt=DateTime.Now,
                UpdatedAt=DateTime.Now,
            };
            context.Tenants.Add(tenant);

            var tenantRoom = new TenantRoom
            {
                TenantRoomId = Guid.NewGuid(),
                TenantId = tenant.TenantId,
                RoomId = room.RoomId,
                PgId = pgId,
                FromDate = createTenantDto.FromDate ?? DateTime.Now,
                ToDate = createTenantDto.ToDate
            };
            context.TenantRooms.Add(tenantRoom);
            await context.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new { tenantId = tenant.TenantId });
        }
        [HttpPut("{tenantId}")]
        public async Task<IActionResult> UpdateTenant(string tenantId, UpdateTenantDto dto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if(string.IsNullOrEmpty(pgId)) 
                return Unauthorized();
            
            var tenant = await context.Tenants.SingleOrDefaultAsync(t=>t.TenantId== tenantId && t.PgId==pgId);
            if (tenant == null)
                return NotFound();

            tenant.Name=dto.Name;
            tenant.ContactNumber=dto.ContactNumber;
            tenant.AadharNumber=dto.AadharNumber;
            tenant.AdvanceAmount=dto.AdvanceAmount;
            tenant.RentPaidUpto =dto.RentPaidUpto;
            tenant.Notes = dto.Notes;
            tenant.UpdatedAt = DateTime.Now;

            await context.SaveChangesAsync();

            return NoContent();
        }
    }
}
