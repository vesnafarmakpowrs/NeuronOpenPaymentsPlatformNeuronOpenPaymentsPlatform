using System;
using System.Collections.Generic;
using System.Net;
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
	/// Sign payments
	/// </summary>
	public class SignPayments : HttpAsynchronousResource, IHttpPostMethod
	{
		/// <summary>
		/// Sign payments
		/// </summary>
		public SignPayments()
			: base("/OpenPaymentsPlatform/" + nameof(SignPayments))
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
				IUser User
			) = await PrepareRequest(Request, this.ResourceName, true);

			string UserAgent = Request.Header.UserAgent?.Value.ToLower() ?? string.Empty;
			bool FromMobildeDevice =
				UserAgent.Contains("android") ||
				UserAgent.Contains("mobile") ||
				UserAgent.Contains("iphone") ||
				UserAgent.Contains("ipad") ||
				UserAgent.Contains("windows phone");

			Task _ = Task.Run(async () =>
			{
				PaymentBasketReference Basket = null;
				AuthorizationInformation Authorization;
				AuthenticationMethod Method;
				PaymentServiceUserDataResponse PsuDataResponse;

				try
				{
					if (Payments.Length > 1)
					{
						Basket = await Client.CreatePaymentBasket(PaymentIds, Operation);

						foreach (OutboundPayment Payment in Payments)
							Payment.BasketId = Basket.BasketId;

						await Database.Update(Payments);

						Authorization = await Client.StartPaymentBasketAuthorization(Basket.BasketId, Operation);

						if (FromMobildeDevice)
						{
							Method = Authorization.GetAuthenticationMethod("mbid_same_device")
								?? Authorization.GetAuthenticationMethod("mbid")
								?? throw new ServiceUnavailableException("Unable to find a Mobile Bank ID authorization method for the operation.");
						}
						else
						{
							Method = Authorization.GetAuthenticationMethod("mbid_animated_qr_image")
								?? Authorization.GetAuthenticationMethod("mbid")
								?? Authorization.GetAuthenticationMethod("mbid_same_device")
								?? throw new ServiceUnavailableException("Unable to find a Mobile Bank ID authorization method for the operation.");
						}

						PsuDataResponse = await Client.PutPaymentBasketUserData(Basket.BasketId, Authorization.AuthorizationID,
							Method.MethodId, Operation);
					}
					else if (Payments.Length == 1)
					{
						Basket = null;
						Authorization = await Client.StartPaymentInitiationAuthorization(Payments[0].Product, 
							Payments[0].PaymentId, Operation);

						if (FromMobildeDevice)
						{
							Method = Authorization.GetAuthenticationMethod("mbid_same_device")
								?? Authorization.GetAuthenticationMethod("mbid")
								?? throw new ServiceUnavailableException("Unable to find a Mobile Bank ID authorization method for the operation.");
						}
						else
						{
							Method = Authorization.GetAuthenticationMethod("mbid_animated_qr_image")
								?? Authorization.GetAuthenticationMethod("mbid")
								?? Authorization.GetAuthenticationMethod("mbid_same_device")
								?? throw new ServiceUnavailableException("Unable to find a Mobile Bank ID authorization method for the operation.");
						}

						PsuDataResponse = await Client.PutPaymentInitiationUserData(Payments[0].Product, Payments[0].PaymentId,
							Authorization.AuthorizationID, Method.MethodId, Operation);
					}
					else
						throw new BadRequestException("No payments selected.");

					string LastImageSent = string.Empty;
					bool PaymentCodeSent = false;
					bool CreditorAccountCodeSent = false;

					await PushQrCodeIfNecessary(TabId, PsuDataResponse.Status, PsuDataResponse.ChallengeData, FromMobildeDevice,
						ref PaymentCodeSent, ref CreditorAccountCodeSent, ref LastImageSent);

					TppMessage[] ErrorMessages = PsuDataResponse.Messages;
					AuthorizationStatusValue Status = PsuDataResponse.Status;
					DateTime Start = DateTime.Now;

					while (Status != AuthorizationStatusValue.finalised &&
						Status != AuthorizationStatusValue.failed &&
						DateTime.Now.Subtract(Start).TotalMinutes < Configuration.TimeoutMinutes)
					{
						await Task.Delay(Configuration.PollingIntervalSeconds * 1000);

						AuthorizationStatus P;

						if (Basket is null)
						{
							P = await Client.GetPaymentInitiationAuthorizationStatus(Payments[0].Product, Payments[0].PaymentId,
								Authorization.AuthorizationID, Operation);
						}
						else
						{
							P = await Client.GetPaymentBasketAuthorizationStatus(Basket.BasketId,
								Authorization.AuthorizationID, Operation);
						}

						Status = P.Status;
						ErrorMessages = P.Messages;

						if (!(P.ChallengeData is null))
						{
							await PushQrCodeIfNecessary(TabId, Status, P.ChallengeData, FromMobildeDevice,
								ref PaymentCodeSent, ref CreditorAccountCodeSent, ref LastImageSent);
						}
					}

					StringBuilder Message = new StringBuilder();
					DateTime TP = DateTime.UtcNow;
					string[] TabIDs = ClientEvents.GetTabIDsForLocation("/OpenPaymentsPlatform/OutgoingPayments.md");
					bool PushToClients = TabIDs.Length > 0;

					if (Basket is null)
					{
						PaymentTransactionStatus TransactionStatus = await Client.GetPaymentInitiationStatus(Payments[0].Product,
							Payments[0].PaymentId, Operation);
						bool IsPaid = Status == AuthorizationStatusValue.finalised &&
							TransactionStatus.Status != PaymentStatus.RJCT &&
							TransactionStatus.Status != PaymentStatus.CANC;

						Payments[0].TransactionStatus = TransactionStatus.Status;

						if (!IsPaid && !(TransactionStatus.Messages is null))
						{
							foreach (TppMessage Msg2 in TransactionStatus.Messages)
								Message.AppendLine(Msg2.Text);
						}

						if (IsPaid)
							Payments[0].Paid = TP;

						Payments[0].Updated = TP;

						await Database.Update(Payments[0]);

						if (PushToClients)
						{
							await ClientEvents.PushEvent(TabIDs, "PaymentUpdated", JSON.Encode(new Dictionary<string, object>()
								{
									{ "objectId", Payments[0].ObjectId },
									{ "updatedDate", Payments[0].Updated.ToShortDateString() },
									{ "updatedTime", Payments[0].Updated.ToLongTimeString() },
									{ "status", Payments[0].TransactionStatus },
									{ "isPaid", IsPaid }
								}, false), true, "User", "Admin.Payments.Paiwise.OpenPaymentsPlatform");
						}
					}
					else
					{
						BasketTransactionStatus BasketStatus = await Client.GetPaymentBasketStatus(Basket.BasketId, Operation);
						bool IsPaid = Status == AuthorizationStatusValue.finalised &&
							BasketStatus.Status != PaymentBasketStatus.RJCT;
						
						if (!IsPaid && !(ErrorMessages is null))
						{
							foreach (TppMessage Msg2 in ErrorMessages)
								Message.AppendLine(Msg2.Text);

							if (Message.Length == 0)
								Message.AppendLine("Payments were rejected.");
						}

						foreach (OutboundPayment Payment in Payments)
						{
							try
							{
								PaymentTransactionStatus Status2 = await Client.GetPaymentInitiationStatus(Payment.Product, Payment.PaymentId, Operation);
								Payment.TransactionStatus = Status2.Status;

								IsPaid = Status2.Status != PaymentStatus.RJCT &&
									Status2.Status != PaymentStatus.CANC;

								if (!(Status2.Messages is null))
								{
									foreach (TppMessage Msg2 in Status2.Messages)
									{
										Message.Append(Payment.PaymentId);
										Message.Append(": ");
										Message.AppendLine(Msg2.Text);
									}
								}
							}
							catch (Exception ex)
							{
								Log.Critical(ex);
								IsPaid = false;
								Message.Append(Payment.PaymentId);
								Message.Append(": ");
								Message.AppendLine(ex.Message);
							}

							if (IsPaid)
								Payment.Paid = TP;
							else
								Payment.BasketId = string.Empty;

							Payment.Updated = TP;

							await Database.Update(Payment);

							if (PushToClients)
							{
								await ClientEvents.PushEvent(TabIDs, "PaymentUpdated", JSON.Encode(new Dictionary<string, object>()
								{
									{ "objectId", Payment.ObjectId },
									{ "updatedDate", Payment.Updated.ToShortDateString() },
									{ "updatedTime", Payment.Updated.ToLongTimeString() },
									{ "status", Payment.TransactionStatus },
									{ "isPaid", IsPaid }
								}, false), true, "User", "Admin.Payments.Paiwise.OpenPaymentsPlatform");
							}
						}
					}

					string Msg = Message.ToString().Trim();
					Log.Informational(Msg);
					if (!string.IsNullOrEmpty(Msg))
					{
						if (PushToClients)
						{
							await ClientEvents.PushEvent(TabIDs, "PaymentError", Msg, false,
								"User", "Admin.Payments.Paiwise.OpenPaymentsPlatform");
						}

						throw new UnavailableForLegalReasonsException(Msg);
					}

					await Response.Return(string.Empty);
				}
				catch (Exception ex)
				{
					await Response.SendResponse(ex);

					if (!(Basket is null))
					{
						try
						{
							await Client.DeletePaymentBasket(Basket.BasketId, Operation);

							foreach (OutboundPayment Payment in Payments)
								Payment.BasketId = null;

							await Database.Update(Payments);
						}
						catch (Exception ex2)
						{
							Log.Critical(ex2);
						}
					}
				}
				finally
				{
					OpenPaymentsPlatformServiceProvider.Dispose(Client, Configuration.OperationMode);
				}
			});
		}

		private static Task PushQrCodeIfNecessary(string TabId, AuthorizationStatusValue Status,
			ChallengeData ChallengeData, bool FromMobileDevice, ref bool PaymentCodeSent, ref bool CreditorAccountCodeSent,
			ref string LastImageUrl)
		{
			if (ChallengeData is null)
				return Task.CompletedTask;

			string Url;
			bool UrlIsImage;

			if (!string.IsNullOrEmpty(ChallengeData.ImageUrl))
			{
				Url = ChallengeData.ImageUrl;
				UrlIsImage = true;

				if (Url == LastImageUrl)
					return Task.CompletedTask;

				LastImageUrl = Url;
			}
			else if (!string.IsNullOrEmpty(ChallengeData.BankIdURL))
			{
				Url = ChallengeData.BankIdURL;
				UrlIsImage = false;
			}
			else
				return Task.CompletedTask;

			switch (Status)
			{
				case AuthorizationStatusValue.started:
				case AuthorizationStatusValue.authenticationStarted:
					if (PaymentCodeSent && !UrlIsImage)
						break;

					PaymentCodeSent = true;

					return ClientEvents.PushEvent(new string[] { TabId }, "ShowQRCode",
						JSON.Encode(new Dictionary<string, object>()
						{
							{ "url", Url },
							{ "urlIsImage", UrlIsImage },
							{ "fromMobileDevice", FromMobileDevice },
							{ "title", "Authorize payment" },
							{ "message", "Scan the following QR-code with your Bank-ID app, or click on it if your Bank-ID is installed on your computer." },
						}, false), true, "User", "Admin.Payments.Paiwise.OpenPaymentsPlatform");

				case AuthorizationStatusValue.authoriseCreditorAccountStarted:
					if (CreditorAccountCodeSent && !UrlIsImage)
						break;

					CreditorAccountCodeSent = true;

					return ClientEvents.PushEvent(new string[] { TabId }, "ShowQRCode",
						JSON.Encode(new Dictionary<string, object>()
						{
							{ "url", Url },
							{ "urlIsImage", UrlIsImage },
							{ "fromMobileDevice", FromMobileDevice },
							{ "title", "Authorize recipient" },
							{ "message", "Scan the following QR-code with your Bank-ID app, or click on it if your Bank-ID is installed on your computer." },
						}, false), true, "User", "Admin.Payments.Paiwise.OpenPaymentsPlatform");
			}

			return Task.CompletedTask;
		}

		internal static async Task<(ServiceConfiguration, OperationInformation, OpenPaymentsPlatformClient, OutboundPayment[], string[], string, string, IUser)>
			PrepareRequest(HttpRequest Request, string ResourceName, bool OutboundPayment)
		{
			IUser User = null;

			if (Request.Session is null ||
				!Request.Session.TryGetVariable("User", out Variable v) ||
				((User = v.ValueObject as IUser) is null) ||
				!User.HasPrivilege(OpenPaymentsPlatformServiceProvider.RequiredPrivilege))
			{
				throw ForbiddenException.AccessDenied(ResourceName, User?.UserName ?? string.Empty, OpenPaymentsPlatformServiceProvider.RequiredPrivilege);
			}

			if (!(await Request.DecodeDataAsync() is Dictionary<string, object> Query))
				throw new BadRequestException("Expected JSON object.");

			if (!Query.TryGetValue("objectIds", out object Obj) || !(Obj is Array ObjectIds))
				throw new BadRequestException("Expected array of Object IDs.");

			if (!Query.TryGetValue("tabId", out Obj) || !(Obj is string TabId))
				throw new BadRequestException("Tab ID not in request.");

			if (!Query.TryGetValue("password", out Obj) || !(Obj is string Password))
				Password = string.Empty;

			List<string> PaymentIds = new List<string>();
			List<OutboundPayment> Payments = new List<OutboundPayment>();
			int i, c = ObjectIds.Length;

			for (i = 0; i < c; i++)
			{
				string ObjectId = ObjectIds.GetValue(i)?.ToString() ?? String.Empty;
				OutboundPayment Payment = await Database.LoadObject<OutboundPayment>(ObjectId)
					?? throw new NotFoundException("Payment with Object ID " + ObjectId + " not found.");

				if (!string.IsNullOrEmpty(Payment.BasketId))
					throw new ForbiddenException("Payment " + Payment.PaymentId + " already in a payment basket.");

				if (Payment.Paid != DateTime.MinValue)
					throw new ForbiddenException("Payment " + Payment.PaymentId + " already paid.");

				PaymentIds.Add(Payment.PaymentId);
				Payments.Add(Payment);
			}

			i = Request.RemoteEndPoint.LastIndexOf(':');
			if (i < 0 || !IPAddress.TryParse(Request.RemoteEndPoint.Substring(0, i), out IPAddress ClientIpAddress))
				throw new ForbiddenException("Request must be made from an IP Client.");

			string UserAgent = Request.Header.UserAgent?.Value;
			if (string.IsNullOrEmpty(UserAgent))
				throw new ForbiddenException("Request lacks a User-Agent.");

			if (PaymentIds.Count == 0)
				throw new BadRequestException("No payments selected.");

			ServiceConfiguration Configuration = await ServiceConfiguration.Load();
			OperationInformation Operation = new OperationInformation(ClientIpAddress, UserAgent, 
				AuthorizationFlow.Decoupled, Configuration.PersonalID, Configuration.OrganizationID,
				OutboundPayment ? Configuration.NeuronBankBic : Payments[0].FromBank);
			OpenPaymentsPlatformClient Client = OpenPaymentsPlatformServiceProvider.CreateClient(Configuration)
				?? throw new TemporaryRedirectException("Settings.md");

			return (Configuration, Operation, Client, Payments.ToArray(), PaymentIds.ToArray(), TabId, Password, User);
		}

	}
}
