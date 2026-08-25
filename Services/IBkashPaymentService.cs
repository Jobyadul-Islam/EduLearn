using System.Threading.Tasks;

namespace EduLearn.Services
{
    public class BkashCreateResult
    {
        public bool Success { get; set; }
        public string? PaymentId { get; set; }
        public string? BkashUrl { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class BkashAgreementExecuteResult
    {
        public bool Success { get; set; }
        public string? AgreementId { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class BkashPaymentExecuteResult
    {
        public bool Success { get; set; }
        public string? TrxId { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public interface IBkashPaymentService
    {
        bool IsConfigured { get; }

        Task<string?> GrantTokenAsync();

        // mode "0000" — the one-time customer consent step that must precede a payment
        Task<BkashCreateResult> CreateAgreementAsync(string idToken, string payerReference, string callbackUrl);

        Task<BkashAgreementExecuteResult> ExecuteAgreementAsync(string idToken, string paymentId);

        // mode "0011" — the actual charge, made against an already-executed agreement
        Task<BkashCreateResult> CreatePaymentAsync(string idToken, string payerReference, string agreementId, decimal amount, string invoiceNumber, string callbackUrl);

        Task<BkashPaymentExecuteResult> ExecutePaymentAsync(string idToken, string paymentId);
    }
}
