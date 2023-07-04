using System;

namespace TAG.Networking.OpenPaymentsPlatform
{
	/// <summary>
	/// Containst information about a consent request.
	/// </summary>
	public class ConsentRequest
	{
		/// <summary>
		/// Containst information about a consent request.
		/// </summary>
		/// <param name="ConsentID">Consent ID</param>
		/// <param name="Status">Status</param>
		/// <param name="AccountAccess">Access to accounts</param>
		/// <param name="BalanceAccess">Access to balance</param>
		/// <param name="TransactionsAccess">Access to transactions</param>
		/// <param name="Recurring">If the consent can be used multiple times or not.</param>
		/// <param name="ValidUntil">Until when the consent is requested.</param>
		/// <param name="FrequencyPerDay">Number of usages per day requested.</param>
		/// <param name="LastActionDate">Last action date.</param>
		public ConsentRequest(string ConsentID, ConsentStatusValue Status,
			AccountReference[] AccountAccess, AccountReference[] BalanceAccess,
			AccountReference[] TransactionsAccess, bool Recurring, DateTime ValidUntil,
			int FrequencyPerDay, DateTime LastActionDate)
		{
			this.ConsentID = ConsentID;
			this.Status = Status;
			this.AccountAccess = AccountAccess;
			this.BalanceAccess = BalanceAccess;
			this.TransactionsAccess = TransactionsAccess;
			this.Recurring = Recurring;
			this.ValidUntil = ValidUntil;
			this.FrequencyPerDay = FrequencyPerDay;
			this.LastActionDate = LastActionDate;
		}

		/// <summary>
		/// Consent ID
		/// </summary>
		public string ConsentID { get; }

		/// <summary>
		/// Status
		/// </summary>
		public ConsentStatusValue Status { get; }

		/// <summary>
		/// Access to accounts
		/// </summary>
		public AccountReference[] AccountAccess { get; }

		/// <summary>
		/// Access to balance
		/// </summary>
		public AccountReference[] BalanceAccess { get; }

		/// <summary>
		/// Access to transactions
		/// </summary>
		public AccountReference[] TransactionsAccess { get; }

		/// <summary>
		/// If the consent can be used multiple times or not.
		/// </summary>
		public bool Recurring { get; }

		/// <summary>
		/// Until when the consent is requested.
		/// </summary>
		public DateTime ValidUntil { get; }

		/// <summary>
		/// Number of usages per day requested.
		/// </summary>
		public int FrequencyPerDay { get; }

		/// <summary>
		/// Last action date.
		/// </summary>
		public DateTime LastActionDate { get; }
	}
}
