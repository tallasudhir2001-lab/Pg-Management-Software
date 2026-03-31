using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Booking;
using PgManagement_WebApi.DTOs.Pagination;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.Services
{
    public class BookingService : IBookingService
    {
        private readonly ApplicationDbContext _context;

        public BookingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PageResultsDto<BookingListItemDto>> GetBookingsAsync(string pgId, BookingListQueryDto query)
        {
            var bookingsQuery = _context.Bookings
                .AsNoTracking()
                .Where(b => b.PgId == pgId);

            // -------------------------
            // Filters
            // -------------------------
            if (query.FromDate.HasValue)
                bookingsQuery = bookingsQuery
                    .Where(b => b.ScheduledCheckInDate >= query.FromDate.Value);

            if (query.ToDate.HasValue)
                bookingsQuery = bookingsQuery
                    .Where(b => b.ScheduledCheckInDate <= query.ToDate.Value);

            if (!string.IsNullOrEmpty(query.Status)
                && Enum.TryParse<BookingStatus>(query.Status, true, out var statusFilter))
                bookingsQuery = bookingsQuery
                    .Where(b => b.Status == statusFilter);

            if (!string.IsNullOrEmpty(query.RoomId))
                bookingsQuery = bookingsQuery
                    .Where(b => b.RoomId == query.RoomId);

            // -------------------------
            // Total count
            // -------------------------
            var totalCount = await bookingsQuery.CountAsync();

            // -------------------------
            // Sorting
            // -------------------------
            bookingsQuery = query.SortBy?.ToLower() switch
            {
                "checkindate" => query.SortDir == "asc"
                    ? bookingsQuery.OrderBy(b => b.ScheduledCheckInDate)
                    : bookingsQuery.OrderByDescending(b => b.ScheduledCheckInDate),

                "advanceamount" => query.SortDir == "asc"
                    ? bookingsQuery.OrderBy(b => b.AdvanceAmount)
                    : bookingsQuery.OrderByDescending(b => b.AdvanceAmount),

                _ => bookingsQuery.OrderByDescending(b => b.CreatedAt)
            };

            // -------------------------
            // Pagination + Projection
            // -------------------------
            var items = await bookingsQuery
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(b => new BookingListItemDto
                {
                    BookingId = b.BookingId,
                    TenantId = b.TenantId,
                    TenantName = b.Tenant.Name,
                    RoomId = b.RoomId,
                    RoomNumber = b.Room.RoomNumber,
                    ScheduledCheckInDate = b.ScheduledCheckInDate,
                    Status = b.Status.ToString(),
                    AdvanceAmount = b.AdvanceAmount,
                    Notes = b.Notes,
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();

            return new PageResultsDto<BookingListItemDto>
            {
                Items = items,
                TotalCount = totalCount
            };
        }

        public async Task<PageResultsDto<BookingListItemDto>> GetBookingsAsync(List<string> pgIds, BookingListQueryDto query)
        {
            var bookingsQuery = _context.Bookings
                .AsNoTracking()
                .Where(b => pgIds.Contains(b.PgId));

            if (query.FromDate.HasValue)
                bookingsQuery = bookingsQuery.Where(b => b.ScheduledCheckInDate >= query.FromDate.Value);
            if (query.ToDate.HasValue)
                bookingsQuery = bookingsQuery.Where(b => b.ScheduledCheckInDate <= query.ToDate.Value);
            if (!string.IsNullOrEmpty(query.Status) && Enum.TryParse<BookingStatus>(query.Status, true, out var statusFilter))
                bookingsQuery = bookingsQuery.Where(b => b.Status == statusFilter);
            if (!string.IsNullOrEmpty(query.RoomId))
                bookingsQuery = bookingsQuery.Where(b => b.RoomId == query.RoomId);

            var totalCount = await bookingsQuery.CountAsync();

            bookingsQuery = query.SortBy?.ToLower() switch
            {
                "checkindate" => query.SortDir == "asc"
                    ? bookingsQuery.OrderBy(b => b.ScheduledCheckInDate)
                    : bookingsQuery.OrderByDescending(b => b.ScheduledCheckInDate),
                "advanceamount" => query.SortDir == "asc"
                    ? bookingsQuery.OrderBy(b => b.AdvanceAmount)
                    : bookingsQuery.OrderByDescending(b => b.AdvanceAmount),
                _ => bookingsQuery.OrderByDescending(b => b.CreatedAt)
            };

            var items = await bookingsQuery
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(b => new BookingListItemDto
                {
                    BookingId = b.BookingId,
                    TenantId = b.TenantId,
                    TenantName = b.Tenant.Name,
                    RoomId = b.RoomId,
                    RoomNumber = b.Room.RoomNumber,
                    ScheduledCheckInDate = b.ScheduledCheckInDate,
                    Status = b.Status.ToString(),
                    AdvanceAmount = b.AdvanceAmount,
                    Notes = b.Notes,
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();

            return new PageResultsDto<BookingListItemDto> { Items = items, TotalCount = totalCount };
        }

        public async Task<BookingDetailsDto?> GetBookingByIdAsync(string pgId, string bookingId)
        {
            return await _context.Bookings
                .AsNoTracking()
                .Where(b => b.BookingId == bookingId && b.PgId == pgId)
                .Select(b => new BookingDetailsDto
                {
                    BookingId = b.BookingId,
                    TenantId = b.TenantId,
                    TenantName = b.Tenant.Name,
                    TenantContact = b.Tenant.ContactNumber,
                    RoomId = b.RoomId,
                    RoomNumber = b.Room.RoomNumber,
                    ScheduledCheckInDate = b.ScheduledCheckInDate,
                    Status = b.Status.ToString(),
                    AdvanceAmount = b.AdvanceAmount,
                    Notes = b.Notes,
                    CreatedAt = b.CreatedAt,
                    CreatedBy = b.CreatedBy
                })
                .FirstOrDefaultAsync();
        }

        public async Task<string> CreateBookingAsync(string pgId, CreateBookingDto dto, string userId, string? branchId = null)
        {
            // Validate: if advance given, payment mode is required
            if (dto.AdvanceAmount.HasValue && dto.AdvanceAmount > 0
                && string.IsNullOrEmpty(dto.PaymentModeCode))
                throw new InvalidOperationException("Payment mode is required when advance amount is provided.");

            // Validate: room belongs to this PG
            var roomExists = await _context.Rooms
                .AnyAsync(r => r.RoomId == dto.RoomId && r.PgId == pgId);

            if (!roomExists)
                throw new KeyNotFoundException("Room not found in this PG.");

            // Validate: room has capacity on the scheduled date
            var roomCapacity = await _context.Rooms
                .Where(r => r.RoomId == dto.RoomId)
                .Select(r => r.Capacity)
                .FirstAsync();

            var occupiedBeds = await _context.TenantRooms
                .CountAsync(tr => tr.RoomId == dto.RoomId
                                && tr.FromDate <= dto.ScheduledCheckInDate
                                && (tr.ToDate == null || tr.ToDate >= dto.ScheduledCheckInDate));

            var activeBookingsForRoom = await _context.Bookings
                .CountAsync(b => b.RoomId == dto.RoomId
                              && b.Status == BookingStatus.Active
                              && b.ScheduledCheckInDate.Date == dto.ScheduledCheckInDate.Date
                              && !b.IsDeleted);

            if (occupiedBeds + activeBookingsForRoom >= roomCapacity)
                throw new InvalidOperationException("No vacancy available in this room for the selected date.");

            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                // -------------------------
                // Find or create tenant
                // -------------------------
                var existingTenant = await _context.Tenants
                    .FirstOrDefaultAsync(t => t.AadharNumber == dto.AadharNumber && t.PgId == pgId);

                string tenantId;

                if (existingTenant != null)
                {
                    tenantId = existingTenant.TenantId;

                    // Validate: tenant must not have an active booking
                    var hasActiveBooking = await _context.Bookings
                        .AnyAsync(b => b.TenantId == tenantId
                                    && b.Status == BookingStatus.Active
                                    && !b.IsDeleted);

                    if (hasActiveBooking)
                        throw new InvalidOperationException("Tenant already has an active booking.");
                }
                else
                {
                    // Create new tenant
                    var newTenant = new Tenant
                    {
                        TenantId = Guid.NewGuid().ToString(),
                        PgId = pgId,
                        BranchId = branchId,
                        Name = dto.Name,
                        ContactNumber = dto.ContactNumber,
                        AadharNumber = dto.AadharNumber,
                        Notes = dto.Notes,
                        CreatedAt = DateTime.UtcNow,
                        isDeleted = false
                    };

                    _context.Tenants.Add(newTenant);
                    tenantId = newTenant.TenantId;
                }

                // -------------------------
                // Create booking
                // -------------------------
                var booking = new Booking
                {
                    BookingId = Guid.NewGuid().ToString(),
                    PgId = pgId,
                    BranchId = branchId,
                    TenantId = tenantId,
                    RoomId = dto.RoomId,
                    ScheduledCheckInDate = dto.ScheduledCheckInDate,
                    Status = BookingStatus.Active,
                    AdvanceAmount = dto.AdvanceAmount ?? 0,
                    Notes = dto.Notes
                };

                _context.Bookings.Add(booking);

                // -------------------------
                // Create advance + payment if provided
                // -------------------------
                if (dto.AdvanceAmount.HasValue && dto.AdvanceAmount > 0)
                {
                    var hasActiveAdvance = await _context.Advances
                        .AnyAsync(a => a.TenantId == tenantId && !a.IsSettled);

                    if (hasActiveAdvance)
                        throw new InvalidOperationException("Tenant already has an active advance.");

                    var advance = new Advance
                    {
                        AdvanceId = Guid.NewGuid().ToString(),
                        TenantId = tenantId,
                        BranchId = branchId,
                        Amount = dto.AdvanceAmount.Value,
                        PaidDate = DateTime.UtcNow,
                        IsSettled = false,
                        CreatedByUserId = userId,
                        Notes = $"Advance for booking: {booking.BookingId}"
                    };

                    _context.Advances.Add(advance);

                    var payment = new Payment
                    {
                        PaymentId = Guid.NewGuid().ToString(),
                        TenantId = tenantId,
                        PgId = pgId,
                        BranchId = branchId,
                        Amount = dto.AdvanceAmount.Value,
                        PaymentDate = DateTime.UtcNow,
                        PaidFrom = DateTime.UtcNow,
                        PaidUpto = DateTime.UtcNow,
                        PaymentTypeCode = "ADVANCE_PAYMENT",
                        PaymentModeCode = dto.PaymentModeCode!,
                        PaymentFrequencyCode = "ONETIME",
                        CreatedByUserId = userId,
                        Notes = $"Advance for booking: {booking.BookingId}"
                    };

                    _context.Payments.Add(payment);
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return booking.BookingId;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateBookingAsync(string pgId, string bookingId, UpdateBookingDto dto)
        {
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.PgId == pgId);

            if (booking == null)
                throw new KeyNotFoundException("Booking not found.");

            if (booking.Status != BookingStatus.Active)
                throw new InvalidOperationException("Only active bookings can be updated.");

            booking.RoomId = dto.RoomId;
            booking.ScheduledCheckInDate = dto.ScheduledCheckInDate;
            booking.AdvanceAmount = dto.AdvanceAmount;
            booking.Notes = dto.Notes;

            await _context.SaveChangesAsync();
        }

        public async Task CancelBookingAsync(string pgId, string bookingId)
        {
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.PgId == pgId);

            if (booking == null)
                throw new KeyNotFoundException("Booking not found.");

            if (booking.Status != BookingStatus.Active)
                throw new InvalidOperationException("Only active bookings can be cancelled.");

            booking.Status = BookingStatus.Cancelled;

            await _context.SaveChangesAsync();
        }

        public async Task TerminateBookingAsync(string pgId, string bookingId)
        {
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.PgId == pgId);

            if (booking == null)
                throw new KeyNotFoundException("Booking not found.");

            if (booking.Status != BookingStatus.Active)
                throw new InvalidOperationException("Only active bookings can be terminated.");

            booking.Status = BookingStatus.Terminated;

            await _context.SaveChangesAsync();
        }

        public async Task<bool> HasActiveBookingAsync(string tenantId)
        {
            return await _context.Bookings
                .AnyAsync(b => b.TenantId == tenantId
                            && b.Status == BookingStatus.Active
                            && !b.IsDeleted);
        }

        public async Task TerminateNoShowBookingsAsync(string pgId)
        {
            var today = DateTime.UtcNow.Date;

            var noShowBookings = await _context.Bookings
                .Where(b => b.PgId == pgId
                         && b.Status == BookingStatus.Active
                         && b.ScheduledCheckInDate.Date < today)
                .ToListAsync();

            foreach (var booking in noShowBookings)
            {
                booking.Status = BookingStatus.Terminated;
            }

            await _context.SaveChangesAsync();
        }
    }
}