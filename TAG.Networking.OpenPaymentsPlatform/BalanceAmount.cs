namespace TAG.Networking.OpenPaymentsPlatform
{
	/// <summary>
	/// Amount and currency.
	/// </summary>
	public class BalanceAmount
	{
		/// <summary>
		/// Amount and currency.
		/// </summary>
		/// <param name="BalanceAmount">BalanceAmount</param>
		/// <param name="BalanceType">BalanceType</param>
		/// <param name="CreditLimitIncluded">Credit Limit Included</param>
		public BalanceAmount(string Currency, decimal Amount)
		{
			this.Currency = Currency;
			this.Amount = Amount;
		}

		/// <summary>
		/// Currency 
		/// </summary>
		public string Currency { get; }
		
		/// <summary>
		/// Amount 
		/// </summary>
		public decimal Amount { get; }
	}
}
