using System;
using TAG.Networking.OpenPaymentsPlatform;
using Waher.Persistence.Attributes;

namespace TAG.Payments.OpenPaymentsPlatform
{
	/// <summary>
	/// Contains a record of an outbound payment.
	/// </summary>
	[CollectionName("OPP_OutboundPayments")]
	[ArchivingTime(3653)]
	[Index("Created")]
	[Index("Paid", "Created")]
	[Index("PaymentId")]
	[Index("BasketId", "PaymentId")]
	[Index("TransactionStatus", "Created")]
	public class OutboundPayment
	{
		/// <summary>
		/// Contains a record of an outbound payment.
		/// </summary>
		public OutboundPayment()
		{
		}

		/// <summary>
		/// Object ID
		/// </summary>
		[ObjectId]
		public string ObjectId { get; set; }

		/// <summary>
		/// When record was created.
		/// </summary>
		public DateTime Created { get; set; }

		/// <summary>
		/// When record was last updated.
		/// </summary>
		public DateTime Updated { get; set; }

		/// <summary>
		/// When record was paid.
		/// </summary>
		public DateTime Paid { get; set; }

		/// <summary>
		/// Account performing the payment
		/// </summary>
		public string Account { get; set; }

		/// <summary>
		/// Payment ID
		/// </summary>
		public string PaymentId { get; set; }

		/// <summary>
		/// Basket ID
		/// </summary>
		public string BasketId { get; set; }

		/// <summary>
		/// Message returned from payment service.
		/// </summary>
		public string Message { get; set; }

		/// <summary>
		/// Transaction status
		/// </summary>
		public PaymentStatus TransactionStatus { get; set; }

		/// <summary>
		/// Payment product
		/// </summary>
		public PaymentProduct Product { get; set; }

		public decimal Amount { get; set; }

		public string Currency { get; set; }

		/// <summary>
		/// From which bank account the payment is/was made.
		/// </summary>
		public string FromBankAccount { get; set; }

		/// <summary>
		/// From which bank the payment is/was made.
		/// </summary>
		public string FromBank { get; set; }

		/// <summary>
		/// To which bank account the payment is/was made.
		/// </summary>
		public string ToBankAccount { get; set; }

		/// <summary>
		/// To which bank the payment is/was made.
		/// </summary>
		public string ToBank { get; set; }

		/// <summary>
		/// Name of account receiving the money
		/// </summary>
		public string ToBankAccountName { get; set; }

		/// <summary>
		/// Text message sent with payment.
		/// </summary>
		public string TextMessage { get; set; }

	}
}
