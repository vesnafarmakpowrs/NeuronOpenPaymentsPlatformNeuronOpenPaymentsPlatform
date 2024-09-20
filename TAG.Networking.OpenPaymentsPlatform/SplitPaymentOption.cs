namespace TAG.Networking.OpenPaymentsPlatform
{
    public class SplitPaymentOption
    {
        public string BankAccount { get; set; }
        public string AccountName { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
    }
}
