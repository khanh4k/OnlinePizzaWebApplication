using Microsoft.AspNetCore.Http;
using OnlinePizzaWebApplication.ViewModels;
using System.Net.Http;

namespace OnlinePizzaWebApplication.Service
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(HttpContext context, VnPaymentRequestModel model);
        VnPaymentResponseModel PaymentExecute(IQueryCollection collections);
    }
}
