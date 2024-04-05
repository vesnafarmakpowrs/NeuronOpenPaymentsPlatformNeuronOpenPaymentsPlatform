using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TAG.Networking.OpenPaymentsPlatform;
using Waher.Content;
using Waher.Events;
using Waher.IoTGateway;
using Waher.Networking.HTTP;
using Waher.Persistence;
using Waher.Script;
using Waher.Security;

namespace TAG.Payments.OpenPaymentsPlatform.Api
{
	/// <summary>
	/// Return payments
	/// </summary>
	public class ReturnPayments : HttpAsynchronousResource, IHttpPostMethod
	{
		/// <summary>
		/// Return payments
		/// </summary>
		public ReturnPayments()
			: base("/OpenPaymentsPlatform/" + nameof(ReturnPayments))
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
		
		private static readonly Expression generateEDaler = new Expression(
			Resources.LoadResourceAsText(typeof(ReturnPayments).Namespace + ".GenerateEDaler.ws"));

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
            ) = await SignPayments.PrepareRequest(Request, this.ResourceName, false);

			Task _ = Task.Run(async () =>
			{
				try
				{
					string[] TabIDs = ClientEvents.GetTabIDsForLocation("/OpenPaymentsPlatform/OutgoingPayments.md");
					bool PushToClients = TabIDs.Length > 0;
					DateTime TP = DateTime.UtcNow;
					StringBuilder Error = null;

					foreach (OutboundPayment Payment in Payments)
					{
						try
						{
							await Client.DeletePaymentInitiation(Payment.Product, Payment.PaymentId, Operation);
						}
						catch (Exception ex)
						{
							Log.Error("Unable to delete payment initiation in Open Payments Platform back-end. Error reported: " + ex.Message);
						}

						try
						{
							await generateEDaler.EvaluateAsync(new Variables()
							{
								{ "To", Payment.Account },
								{ "Amount", Payment.Amount },
								{ "Currency", Payment.Currency },
								{ "ExpiresDays", 365 },
								{ "FreeText", "Returned" },
								{ "ManagerPassword", Password },
								{ "Request", Request },
								{ "User", User }
							});
						}
						catch (Exception ex)
						{
							if (Error is null)
								Error = new StringBuilder();
							else
								Error.AppendLine();

							Error.Append(ex.Message);

							continue;
						}

						Payment.TransactionStatus = PaymentStatus.CANC;
						Payment.Paid = DateTime.MaxValue;
						Payment.Updated = TP;

						await Database.Update(Payment);

						// TODO: Regenerate eDaler for client.

						if (PushToClients)
						{
							await ClientEvents.PushEvent(TabIDs, "PaymentUpdated", JSON.Encode(new Dictionary<string, object>()
							{
								{ "objectId", Payment.ObjectId },
								{ "updatedDate", Payment.Updated.ToShortDateString() },
								{ "updatedTime", Payment.Updated.ToLongTimeString() },
								{ "status", Payment.TransactionStatus },
								{ "isPaid", true }
							}, false), true, "User", "Admin.Payments.Paiwise.OpenPaymentsPlatform");
						}
					}

					if (!(Error is null))
					{
						await ClientEvents.PushEvent(TabIDs, "PaymentError", 
							Error.ToString(), false, "User", "Admin.Payments.Paiwise.OpenPaymentsPlatform");
					}

					await Response.Return(string.Empty);
				}
				catch (Exception ex)
				{
					Log.Critical(ex);
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
