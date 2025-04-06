using Microsoft.Extensions.Configuration;
using Net.payOS;
using Net.payOS.Types;

namespace MedicalWarehouse_Services
{
    public class PayOsService
    {
        private readonly IConfiguration _configuration;
        private readonly IConfigurationSection _payOsSetting;

        public PayOsService(IConfiguration configuration)
        {
            _configuration = configuration;
            _payOsSetting = _configuration.GetSection("PayOs");
        }
        public async Task<CreatePaymentResult> createPaymentLink(PaymentData paymentData)
        {
            var client_id = _payOsSetting.GetSection("ClientId").Value;
            var api_key = _payOsSetting.GetSection("ApiKey").Value;
            var checkSum_key = _payOsSetting.GetSection("CheckSumKey").Value;

            PayOS payOS = new PayOS(client_id, api_key, checkSum_key);

            PaymentData payment = new PaymentData(
                paymentData.orderCode,
                paymentData.amount,
                "Payment Order",
                paymentData.items,
                paymentData.cancelUrl,
                paymentData.returnUrl
                );

            CreatePaymentResult createPayment = await payOS.createPaymentLink(paymentData);
            return createPayment;
        }

        public async Task<PaymentLinkInformation> getPaymentLinkInformation(int id)
        {

            var client_id = _payOsSetting.GetSection("ClientId").Value;
            var api_key = _payOsSetting.GetSection("ApiKey").Value;
            var checkSum_key = _payOsSetting.GetSection("CheckSumKey").Value;

            PayOS payOS = new PayOS(client_id, api_key, checkSum_key);
            PaymentLinkInformation paymentLinkInformation = await payOS.getPaymentLinkInformation(id);
            return paymentLinkInformation;
        }

        public async Task<PaymentLinkInformation> cancelPaymentLink(int id, string reason)
        {
            var client_id = _payOsSetting.GetSection("ClientId").Value;
            var api_key = _payOsSetting.GetSection("ApiKey").Value;
            var checkSum_key = _payOsSetting.GetSection("CheckSumKey").Value;

            PayOS payOS = new PayOS(client_id, api_key, checkSum_key);

            PaymentLinkInformation cancelledPaymentLinkInfo = await payOS.cancelPaymentLink(id, reason);
            return cancelledPaymentLinkInfo;
        }

        public async Task<string> confirmWebhook(string url)
        {
            var client_id = _payOsSetting.GetSection("ClientId").Value;
            var api_key = _payOsSetting.GetSection("ApiKey").Value;
            var checkSum_key = _payOsSetting.GetSection("CheckSumKey").Value;

            PayOS payOS = new PayOS(client_id, api_key, checkSum_key);
            return await payOS.confirmWebhook(url);

        }



        public WebhookData verifyPaymentWebhookData(WebhookType webhookType)
        {
            var client_id = _payOsSetting.GetSection("ClientId").Value;
            var api_key = _payOsSetting.GetSection("ApiKey").Value;
            var checkSum_key = _payOsSetting.GetSection("CheckSumKey").Value;

            PayOS payOS = new PayOS(client_id, api_key, checkSum_key);
            WebhookData webhookData = payOS.verifyPaymentWebhookData(webhookType);
            return webhookData;
        }

    }
}
