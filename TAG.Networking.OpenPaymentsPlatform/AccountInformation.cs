namespace TAG.Networking.OpenPaymentsPlatform
{
	/// <summary>
	/// Information about an IBAN Account.
	/// </summary>
	public class AccountInformation : AccountReference
	{
		/// <summary>
		/// Information about an IBAN Account.
		/// </summary>
		/// <param name="ResourceId">Resource ID</param>
		/// <param name="Iban">IBAN Account number.</param>
		/// <param name="BBan">BBAN</param>
		/// <param name="Currency">Currency</param>
		/// <param name="Bic">Financial Institution</param>
		/// <param name="Balance">Balance</param>
		/// <param name="CashAccountType">CashAccountType</param>
		/// <param name="Name">Name</param>
		/// <param name="OwnerName">OwnerName</param>
		/// <param name="Product">Product</param>
		/// <param name="Status">Status</param>
		/// <param name="Usage">Usage</param>
		public AccountInformation(string ResourceId, string Iban, string Currency,
			string Bban, string Bic, Balance[] Balance, string CashAccountType, string Name
			, string OwnerName, string Product, string Status, string Usage)
			: base(Iban, Currency)
		{
			this.ResourceID = ResourceId;
			this.Bban = Bban;
			this.Bic = Bic;
			this.Balances = Balance;
			this.CashAccountType = CashAccountType;
			this.Name = Name;
			this.OwnerName = OwnerName;
			this.Product = Product;
			this.Status = Status;
			this.Usage = Usage;
		}

		/// <summary>
		/// Resource ID
		/// </summary>
		public string ResourceID { get; }

		/// <summary>
		/// BBAN number.
		/// </summary>
		public string Bban { get; }

		/// <summary>
		/// Financial Institution
		/// </summary>
		public string Bic { get; }

        /// <summary>
        /// Balances
        /// </summary>
        public decimal Balance {
			get
			{
				if (!(Balances is null) && Balances.Length > 0)
					foreach (Balance balance in Balances)
						if (balance.BalanceType == "interimAvailable")
							return balance.BalanceAmount.Amount;
				return 0;
			} }
        /// <summary>
        /// Balances
        /// </summary>
        public Balance[] Balances { get; }

		/// <summary>
		/// Cash Account Type
		/// </summary>
		public string CashAccountType { get; }

		/// <summary>
		/// Name
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Owner Name
		/// </summary>
		public string OwnerName { get; }

		/// <summary>
		/// Product
		/// </summary>
		public string Product { get; }

		/// <summary>
		/// Status
		/// </summary>
		public string Status { get; }

		/// <summary>
		/// Usage
		/// </summary>
		public string Usage { get; }


	}
}
