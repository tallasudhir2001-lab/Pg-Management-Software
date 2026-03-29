using PgManagement_WebApi.DTOs.Booking;
using PgManagement_WebApi.DTOs.Pagination;

namespace PgManagement_WebApi.Services
{
    public interface IBookingService
    {
        Task<PageResultsDto<BookingListItemDto>> GetBookingsAsync(string pgId, BookingListQueryDto query);
        Task<BookingDetailsDto?> GetBookingByIdAsync(string pgId, string bookingId);
        Task<string> CreateBookingAsync(string pgId, CreateBookingDto dto, string userId, string? branchId = null);
        Task UpdateBookingAsync(string pgId, string bookingId, UpdateBookingDto dto);
        Task CancelBookingAsync(string pgId, string bookingId);
        Task TerminateBookingAsync(string pgId, string bookingId);
        Task<bool> HasActiveBookingAsync(string tenantId);
        Task TerminateNoShowBookingsAsync(string pgId);
    }
}