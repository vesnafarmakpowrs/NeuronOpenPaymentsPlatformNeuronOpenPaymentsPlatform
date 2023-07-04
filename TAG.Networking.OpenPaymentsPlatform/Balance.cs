namespace TAG.Networking.OpenPaymentsPlatform
{
	/// <summary>
	/// Information about a Balance.
	/// </summary>
	public class Balance
	{
		/// <summary>
		/// Information about a Balance.
		/// </summary>
		/// <param name="BalanceAmount">BalanceAmount</param>
		/// <param name="BalanceType">BalanceType</param>
		/// <param name="CreditLimitIncluded">Credit Limit Included</param>
		public Balance(BalanceAmount BalanceAmount, string BalanceType, bool CreditLimitIncluded)
		{
			this.BalanceAmount = BalanceAmount;
			this.BalanceType = BalanceType;
			this.CreditLimitIncluded = CreditLimitIncluded;

		}
		/// <summary>
		/// Balance Amount 
		/// </summary>
		public BalanceAmount BalanceAmount { get; }
		
		/// <summary>
		/// Balance Type 
		/// </summary>
		public string BalanceType { get; }
		
		/// <summary>
		/// Credit Limit Included 
		/// </summary>
		public bool CreditLimitIncluded { get; }
	}
}
