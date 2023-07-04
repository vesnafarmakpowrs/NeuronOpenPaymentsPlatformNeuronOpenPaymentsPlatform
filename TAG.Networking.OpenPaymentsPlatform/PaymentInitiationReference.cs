namespace TAG.Networking.OpenPaymentsPlatform
{
	/// <summary>
	/// Enumeration of different payment products available.
	/// </summary>
	public enum PaymentProduct
	{
		/// <summary>
		/// This is a non-euro payment within one country.
		/// </summary>
		domestic,

		/// <summary>
		/// This is a EURO payment from one EURO ASPSP to another
		/// </summary>
		sepa_credit_transfers,

		/// <summary>
		/// This is an international payment
		/// </summary>
		international
	}

	/// <summary>
	/// Payment status.
	/// </summary>
	public enum PaymentStatus
	{
		/// <summary>
		/// 'AcceptedSettlementCompleted' - Settlement on the creditor's account has 
		/// been completed.
		/// </summary>
		ACCC,

		/// <summary>
		/// ACCP': 'AcceptedCustomerProfile' - Preceding check of technical 
		/// validation was successful. Customer profile check was also successful.
		/// </summary>
		ACCP,

		/// <summary>
		/// 'ACSC': 'AcceptedSettlementCompleted' - Settlement on the debtor's 
		/// account has been completed.
		/// 
		/// Usage: this can be used by the first agent to report to the debtor that 
		/// the transaction has been completed.
		/// 
		/// Warning: this status is provided for transaction status reasons, not for 
		/// financial information. It can only be used after bilateral agreement.
		/// </summary>
		ACSC,

		/// <summary>
		/// 'AcceptedSettlementInProcess' - All preceding checks such as technical 
		/// validation and customer profile were successful and therefore the payment 
		/// initiation has been accepted for execution.
		/// </summary>
		ACSP,

		/// <summary>
		/// 'AcceptedTechnicalValidation' - Authentication and syntactical and semantical 
		/// validation are successful.
		/// </summary>
		ACTC,

		/// <summary>
		/// 'AcceptedWithChange' - Instruction is accepted but a change will be made, 
		/// such as date or remittance not sent.
		/// </summary>
		ACWC,

		/// <summary>
		/// 'AcceptedWithoutPosting' - Payment instruction included in the credit transfer 
		/// is accepted without being posted to the creditor customer's account.
		/// </summary>
		ACWP,

		/// <summary>
		/// 'Received' - Payment initiation has been received by the receiving agent.
		/// </summary>
		RCVD,

		/// <summary>
		/// 'Pending' - Payment initiation or individual transaction included in the payment 
		/// initiation is pending. Further checks and status update will be performed.
		/// </summary>
		PDNG,

		/// <summary>
		/// 'Rejected' - Payment initiation or individual transaction included in the 
		/// payment initiation has been rejected.
		/// </summary>
		RJCT,

		/// <summary>
		/// 'Cancelled' Payment initiation has been cancelled before execution 
		/// Remark: This codeis accepted as new code by ISO20022.
		/// </summary>
		CANC,

		/// <summary>
		/// 'AcceptedFundsChecked' - Preceding check of technical validation and 
		/// customer profile was successful and an automatic funds check was positive. 
		/// Remark: This code is accepted as new code by ISO20022.
		/// </summary>
		ACFC,

		/// <summary>
		/// 'PartiallyAcceptedTechnical' Correct The payment initiation needs 
		/// multiple authentications, where some but not yet all have been performed. 
		/// Syntactical and semantical validations are successful. Remark: This code is 
		/// accepted as new code by ISO20022.
		/// </summary>
		PATC,

		/// <summary>
		/// 'PartiallyAccepted' - A number of transactions have been accepted, 
		/// whereas another number of transactions have not yet achieved 'accepted' 
		/// status. Remark: This code may be used only in case of bulk payments. It 
		/// is only used in a situation where all mandated authorisations have been 
		/// applied, but some payments have been rejected.
		/// </summary>
		PART
	}

	/// <summary>
	/// Contains a reference to a payment initiation.
	/// </summary>
	public class PaymentInitiationReference : ObjectWithLinks
	{
		/// <summary>
		/// Contains a reference to a payment initiation.
		/// </summary>
		/// <param name="TransactionStatus">Transaction status.</param>
		/// <param name="PaymentId">Payment ID</param>
		/// <param name="Links">Links</param>
		/// <param name="Message">Unstructured message.</param>
		/// <param name="Messages">Error messages</param>
		public PaymentInitiationReference(PaymentStatus TransactionStatus,
			string PaymentId, Links Links, string Message, TppMessage[] Messages)
			: base(Links)
		{
			this.TransactionStatus = TransactionStatus;
			this.PaymentId = PaymentId;
			this.Message = Message;
			this.Messages = Messages;
		}

		/// <summary>
		/// Transaction status.
		/// </summary>
		public PaymentStatus TransactionStatus { get; }

		/// <summary>
		/// Payment ID
		/// </summary>
		public string PaymentId { get; }

		/// <summary>
		/// Unstructured message.
		/// </summary>
		public string Message { get; }

		/// <summary>
		/// Error messages.
		/// </summary>
		public TppMessage[] Messages { get; }
	}
}
