using PgManagement_WebApi.DTOs.Payment;

namespace PgManagement_WebApi.Services
{
    public interface IPaymentModeService
    {
        Task<List<PaymentModeDto>> GetPaymentModesAsync();
    }
}
