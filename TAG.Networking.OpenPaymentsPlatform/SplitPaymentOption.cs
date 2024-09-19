namespace TAG.Networking.OpenPaymentsPlatform
{
    public class SplitPaymentOption
    {
        public string Iban { get; set; }
        public string AccountName { get; set; }
        public decimal Amount { get; set; }
        public string TextMessage { get; set; }
    }
}
