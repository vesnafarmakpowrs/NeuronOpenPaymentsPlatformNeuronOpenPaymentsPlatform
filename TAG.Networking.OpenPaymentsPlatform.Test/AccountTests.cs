using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using Waher.Content;
using Waher.Content.Html;
using Waher.Networking.Sniffers;
using Waher.Runtime.Settings;
using Waher.Script.Functions.Vectors;

namespace TAG.Networking.OpenPaymentsPlatform.Test
{
	[TestClass]
	public class AccountTests
	{
		private static OpenPaymentsPlatformClient? client;

		[ClassInitialize]
		public static async Task ClassInitialize(TestContext _)
		{
			// Configuring API Key
			// NOTE: Don't check in API credentials into the repository. Uncomment the code below, and write your
			//       API Key into the runtime setting. Once written, you can empty the string in the code and re-comment
			//       it, so it's not overwritten the next time you run the tests.
			// await RuntimeSettings.SetAsync("OpenPaymentsPlatform.ClientID", string.Empty);
			// await RuntimeSettings.SetAsync("OpenPaymentsPlatform.ClientSecret", string.Empty);

			// Reading API Key
			string ClientID = await RuntimeSettings.GetAsync("OpenPaymentsPlatform.ClientID", string.Empty);
			string ClientSecret = await RuntimeSettings.GetAsync("OpenPaymentsPlatform.ClientSecret", string.Empty);
			if (string.IsNullOrEmpty(ClientID) || string.IsNullOrEmpty(ClientSecret))
				Assert.Fail("Credentials not configured. Make sure the credentials are configured before running tests.");

			client = OpenPaymentsPlatformClient.CreateSandbox(ClientID, ClientSecret, ServicePurpose.Private,
				new ConsoleOutSniffer(BinaryPresentationMethod.Base64, LineEnding.NewLine));
		}

		[ClassCleanup]
		public static void ClassCleanup()
		{
			client?.Dispose();
			client = null;
		}

		[TestMethod]
		public async Task Test_01_GetToken()
		{
			Assert.IsNotNull(client);

			await client.CheckToken(ServiceApi.AccountInformation);
		}

		[TestMethod]
		public async Task Test_02_CreateConsent_Redirect()
		{
			Assert.IsNotNull(client);
			OperationInformation Operation = await GetOperation(AuthorizationFlow.Redirect);
			await CreateConsent(client, Operation);
		}

		[TestMethod]
		public async Task Test_03_CreateConsent_Decoupled()
		{
			Assert.IsNotNull(client);
			OperationInformation Operation = await GetOperation(AuthorizationFlow.Decoupled);
			await CreateConsent(client, Operation);
		}

		internal static async Task<OperationInformation> GetOperation(AuthorizationFlow Flow)
		{
			return new OperationInformation(
				await GetIPAddress(),
				typeof(AccountTests).Assembly.FullName ?? "Test Anchor Group Unit Test",
				Flow,
				await GetPersonalNumber1(),
				null,
				await GetServiceProvider1());
		}

		internal static async Task<ConsentStatus> CreateConsent(OpenPaymentsPlatformClient Client,
			OperationInformation Operation)
		{
			ConsentStatus Consent = await Client.CreateConsent(await GetAccountNr1(),
				true, true, false, DateTime.Today.AddDays(3), 1, false, Operation);

			ServiceProviderTests.Print("Consent ID", Consent.ConsentID);
			ServiceProviderTests.Print("Status", Consent.Status);
			ServiceProviderTests.Print(Consent.Links);

			foreach (AuthenticationMethod Method in Consent.AuthenticationMethods)
			{
				ServiceProviderTests.Print("Type", Method.Type);
				ServiceProviderTests.Print("Method ID", Method.MethodId);
				ServiceProviderTests.Print("Name", Method.Name);
			}

			Assert.AreEqual(ConsentStatusValue.received, Consent.Status);

			return Consent;
		}

		internal static async Task<string> GetServiceProvider1()
		{
			//await RuntimeSettings.SetAsync("OpenPaymentsPlatform.BifFi1", string.Empty);

			return await RuntimeSettings.GetAsync("OpenPaymentsPlatform.BifFi1", string.Empty);
		}

		internal static async Task<string> GetAccountNr1()
		{
			//await RuntimeSettings.SetAsync("OpenPaymentsPlatform.AccountNr1", string.Empty);

			return await RuntimeSettings.GetAsync("OpenPaymentsPlatform.AccountNr1", string.Empty);
		}

		internal static async Task<string> GetPersonalNumber1()
		{
			//await RuntimeSettings.SetAsync("OpenPaymentsPlatform.PersonalNumber1", string.Empty);

			return await RuntimeSettings.GetAsync("OpenPaymentsPlatform.PersonalNumber1", string.Empty);
		}

		internal static async Task<IPAddress> GetIPAddress()
		{
			object Response = await InternetContent.PostAsync(new Uri("https://id.tagroot.io/ID/CountryCode.ws"),
				string.Empty, new KeyValuePair<string, string>("Accept", "application/json"));

			if (Response is not Dictionary<string, object> ResponseObject ||
				!ResponseObject.TryGetValue("RemoteEndPoint", out object? Obj) ||
				Obj is not string s ||
				!IPEndPoint.TryParse(s, out IPEndPoint? IP))
			{
				throw new Exception("Unexpected response.");
			}

			return IP.Address;
		}

		[TestMethod]
		public async Task Test_04_GetConsent_Redirect()
		{
			OperationInformation Operation = await GetOperation(AuthorizationFlow.Redirect);
			await GetConsent(Operation);
		}

		[TestMethod]
		public async Task Test_05_GetConsent_Decoupled()
		{
			OperationInformation Operation = await GetOperation(AuthorizationFlow.Decoupled);
			await GetConsent(Operation);
		}

		private static async Task GetConsent(OperationInformation Operation)
		{
			Assert.IsNotNull(client);

			ConsentStatus Consent = await CreateConsent(client, Operation);
			ConsentRequest Request = await client.GetConsent(Consent.ConsentID, Operation);

			Assert.AreEqual(Consent.ConsentID, Request.ConsentID);
			Assert.AreEqual(Consent.Status, Request.Status);

			ServiceProviderTests.Print("ConsentID", Request.ConsentID);
			ServiceProviderTests.Print("Status", Request.Status);
			ServiceProviderTests.Print("Recurring", Request.Recurring);
			ServiceProviderTests.Print("ValidUntil", Request.ValidUntil);
			ServiceProviderTests.Print("FrequencyPerDay", Request.FrequencyPerDay);
			ServiceProviderTests.Print("LastActionDate", Request.LastActionDate);

			foreach (AccountReference Ref in Request.AccountAccess)
			{
				ServiceProviderTests.Print("AccountRef", Ref.Iban);
				ServiceProviderTests.Print("Currency", Ref.Currency);

			}

			foreach (AccountReference Ref in Request.BalanceAccess)
			{
				ServiceProviderTests.Print("BalanceRef", Ref.Iban);
				ServiceProviderTests.Print("Currency", Ref.Currency);

			}

			foreach (AccountReference Ref in Request.TransactionsAccess)
			{
				ServiceProviderTests.Print("TransactionRef", Ref.Iban);
				ServiceProviderTests.Print("Currency", Ref.Currency);

			}
		}

		[TestMethod]
		public async Task Test_06_StartConsentAuthorization_Redirect()
		{
			OperationInformation Operation = await GetOperation(AuthorizationFlow.Redirect);
			await StartConsentAuthorization(Operation);
		}

		[TestMethod]
		public async Task Test_07_StartConsentAuthorization_Decoupled()
		{
			OperationInformation Operation = await GetOperation(AuthorizationFlow.Decoupled);
			await StartConsentAuthorization(Operation);
		}

		private static async Task StartConsentAuthorization(OperationInformation Operation)
		{
			Assert.IsNotNull(client);

			ConsentStatus Consent = await CreateConsent(client, Operation);
			AuthorizationInformation Status = await client.StartConsentAuthorization(Consent.ConsentID, Operation);

			ServiceProviderTests.Print("ConsentID", Status.AuthorizationID);
			ServiceProviderTests.Print("Status", Status.Status);
			ServiceProviderTests.Print(Status.Links);

			foreach (AuthenticationMethod Method in Status.AuthenticationMethods)
			{
				ServiceProviderTests.Print("Type", Method.Type);
				ServiceProviderTests.Print("Method ID", Method.MethodId);
				ServiceProviderTests.Print("Name", Method.Name);
			}

			Assert.AreEqual(AuthorizationStatusValue.received, Status.Status);
		}

		[TestMethod]
		public async Task Test_08_GetAuthorizationIDs_Redirect()
		{
			OperationInformation Operation = await GetOperation(AuthorizationFlow.Redirect);
			await GetAuthorizationIDs(Operation);
		}

		[TestMethod]
		public async Task Test_09_GetAuthorizationIDs_Decoupled()
		{
			OperationInformation Operation = await GetOperation(AuthorizationFlow.Decoupled);
			await GetAuthorizationIDs(Operation);
		}

		private static async Task GetAuthorizationIDs(OperationInformation Operation)
		{
			Assert.IsNotNull(client);

			ConsentStatus Consent = await CreateConsent(client, Operation);
			AuthorizationInformation Status = await client.StartConsentAuthorization(
				Consent.ConsentID, Operation);

			string[] IDs = await client.GetConsentAuthorizationIDs(Consent.ConsentID, Operation);
			bool Found = false;

			foreach (string ID in IDs)
			{
				Console.Out.WriteLine(ID);
				Found |= ID == Status.AuthorizationID;
			}

			Assert.IsTrue(Found);
		}

		[TestMethod]
		public async Task Test_10_GetAuthorizationStatus_Redirect()
		{
			OperationInformation Operation = await GetOperation(AuthorizationFlow.Redirect);
			await GetAuthorizationStatus(Operation);
		}

		[TestMethod]
		public async Task Test_11_GetAuthorizationStatus_Decoupled()
		{
			OperationInformation Operation = await GetOperation(AuthorizationFlow.Decoupled);
			await GetAuthorizationStatus(Operation);
		}

		private static async Task GetAuthorizationStatus(OperationInformation Operation)
		{
			Assert.IsNotNull(client);

			ConsentStatus Consent = await CreateConsent(client, Operation);
			AuthorizationInformation Status = await client.StartConsentAuthorization(
				Consent.ConsentID, Operation);

			AuthorizationStatus Status2 = await client.GetConsentAuthorizationStatus(
				Consent.ConsentID, Status.AuthorizationID, Operation);

			Assert.AreEqual(Status.Status, Status2.Status);
		}

		[TestMethod]
		public async Task Test_12_DeleteConsent_Redirect()
		{
			OperationInformation Operation = await GetOperation(AuthorizationFlow.Redirect);
			await DeleteConsent(Operation);
		}

		[TestMethod]
		public async Task Test_13_DeleteConsent_Decoupled()
		{
			OperationInformation Operation = await GetOperation(AuthorizationFlow.Decoupled);
			await DeleteConsent(Operation);
		}

		private static async Task DeleteConsent(OperationInformation Operation)
		{
			Assert.IsNotNull(client);

			ConsentStatus Consent = await CreateConsent(client, Operation);
			await client.DeleteConsent(Consent.ConsentID, Operation);
		}

		[TestMethod]
		public async Task Test_14_ChooseAuthenticationMechanism_Redirect()
		{
			OperationInformation Operation = await GetOperation(AuthorizationFlow.Redirect);
			await StartAuthentication(Operation);
		}

		[TestMethod]
		public async Task Test_15_ChooseAuthenticationMechanism_Decoupled()
		{
			OperationInformation Operation = await GetOperation(AuthorizationFlow.Decoupled);
			await StartAuthentication(Operation);
		}

		internal static async Task<PaymentServiceUserDataResponse> StartAuthentication(
			OperationInformation Operation)
		{
			Assert.IsNotNull(client);

			ConsentStatus Consent = await CreateConsent(client, Operation);

			PaymentServiceUserDataResponse? PsuDataResponse;
			AuthorizationInformation Status = await client.StartConsentAuthorization(Consent.ConsentID, Operation);
			
			Assert.AreEqual(AuthorizationStatusValue.received, Status.Status);
			
			AuthenticationMethod Method = Status.GetAuthenticationMethod("mbid_same_device")
				?? Status.GetAuthenticationMethod("mbid");
			Assert.IsNotNull(Method);

			PsuDataResponse = await client.PutConsentUserData(Consent.ConsentID, 
				Status.AuthorizationID, Method.MethodId, Operation);

			Assert.IsNotNull(PsuDataResponse);

			if (PsuDataResponse.Status != AuthorizationStatusValue.finalised)
			{
				if (Operation.Flow == AuthorizationFlow.Redirect)
					Assert.IsTrue(string.IsNullOrEmpty(PsuDataResponse.ChallengeData?.BankIdURL));
				else
					Assert.IsFalse(string.IsNullOrEmpty(PsuDataResponse.ChallengeData?.BankIdURL));
			}

			ServiceProviderTests.Print("Message", PsuDataResponse.Message);
			ServiceProviderTests.Print("Status", PsuDataResponse.Status);
			ServiceProviderTests.Print(PsuDataResponse.Links);
			ServiceProviderTests.Print("ChosenMethod.Name", PsuDataResponse.ChosenMethod.Name);
			ServiceProviderTests.Print("ChosenMethod.MethodId", PsuDataResponse.ChosenMethod.MethodId);
			ServiceProviderTests.Print("ChosenMethod.Type", PsuDataResponse.ChosenMethod.Type);
			ServiceProviderTests.Print("BankIdURL", PsuDataResponse.ChallengeData?.BankIdURL ?? "NULL");

			switch (Operation.Flow)
			{
				case AuthorizationFlow.Redirect:
					Assert.AreEqual(AuthorizationStatusValue.started, PsuDataResponse.Status);
					break;

				case AuthorizationFlow.Decoupled:
					Assert.AreEqual(AuthorizationStatusValue.finalised, PsuDataResponse.Status);
					break;
			}

			return PsuDataResponse;
		}

		[TestMethod]
		public async Task Test_16_Authentication_Redirected()
		{
			Assert.IsNotNull(client);

			OperationInformation Operation = await GetOperation(AuthorizationFlow.Redirect);
			PaymentServiceUserDataResponse Response = await StartAuthentication(Operation);

			Assert.IsFalse(string.IsNullOrEmpty(Response.Links.ScaOAuth));

			Console.Out.WriteLine();
			Console.Out.WriteLine(Response.Message);
			Console.Out.WriteLine();
			Console.Out.WriteLine("Getting authentication page:");

			object Obj = await client.GetWebPage(Response.Links.ScaOAuth,
				"https://localhost:8080/", "Unit Test Request");

			Assert.IsTrue(Obj is HtmlDocument);
		}

		[TestMethod]
		public async Task Test_17_Authentication_Decoupled()
		{
			Assert.IsNotNull(client);

			OperationInformation Operation = await GetOperation(AuthorizationFlow.Decoupled);
			PaymentServiceUserDataResponse Response = await StartAuthentication(Operation);

			Assert.IsTrue(string.IsNullOrEmpty(Response.Links.ScaOAuth));

			Console.Out.WriteLine();
			Console.Out.WriteLine(Response.Message);
			Console.Out.WriteLine();
		}

		[TestMethod]
		public async Task Test_18_WaitUntilFinalized_Decoupled()
		{
			Assert.IsNotNull(client);

			OperationInformation Operation = await GetOperation(AuthorizationFlow.Decoupled);
			ConsentStatus Consent = await CreateConsent(client, Operation);

			AuthorizationInformation Status = await client.StartConsentAuthorization(
				Consent.ConsentID, Operation);

			Assert.AreEqual(AuthorizationStatusValue.received, Status.Status);
			
			AuthenticationMethod Method = Status.GetAuthenticationMethod("mbid_same_device")
				?? Status.GetAuthenticationMethod("mbid");
			Assert.IsNotNull(Method);

			await client.PutConsentUserData(Consent.ConsentID, Status.AuthorizationID, 
				Method.MethodId, Operation);

			AuthorizationStatus Status2;
			DateTime Start = DateTime.Now;

			do
			{
				await Task.Delay(2000);
				Status2 = await client.GetConsentAuthorizationStatus(Consent.ConsentID, Status.AuthorizationID, Operation);
			}
			while ((Status2.Status != AuthorizationStatusValue.finalised &&
				Status2.Status != AuthorizationStatusValue.failed) &&
				DateTime.Now.Subtract(Start).TotalMinutes < 1);

			Assert.AreEqual(AuthorizationStatusValue.finalised, Status2.Status);

			ConsentStatusValue ConsentStatusValue = await client.GetConsentStatus(Consent.ConsentID, Operation);

			switch (ConsentStatusValue)
			{
				case ConsentStatusValue.rejected:
					Assert.Fail("Consent was rejected.");
					break;

				case ConsentStatusValue.revokedByPsu:
					Assert.Fail("Consent was revoked.");
					break;

				case ConsentStatusValue.expired:
					Assert.Fail("Consent has expired.");
					break;

				case ConsentStatusValue.terminatedByTpp:
					Assert.Fail("Consent was terminated.");
					break;

				case ConsentStatusValue.valid:
					break;

				default:
					Assert.Fail("Consent was not valid.");
					break;
			}
		}
	}
}