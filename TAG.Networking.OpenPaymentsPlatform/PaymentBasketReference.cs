namespace TAG.Networking.OpenPaymentsPlatform
{
	/// <summary>
	/// Payment basket status.
	/// </summary>
	public enum PaymentBasketStatus
	{
		/// <summary>
		/// 'AcceptedSettlementInProcess' - All preceding checks such as technical 
		/// validation and customer profile were successful and therefore the payment 
		/// initiation has been accepted for execution.
		/// </summary>
		ACSP,

		/// <summary>
		/// 'AcceptedTechnicalValidation' - Authentication and syntactical and 
		/// semantical validation are successful.
		/// </summary>
		ACTC,

		/// <summary>
		/// 'AcceptedWithChange' - Instruction is accepted but a change will be made, 
		/// such as date or remittance not sent.
		/// </summary>
		ACWC,

		/// <summary>
		/// 'Received' - Payment initiation has been received by the receiving agent.
		/// </summary>
		RCVD,

		/// <summary>
		/// 'Rejected' - Payment initiation or individual transaction included in the
		/// payment initiation has been rejected.
		/// </summary>
		RJCT
	}

	/// <summary>
	/// Contains a reference to a payment initiation.
	/// </summary>
	public class PaymentBasketReference : ObjectWithLinks
	{
		/// <summary>
		/// Contains a reference to a payment initiation.
		/// </summary>
		/// <param name="TransactionStatus">Transaction status.</param>
		/// <param name="BasketId">Payment ID</param>
		/// <param name="Links">Links</param>
		/// <param name="Message">Unstructured message.</param>
		/// <param name="Messages">Error messages</param>
		public PaymentBasketReference(PaymentBasketStatus TransactionStatus,
			string BasketId, Links Links, string Message, TppMessage[] Messages)
			: base(Links)
		{
			this.TransactionStatus = TransactionStatus;
			this.BasketId = BasketId;
			this.Message = Message;
			this.Messages = Messages;
		}

		/// <summary>
		/// Basket status.
		/// </summary>
		public PaymentBasketStatus TransactionStatus { get; }

		/// <summary>
		/// Basket ID
		/// </summary>
		public string BasketId { get; }

		/// <summary>
		/// Unstructured message.
		/// </summary>
		public string Message { get; }

		/// <summary>
		/// Error messages
		/// </summary>
		public TppMessage[] Messages { get; }
	}
}
