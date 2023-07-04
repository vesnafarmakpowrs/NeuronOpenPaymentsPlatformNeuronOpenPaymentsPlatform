namespace TAG.Networking.OpenPaymentsPlatform
{
	/// <summary>
	/// Contains information about a payment basket process.
	/// </summary>
	public class BasketTransactionStatus
	{
		/// <summary>
		/// Contains information about a payment basket process.
		/// </summary>
		/// <param name="Status">Status</param>
		/// <param name="Messages">Any (error) messages.</param>
		public BasketTransactionStatus(PaymentBasketStatus Status, TppMessage[] Messages)
		{
			this.Status = Status;
			this.Messages = Messages;
		}

		/// <summary>
		/// Status
		/// </summary>
		public PaymentBasketStatus Status { get; }

		/// <summary>
		/// Any (error) messages
		/// </summary>
		public TppMessage[] Messages { get; }
	}
}
