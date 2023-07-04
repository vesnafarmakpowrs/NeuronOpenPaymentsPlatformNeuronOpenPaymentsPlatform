namespace TAG.Networking.OpenPaymentsPlatform
{
	/// <summary>
	/// Reference to an IBAN Account.
	/// </summary>
	public class AccountReference
	{
		/// <summary>
		/// Reference to an IBAN Account.
		/// </summary>
		/// <param name="Iban">IBAN Account number.</param>
		/// <param name="Currency">Currency</param>
		public AccountReference(string Iban, string Currency)
		{
			this.Iban = Iban;
			this.Currency = Currency;
		}

		/// <summary>
		/// IBAN Account number.
		/// </summary>
		public string Iban { get; }

		/// <summary>
		/// Currency
		/// </summary>
		public string Currency { get; }
	}
}
