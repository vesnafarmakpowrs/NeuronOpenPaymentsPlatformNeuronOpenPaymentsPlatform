namespace TAG.Networking.OpenPaymentsPlatform
{
    public class PaymentOption
    {
        public string BankAccount { get; set; }
        public string AccountName { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public string PaymentId { get; set; }
        public string ErrorMessage { get; set; }
        public PaymentStatus Status { get; set; }
        public string StringStatus { get => Status.ToString(); }
    }
}
