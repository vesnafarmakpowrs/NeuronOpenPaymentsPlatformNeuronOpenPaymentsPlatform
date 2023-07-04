namespace TAG.Networking.OpenPaymentsPlatform
{
	/// <summary>
	/// Contains information about a payment process.
	/// </summary>
	public class PaymentTransactionStatus
	{
		/// <summary>
		/// Contains information about a payment process.
		/// </summary>
		/// <param name="Status">Status</param>
		/// <param name="Messages">Any (error) messages.</param>
		public PaymentTransactionStatus(PaymentStatus Status, TppMessage[] Messages)
		{
			this.Status = Status;
			this.Messages = Messages;
		}

		/// <summary>
		/// Status
		/// </summary>
		public PaymentStatus Status { get; }

		/// <summary>
		/// Any (error) messages
		/// </summary>
		public TppMessage[] Messages { get; }
	}
}
