using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using OnlinePizzaWebApplication.Components;
using OnlinePizzaWebApplication.ViewModels;
using System;

namespace OnlinePizzaWebApplication.Service
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _config;

        public VnPayService(IConfiguration config)
        {
            _config = config;
        }

        public string CreatePaymentUrl(HttpContext context, VnPaymentRequestModel model)
        {
            var tick = DateTime.Now.Ticks.ToString();
            var vnpay = new VnPayLibrary();
            vnpay.AddRequestData("vnp_Version", _config["VnPay:Version"]);
            vnpay.AddRequestData("vnp_Command", _config["VnPay:Command"]);
            vnpay.AddRequestData("vnp_TmnCode", _config["VnPay:TmnCode"]);
            vnpay.AddRequestData("vnp_Amount", (model.Amount * 100).ToString());

            vnpay.AddRequestData("vnp_CreateDate", model.CreatedDate.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", _config["VnPay:CurrCode"]);
            vnpay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress(context));
            vnpay.AddRequestData("vnp_Locale", _config["VnPay:Locale"]);

            vnpay.AddRequestData("vnp_OrderInfo", "Checkout Your Orders" + model.OrderId);
            vnpay.AddRequestData("vnp_OrderType", "other"); //default value: other
            vnpay.AddRequestData("vnp_ReturnUrl", _config["VnPay:PaymentBackReturnUrl"]);
            vnpay.AddRequestData("vnp_TxnRef", tick);

            var paymentUrl = vnpay.CreateRequestUrl(_config["VnPay:BaseUrl"], _config["VnPay:HashSecret"]);

            return paymentUrl;
        }

        public VnPaymentResponseModel PaymentExecute(IQueryCollection collections)
        {
            var vnpay = new VnPayLibrary();
            var response = new VnPaymentResponseModel();
            foreach (var (key, value) in collections)
            {
                vnpay.AddResponseData(key, value);
            }

            var vnpResponseCode = collections["vnp_ResponseCode"];
            response.Success = vnpResponseCode == "00"; // "00" là mã thành công của VNPay
            response.VnPayResponseCode = vnpResponseCode;
            response.OrderId = collections["vnp_TxnRef"];
            response.TransactionId = collections["vnp_TransactionNo"];
            response.PaymentId = collections["vnp_PayDate"];
            response.PaymentMethod = collections["vnp_BankCode"];
            response.Token = collections["vnp_SecureHash"];

            // Xác thực chữ ký
            var vnpSecureHash = collections["vnp_SecureHash"];
            if (!string.IsNullOrEmpty(vnpSecureHash) && vnpay.ValidateSignature(vnpSecureHash, _config["VnPay:HashSecret"]))
            {
                response.Success = true;
            }

            return response;
        }
    }
}
