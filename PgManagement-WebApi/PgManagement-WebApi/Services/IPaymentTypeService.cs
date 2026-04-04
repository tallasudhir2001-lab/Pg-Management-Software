using PgManagement_WebApi.DTOs.PaymentType;

namespace PgManagement_WebApi.Services
{
    public interface IPaymentTypeService
    {
        Task<List<PaymentTypeDto>> GetPaymentTypesAsync();
    }
}
