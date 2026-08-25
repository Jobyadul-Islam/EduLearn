using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EduLearn.Services
{
    public class BkashPaymentService : IBkashPaymentService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BkashPaymentService> _logger;

        public BkashPaymentService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<BkashPaymentService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        private string BaseUrl => (_configuration["Bkash:BaseUrl"] ?? "").TrimEnd('/');
        private string Username => _configuration["Bkash:Username"] ?? "";
        private string Password => _configuration["Bkash:Password"] ?? "";
        private string AppKey => _configuration["Bkash:AppKey"] ?? "";
        private string AppSecret => _configuration["Bkash:AppSecret"] ?? "";

        public bool IsConfigured => !string.IsNullOrWhiteSpace(BaseUrl) && !string.IsNullOrWhiteSpace(AppKey) && !string.IsNullOrWhiteSpace(AppSecret);

        public async Task<string?> GrantTokenAsync()
        {
            var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/tokenized/checkout/token/grant");
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("username", Username);
            request.Headers.Add("password", Password);
            request.Content = JsonContent.Create(new GrantTokenRequest { AppKey = AppKey, AppSecret = AppSecret }, options: JsonOptions);

            try
            {
                var response = await client.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("bKash Grant Token failed {StatusCode}: {Body}", response.StatusCode, body);
                    return null;
                }

                var parsed = JsonSerializer.Deserialize<GrantTokenResponse>(body, JsonOptions);
                return parsed?.IdToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling bKash Grant Token");
                return null;
            }
        }

        public async Task<BkashCreateResult> CreateAgreementAsync(string idToken, string payerReference, string callbackUrl)
        {
            var body = new CreatePaymentRequest
            {
                Mode = "0000",
                PayerReference = payerReference,
                CallbackURL = callbackUrl
            };
            return await CreateAsync(idToken, body);
        }

        public async Task<BkashCreateResult> CreatePaymentAsync(string idToken, string payerReference, string agreementId, decimal amount, string invoiceNumber, string callbackUrl)
        {
            var body = new CreatePaymentRequest
            {
                Mode = "0011",
                PayerReference = payerReference,
                AgreementID = agreementId,
                Amount = amount.ToString("0.00"),
                Currency = "BDT",
                Intent = "sale",
                MerchantInvoiceNumber = invoiceNumber,
                CallbackURL = callbackUrl
            };
            return await CreateAsync(idToken, body);
        }

        private async Task<BkashCreateResult> CreateAsync(string idToken, CreatePaymentRequest body)
        {
            var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/tokenized/checkout/create");
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Authorization", idToken);
            request.Headers.Add("X-App-Key", AppKey);
            request.Content = JsonContent.Create(body, options: JsonOptions);

            try
            {
                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();
                var parsed = JsonSerializer.Deserialize<CreatePaymentResponse>(responseBody, JsonOptions);

                if (!response.IsSuccessStatusCode || parsed?.StatusCode != "0000")
                {
                    _logger.LogWarning("bKash Create ({Mode}) failed {StatusCode}: {Body}", body.Mode, response.StatusCode, responseBody);
                    return new BkashCreateResult { Success = false, ErrorMessage = parsed?.StatusMessage ?? "bKash rejected the request." };
                }

                return new BkashCreateResult { Success = true, PaymentId = parsed.PaymentID, BkashUrl = parsed.BkashURL };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling bKash Create ({Mode})", body.Mode);
                return new BkashCreateResult { Success = false, ErrorMessage = "Could not reach bKash." };
            }
        }

        public async Task<BkashAgreementExecuteResult> ExecuteAgreementAsync(string idToken, string paymentId)
        {
            var (success, body, statusMessage) = await ExecuteAsync(idToken, paymentId);
            if (!success)
            {
                return new BkashAgreementExecuteResult { Success = false, ErrorMessage = statusMessage };
            }

            var parsed = JsonSerializer.Deserialize<ExecuteAgreementResponse>(body!, JsonOptions);
            return new BkashAgreementExecuteResult { Success = true, AgreementId = parsed?.AgreementID };
        }

        public async Task<BkashPaymentExecuteResult> ExecutePaymentAsync(string idToken, string paymentId)
        {
            var (success, body, statusMessage) = await ExecuteAsync(idToken, paymentId);
            if (!success)
            {
                return new BkashPaymentExecuteResult { Success = false, ErrorMessage = statusMessage };
            }

            var parsed = JsonSerializer.Deserialize<ExecutePaymentResponse>(body!, JsonOptions);
            if (parsed?.TransactionStatus != "Completed")
            {
                return new BkashPaymentExecuteResult { Success = false, ErrorMessage = parsed?.StatusMessage ?? "Payment was not completed." };
            }

            return new BkashPaymentExecuteResult { Success = true, TrxId = parsed.TrxID };
        }

        private async Task<(bool success, string? body, string? statusMessage)> ExecuteAsync(string idToken, string paymentId)
        {
            var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/tokenized/checkout/execute");
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Authorization", idToken);
            request.Headers.Add("X-App-Key", AppKey);
            request.Content = JsonContent.Create(new ExecuteRequest { PaymentID = paymentId }, options: JsonOptions);

            try
            {
                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("bKash Execute failed {StatusCode}: {Body}", response.StatusCode, responseBody);
                    var errorParsed = JsonSerializer.Deserialize<ExecutePaymentResponse>(responseBody, JsonOptions);
                    return (false, null, errorParsed?.StatusMessage ?? "bKash rejected the execute request.");
                }

                return (true, responseBody, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling bKash Execute");
                return (false, null, "Could not reach bKash.");
            }
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private class GrantTokenRequest
        {
            [JsonPropertyName("app_key")]
            public string AppKey { get; set; } = "";

            [JsonPropertyName("app_secret")]
            public string AppSecret { get; set; } = "";
        }

        private class GrantTokenResponse
        {
            [JsonPropertyName("id_token")]
            public string? IdToken { get; set; }
        }

        private class CreatePaymentRequest
        {
            [JsonPropertyName("mode")]
            public string Mode { get; set; } = "";

            [JsonPropertyName("payerReference")]
            public string? PayerReference { get; set; }

            [JsonPropertyName("callbackURL")]
            public string CallbackURL { get; set; } = "";

            [JsonPropertyName("agreementID")]
            public string? AgreementID { get; set; }

            [JsonPropertyName("amount")]
            public string? Amount { get; set; }

            [JsonPropertyName("currency")]
            public string? Currency { get; set; }

            [JsonPropertyName("intent")]
            public string? Intent { get; set; }

            [JsonPropertyName("merchantInvoiceNumber")]
            public string? MerchantInvoiceNumber { get; set; }
        }

        private class CreatePaymentResponse
        {
            [JsonPropertyName("statusCode")]
            public string? StatusCode { get; set; }

            [JsonPropertyName("statusMessage")]
            public string? StatusMessage { get; set; }

            [JsonPropertyName("paymentID")]
            public string? PaymentID { get; set; }

            [JsonPropertyName("bkashURL")]
            public string? BkashURL { get; set; }
        }

        private class ExecuteRequest
        {
            [JsonPropertyName("paymentID")]
            public string PaymentID { get; set; } = "";
        }

        private class ExecuteAgreementResponse
        {
            [JsonPropertyName("agreementID")]
            public string? AgreementID { get; set; }
        }

        private class ExecutePaymentResponse
        {
            [JsonPropertyName("statusCode")]
            public string? StatusCode { get; set; }

            [JsonPropertyName("statusMessage")]
            public string? StatusMessage { get; set; }

            [JsonPropertyName("trxID")]
            public string? TrxID { get; set; }

            [JsonPropertyName("transactionStatus")]
            public string? TransactionStatus { get; set; }
        }
    }
}
