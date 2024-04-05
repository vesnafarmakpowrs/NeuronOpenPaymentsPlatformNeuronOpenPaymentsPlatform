using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TAG.Networking.OpenPaymentsPlatform;
using Waher.Content;
using Waher.Events;
using Waher.IoTGateway;
using Waher.Networking.HTTP;
using Waher.Persistence;
using Waher.Security;

namespace TAG.Payments.OpenPaymentsPlatform.Api
{
	/// <summary>
	/// Retry payments
	/// </summary>
	public class RetryPayments : HttpAsynchronousResource, IHttpPostMethod
	{
		/// <summary>
		/// Retry payments
		/// </summary>
		public RetryPayments()
			: base("/OpenPaymentsPlatform/" + nameof(RetryPayments))
		{
		}

		/// <summary>
		/// If sub-paths are handled
		/// </summary>
		public override bool HandlesSubPaths => false;

		/// <summary>
		/// If an HTTP session is required for accessing the resource.
		/// </summary>
		public override bool UserSessions => true;

		/// <summary>
		/// If POSt method is allowed
		/// </summary>
		public bool AllowsPOST => true;

		/// <summary>
		/// Method called when a used calls the resource using the POST method.
		/// </summary>
		/// <param name="Request">HTTP Request object.</param>
		/// <param name="Response">HTTP Response object.</param>
		public async Task POST(HttpRequest Request, HttpResponse Response)
		{
			(
				ServiceConfiguration Configuration,
				OperationInformation Operation,
				OpenPaymentsPlatformClient Client,
				OutboundPayment[] Payments,
				string[] PaymentIds,
				string TabId,
				string Password,
				IUser User,
                bool RequestFromMobilePhone
            ) = await SignPayments.PrepareRequest(Request, this.ResourceName, true);

			Task _ = Task.Run(async () =>
			{
				try
				{
					string[] TabIDs = ClientEvents.GetTabIDsForLocation("/OpenPaymentsPlatform/OutgoingPayments.md");

					foreach (OutboundPayment Payment in Payments)
					{
						try
						{
							await Client.DeletePaymentInitiation(Payment.Product,
								Payment.PaymentId, Operation);
						}
						catch (Exception ex)
						{
							Log.Critical(ex);
						}

						PaymentInitiationReference PaymentInitiationReference = 
							await Client.CreatePaymentInitiation(Payment.Product,
							Payment.Amount, Payment.Currency, 
							Configuration.NeuronBankAccountIban, Payment.Currency,
							Payment.ToBankAccount, Payment.Currency,
							Payment.ToBankAccountName, Payment.TextMessage, Operation);

						DateTime TP = DateTime.UtcNow;

						Payment.Updated = TP;
						Payment.Paid = DateTime.MinValue;
						Payment.PaymentId = PaymentInitiationReference.PaymentId;
						Payment.BasketId = null;
						Payment.Message = PaymentInitiationReference.Message;
						Payment.TransactionStatus = PaymentInitiationReference.TransactionStatus;
						Payment.FromBankAccount = Configuration.NeuronBankAccountIban;
						Payment.FromBank = Operation.ServiceProvider;

						await Database.Update(Payment);

						await ClientEvents.PushEvent(TabIDs, "PaymentRetried", JSON.Encode(new Dictionary<string, object>()
							{
								{ "objectId", Payment.ObjectId },
								{ "paymentId", Payment.PaymentId },
								{ "updatedDate", Payment.Updated.ToShortDateString() },
								{ "updatedTime", Payment.Updated.ToLongTimeString() },
								{ "status", Payment.TransactionStatus }
							}, false), true, "User", "Admin.Payments.Paiwise.OpenPaymentsPlatform");
					}

					await Response.Return(string.Empty);
				}
				catch (Exception ex)
				{
					await Response.SendResponse(ex);
				}
				finally
				{
					OpenPaymentsPlatformServiceProvider.Dispose(Client, Configuration.OperationMode);
				}
			});
		}

	}
}
