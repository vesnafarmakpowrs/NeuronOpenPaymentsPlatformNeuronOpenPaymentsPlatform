using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Text.Json;
using Waher.Content;
using Waher.Content.Html.Elements;
using Waher.Networking.Sniffers;
using Waher.Runtime.Profiling.Events;
using Waher.Runtime.Settings;
using Waher.Script.Functions.Vectors;
using static Microsoft.CodeAnalysis.CSharp.SyntaxTokenParser;

namespace TAG.Networking.OpenPaymentsPlatform.Test
{
	[TestClass]
	public class PaymentTests
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
			//string ClientID = await RuntimeSettings.GetAsync("OpenPaymentsPlatform.ClientID", string.Empty);
			//string ClientSecret = await RuntimeSettings.GetAsync("OpenPaymentsPlatform.ClientSecret", string.Empty);
			//if (string.IsNullOrEmpty(ClientID) || string.IsNullOrEmpty(ClientSecret))
			//	Assert.Fail("Credentials not configured. Make sure the credentials are configured before running tests.");

			//client = OpenPaymentsPlatformClient.CreateSandbox(ClientID, ClientSecret, ServicePurpose.Private,
			//	new ConsoleOutSniffer(BinaryPresentationMethod.Base64, LineEnding.NewLine));
		}

		[ClassCleanup]
		public static void ClassCleanup()
		{
			client?.Dispose();
			client = null;
		}

        [TestMethod]
        public async Task Test_00_serialize()
        {
			try
			{
				var test = "[\r\n\t{\r\n\t\t\"BankAccount\": \'test\",\r\n\t\t\"ServiceProviderId\": \"test\",\r\n\t\t\"ServiceProviderType\": \"test\",\r\n\t\t\"AccountName\": \"test\",\r\n\t\t\"Description\": \"test\",\r\n\t\t\"Amount\": 5.123,\r\n\t\t\"IsSuccess\": false\r\n\t},\r\n\t{\r\n\t\t\"BankAccount\": \"SE9560000000000421038098\",\r\n\t\t\"ServiceProviderId\": \".HANDSESS\",\r\n\t\t\"ServiceProviderType\": \"TAG.Payments.OpenPaymentsPlatform.OpenPaymentsPlatformServiceProvider\",\r\n\t\t\"Description\": \"Vaulter\",\r\n\t\t\"AccountName\": \"Vaulter\",\r\n\t\t\"Amount\": 5,\r\n\t\t\"IsSuccess\": false\r\n\t}\r\n]";

				var options = new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true // Ignore case when matching properties
				};

				var SplitPaymentOptions = System.Text.Json.JsonSerializer.Deserialize<List<PaymentOption>>(test, options);
			}
			catch (System.Exception ex)
			{

			}
			

			//if(obj is Array)
			//{
			//	foreach(var item in obj as object[])
			//	{
			//		var x = new SplitPaymentOption(item);
   //             }
			//}

            //await client.CheckToken(ServiceApi.PaymentInitiation);
        }

        [TestMethod]
		public async Task Test_01_GetToken()
		{
			Assert.IsNotNull(client);

			await client.CheckToken(ServiceApi.PaymentInitiation);
		}

		[TestMethod]
		public async Task Test_02_CreatePaymentInitiation()
		{
			PaymentInitiationReference PaymentInitiationReference = await CreatePaymentInitiation();

			Assert.IsNotNull(PaymentInitiationReference);

			ServiceProviderTests.Print("TransactionStatus", PaymentInitiationReference.TransactionStatus.ToString());
			ServiceProviderTests.Print("PaymentId", PaymentInitiationReference.PaymentId);
			ServiceProviderTests.Print(PaymentInitiationReference.Links);
			ServiceProviderTests.Print("Message", PaymentInitiationReference.Message);
		}

		private static async Task<PaymentInitiationReference> CreatePaymentInitiation()
		{
			Assert.IsNotNull(client);
			OperationInformation Operation = await AccountTests.GetOperation(AuthorizationFlow.Decoupled);
			return await client.CreatePaymentInitiation(
				PaymentProduct.domestic, 10, "SEK", await AccountTests.GetAccountNr1(), "SEK",
				await AccountTests.GetAccountNr1(), "SEK", "Test", "Unit test",
				Operation);
		}

		[TestMethod]
		public async Task Test_03_DeletePaymentInitiation()
		{
			Assert.IsNotNull(client);

			OperationInformation Operation = await AccountTests.GetOperation(AuthorizationFlow.Decoupled);
			PaymentInitiationReference PaymentInitiationReference = await CreatePaymentInitiation();

			await client.DeletePaymentInitiation(PaymentProduct.domestic,
				PaymentInitiationReference.PaymentId, Operation);
		}

		[TestMethod]
		public async Task Test_04_GetPaymentInitiationStatus()
		{
			Assert.IsNotNull(client);
			OperationInformation Operation = await AccountTests.GetOperation(AuthorizationFlow.Decoupled);
			PaymentInitiationReference PaymentInitiationReference = await client.CreatePaymentInitiation(
				PaymentProduct.domestic, 10, "SEK", await AccountTests.GetAccountNr1(), "SEK",
				await AccountTests.GetAccountNr1(), "SEK", "Test", "Unit test",
				Operation);

			PaymentTransactionStatus Status = await client.GetPaymentInitiationStatus(PaymentProduct.domestic,
				PaymentInitiationReference.PaymentId, Operation);

			Assert.AreEqual(PaymentInitiationReference.TransactionStatus, Status.Status);
		}

		[TestMethod]
		public async Task Test_05_CreatePaymentAuthorization()
		{
			Assert.IsNotNull(client);
			OperationInformation Operation = await AccountTests.GetOperation(AuthorizationFlow.Decoupled);
			PaymentInitiationReference PaymentInitiationReference = await client.CreatePaymentInitiation(
				PaymentProduct.domestic, 10, "SEK", await AccountTests.GetAccountNr1(), "SEK",
				await AccountTests.GetAccountNr1(), "SEK", "Test", "Unit test",
				Operation);

			AuthorizationInformation Status = await client.StartPaymentInitiationAuthorization(
				PaymentProduct.domestic, PaymentInitiationReference.PaymentId, Operation);

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
		public async Task Test_06_GetAuthorizationIDs()
		{
			Assert.IsNotNull(client);

			OperationInformation Operation = await AccountTests.GetOperation(AuthorizationFlow.Decoupled);
			PaymentInitiationReference PaymentInitiationReference = await client.CreatePaymentInitiation(
				PaymentProduct.domestic, 10, "SEK", await AccountTests.GetAccountNr1(), "SEK",
				await AccountTests.GetAccountNr1(), "SEK", "Test", "Unit test",
				Operation);

			AuthorizationInformation Status = await client.StartPaymentInitiationAuthorization(
				PaymentProduct.domestic, PaymentInitiationReference.PaymentId, Operation);

			string[] IDs = await client.GetPaymentAuthorizationIDs(PaymentProduct.domestic,
				PaymentInitiationReference.PaymentId, Operation);
			bool Found = false;

			foreach (string ID in IDs)
			{
				Console.Out.WriteLine(ID);
				Found |= ID == Status.AuthorizationID;
			}

			Assert.IsTrue(Found);
		}

		[TestMethod]
		public async Task Test_07_GetPaymentInititiationAuthorizationStatus()
		{
			Assert.IsNotNull(client);

			OperationInformation Operation = await AccountTests.GetOperation(AuthorizationFlow.Decoupled);
			PaymentInitiationReference PaymentInitiationReference = await client.CreatePaymentInitiation(
				PaymentProduct.domestic, 10, "SEK", await AccountTests.GetAccountNr1(), "SEK",
				await AccountTests.GetAccountNr1(), "SEK", "Test", "Unit test",
				Operation);

			AuthorizationInformation Status = await client.StartPaymentInitiationAuthorization(
				PaymentProduct.domestic, PaymentInitiationReference.PaymentId, Operation);

			AuthorizationStatus P = await client.GetPaymentInitiationAuthorizationStatus(PaymentProduct.domestic,
				PaymentInitiationReference.PaymentId, Status.AuthorizationID, Operation);
			AuthorizationStatusValue Status2 = P.Status;

			Assert.AreEqual(Status.Status, Status2);
		}

		[TestMethod]
		public async Task Test_08_StartPaymentAuthorization()
		{
			Assert.IsNotNull(client);

			OperationInformation Operation = await AccountTests.GetOperation(AuthorizationFlow.Decoupled);
			PaymentInitiationReference PaymentInitiationReference = await client.CreatePaymentInitiation(
				PaymentProduct.domestic, 10, "SEK", await AccountTests.GetAccountNr1(), "SEK",
				await AccountTests.GetAccountNr1(), "SEK", "Test", "Unit test",
				Operation);

			AuthorizationInformation Status = await client.StartPaymentInitiationAuthorization(
				PaymentProduct.domestic, PaymentInitiationReference.PaymentId, Operation);

			AuthenticationMethod Method = Status.GetAuthenticationMethod("mbid_same_device")
				?? Status.GetAuthenticationMethod("mbid");
			Assert.IsNotNull(Method);

			PaymentServiceUserDataResponse? PsuDataResponse;

			PsuDataResponse = await client.PutPaymentInitiationUserData(
				PaymentProduct.domestic, PaymentInitiationReference.PaymentId,
				Status.AuthorizationID, Method.MethodId, Operation);
			Assert.IsNotNull(PsuDataResponse);

			Assert.IsFalse(string.IsNullOrEmpty(PsuDataResponse.ChallengeData?.BankIdURL));

			ServiceProviderTests.Print("Message", PsuDataResponse.Message);
			ServiceProviderTests.Print("Status", PsuDataResponse.Status);
			ServiceProviderTests.Print(PsuDataResponse.Links);
			ServiceProviderTests.Print("ChosenMethod.Name", PsuDataResponse.ChosenMethod.Name);
			ServiceProviderTests.Print("ChosenMethod.MethodId", PsuDataResponse.ChosenMethod.MethodId);
			ServiceProviderTests.Print("ChosenMethod.Type", PsuDataResponse.ChosenMethod.Type);
			ServiceProviderTests.Print("BankIdURL", PsuDataResponse.ChallengeData?.BankIdURL ?? "NULL");
		}

		[TestMethod]
		public async Task Test_09_WaitUntilPaymentInitializationFinalized()
		{
			Assert.IsNotNull(client);

			OperationInformation Operation = await AccountTests.GetOperation(AuthorizationFlow.Decoupled);
			PaymentInitiationReference PaymentInitiationReference = await client.CreatePaymentInitiation(
				PaymentProduct.domestic, 10, "SEK", await AccountTests.GetAccountNr1(), "SEK",
				await AccountTests.GetAccountNr1(), "SEK", "Test", "Unit test",
				Operation);

			AuthorizationInformation Status = await client.StartPaymentInitiationAuthorization(
				PaymentProduct.domestic, PaymentInitiationReference.PaymentId, Operation);

			AuthenticationMethod Method = Status.GetAuthenticationMethod("mbid_same_device")
				?? Status.GetAuthenticationMethod("mbid");
			Assert.IsNotNull(Method);

			PaymentServiceUserDataResponse? PsuDataResponse;

			PsuDataResponse = await client.PutPaymentInitiationUserData(
				PaymentProduct.domestic, PaymentInitiationReference.PaymentId,
				Status.AuthorizationID, Method.MethodId, Operation);
			Assert.IsNotNull(PsuDataResponse);

			AuthorizationStatusValue AuthorizationStatus = PsuDataResponse.Status;
			DateTime Start = DateTime.Now;

			while (AuthorizationStatus != AuthorizationStatusValue.finalised &&
				AuthorizationStatus != AuthorizationStatusValue.failed &&
				DateTime.Now.Subtract(Start).TotalMinutes < 1)
			{
				await Task.Delay(2000);
				AuthorizationStatus P = await client.GetPaymentInitiationAuthorizationStatus(
					PaymentProduct.domestic, PaymentInitiationReference.PaymentId,
					Status.AuthorizationID, Operation);
				AuthorizationStatus = P.Status;
			}

			Assert.AreEqual(AuthorizationStatusValue.finalised, AuthorizationStatus);

			PaymentTransactionStatus Status2 = await client.GetPaymentInitiationStatus(PaymentProduct.domestic,
				PaymentInitiationReference.PaymentId, Operation);

			switch (Status2.Status)
			{
				case PaymentStatus.RJCT:
					Assert.Fail("Payment was rejected.");
					break;

				case PaymentStatus.CANC:
					Assert.Fail("Payment was cancelled.");
					break;
			}
		}

		[TestMethod]
		public async Task Test_10_Currency()
		{
			Assert.IsNotNull(client);

			OperationInformation Operation = await AccountTests.GetOperation(AuthorizationFlow.Decoupled);
			PaymentInitiationReference PaymentInitiationReference = await client.CreatePaymentInitiation(
				PaymentProduct.domestic, 10, "EUR", await AccountTests.GetAccountNr1(), "EUR",
				await AccountTests.GetAccountNr1(), "EUR", "Test", "Unit test",
				Operation);

			AuthorizationInformation Status = await client.StartPaymentInitiationAuthorization(
				PaymentProduct.domestic, PaymentInitiationReference.PaymentId, Operation);

			AuthenticationMethod Method = Status.GetAuthenticationMethod("mbid_same_device")
				?? Status.GetAuthenticationMethod("mbid");
			Assert.IsNotNull(Method);

			PaymentServiceUserDataResponse? PsuDataResponse;

			PsuDataResponse = await client.PutPaymentInitiationUserData(
				PaymentProduct.domestic, PaymentInitiationReference.PaymentId,
				Status.AuthorizationID, Method.MethodId, Operation);
			Assert.IsNotNull(PsuDataResponse);

			AuthorizationStatusValue AuthorizationStatus = PsuDataResponse.Status;
			DateTime Start = DateTime.Now;

			while (AuthorizationStatus != AuthorizationStatusValue.finalised &&
				AuthorizationStatus != AuthorizationStatusValue.failed &&
				DateTime.Now.Subtract(Start).TotalMinutes < 1)
			{
				await Task.Delay(2000);
				AuthorizationStatus P = await client.GetPaymentInitiationAuthorizationStatus(
					PaymentProduct.domestic, PaymentInitiationReference.PaymentId,
					Status.AuthorizationID, Operation);
				AuthorizationStatus = P.Status;
			}

			Assert.AreEqual(AuthorizationStatusValue.finalised, AuthorizationStatus);

			PaymentTransactionStatus Status2 = await client.GetPaymentInitiationStatus(PaymentProduct.domestic,
				PaymentInitiationReference.PaymentId, Operation);

			switch (Status2.Status)
			{
				case PaymentStatus.RJCT:
					Assert.Fail("Payment was rejected.");
					break;

				case PaymentStatus.CANC:
					Assert.Fail("Payment was cancelled.");
					break;
			}
		}

		[TestMethod]
		public async Task Test_11_CreatePaymentBasket()
		{
			PaymentBasketReference Basket = await CreatePaymentBasket();

			ServiceProviderTests.Print("TransactionStatus", Basket.TransactionStatus.ToString());
			ServiceProviderTests.Print("BasketId", Basket.BasketId);
			ServiceProviderTests.Print(Basket.Links);
			ServiceProviderTests.Print("Message", Basket.Message);
		}

		private static async Task<PaymentBasketReference> CreatePaymentBasket()
		{
			Assert.IsNotNull(client);
			OperationInformation Operation = await AccountTests.GetOperation(AuthorizationFlow.Decoupled);
			PaymentInitiationReference PaymentInitiationReference1 = await client.CreatePaymentInitiation(
				PaymentProduct.domestic, 10, "SEK", await AccountTests.GetAccountNr1(), "SEK",
				await AccountTests.GetAccountNr1(), "SEK", "Test", "Unit test",
				Operation);
			PaymentInitiationReference PaymentInitiationReference2 = await client.CreatePaymentInitiation(
				PaymentProduct.domestic, 11, "SEK", await AccountTests.GetAccountNr1(), "SEK",
				await AccountTests.GetAccountNr1(), "SEK", "Test", "Unit test",
				Operation);
			PaymentInitiationReference PaymentInitiationReference3 = await client.CreatePaymentInitiation(
				PaymentProduct.domestic, 12, "SEK", await AccountTests.GetAccountNr1(), "SEK",
				await AccountTests.GetAccountNr1(), "SEK", "Test", "Unit test",
				Operation);

			PaymentBasketReference Basket = await client.CreatePaymentBasket(new string[]
			{
				PaymentInitiationReference1.PaymentId,
				PaymentInitiationReference2.PaymentId,
				PaymentInitiationReference3.PaymentId
			}, Operation);

			return Basket;
		}

		[TestMethod]
		public async Task Test_12_DeletePaymentBasket()
		{
			Assert.IsNotNull(client);

			OperationInformation Operation = await AccountTests.GetOperation(AuthorizationFlow.Decoupled);
			PaymentBasketReference Basket = await CreatePaymentBasket();

			await client.DeletePaymentBasket(Basket.BasketId, Operation);
		}

		[TestMethod]
		public async Task Test_13_PaymentBasketStatus()
		{
			Assert.IsNotNull(client);

			OperationInformation Operation = await AccountTests.GetOperation(AuthorizationFlow.Decoupled);
			PaymentBasketReference Basket = await CreatePaymentBasket();

			BasketTransactionStatus Status = await client.GetPaymentBasketStatus(Basket.BasketId, Operation);

			Assert.AreEqual(Basket.TransactionStatus, Status.Status);
		}

		[TestMethod]
		public async Task Test_14_SelectPaymentBasketAuthorization()
		{
			Assert.IsNotNull(client);
			OperationInformation Operation = await AccountTests.GetOperation(AuthorizationFlow.Decoupled);
			PaymentBasketReference Basket = await CreatePaymentBasket();

			AuthorizationInformation Status = await client.StartPaymentBasketAuthorization(
				Basket.BasketId, Operation);

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
		public async Task Test_15_StartPaymentBasketAuthorization()
		{
			Assert.IsNotNull(client);
			OperationInformation Operation = await AccountTests.GetOperation(AuthorizationFlow.Decoupled);
			PaymentBasketReference Basket = await CreatePaymentBasket();

			AuthorizationInformation Status = await client.StartPaymentBasketAuthorization(
				Basket.BasketId, Operation);

			AuthenticationMethod Method = Status.GetAuthenticationMethod("mbid_same_device")
				?? Status.GetAuthenticationMethod("mbid");
			Assert.IsNotNull(Method);

			PaymentServiceUserDataResponse? PsuDataResponse;

			PsuDataResponse = await client.PutPaymentBasketUserData(Basket.BasketId, 
				Status.AuthorizationID, Method.MethodId, Operation);
			
			Assert.IsNotNull(PsuDataResponse);

			Assert.IsFalse(string.IsNullOrEmpty(PsuDataResponse.ChallengeData?.BankIdURL));

			ServiceProviderTests.Print("Message", PsuDataResponse.Message);
			ServiceProviderTests.Print("Status", PsuDataResponse.Status);
			ServiceProviderTests.Print(PsuDataResponse.Links);
			if (PsuDataResponse.ChosenMethod is not null)
			{
				ServiceProviderTests.Print("ChosenMethod.Name", PsuDataResponse.ChosenMethod.Name);
				ServiceProviderTests.Print("ChosenMethod.MethodId", PsuDataResponse.ChosenMethod.MethodId);
				ServiceProviderTests.Print("ChosenMethod.Type", PsuDataResponse.ChosenMethod.Type);
			}
			ServiceProviderTests.Print("BankIdURL", PsuDataResponse.ChallengeData?.BankIdURL ?? "NULL");
		}

		[TestMethod]
		public async Task Test_16_WaitUntilPaymentBasketFinalized()
		{
			Assert.IsNotNull(client);
			OperationInformation Operation = await AccountTests.GetOperation(AuthorizationFlow.Decoupled);
			PaymentBasketReference Basket = await CreatePaymentBasket();

			AuthorizationInformation Status = await client.StartPaymentBasketAuthorization(
				Basket.BasketId, Operation);

			AuthenticationMethod Method = Status.GetAuthenticationMethod("mbid_same_device")
				?? Status.GetAuthenticationMethod("mbid");
			Assert.IsNotNull(Method);

			PaymentServiceUserDataResponse? PsuDataResponse;

			PsuDataResponse = await client.PutPaymentBasketUserData(Basket.BasketId,
				Status.AuthorizationID, Method.MethodId, Operation);
			Assert.IsNotNull(PsuDataResponse);

			AuthorizationStatusValue AuthorizationStatus = PsuDataResponse.Status;
			DateTime Start = DateTime.Now;

			while (AuthorizationStatus != AuthorizationStatusValue.finalised &&
				AuthorizationStatus != AuthorizationStatusValue.failed &&
				DateTime.Now.Subtract(Start).TotalMinutes < 1)
			{
				await Task.Delay(2000);
				AuthorizationStatus P = await client.GetPaymentBasketAuthorizationStatus(
					Basket.BasketId, Status.AuthorizationID, Operation);
				AuthorizationStatus = P.Status;
			}

			Assert.AreEqual(AuthorizationStatusValue.finalised, AuthorizationStatus);

			BasketTransactionStatus BasketStatus = await client.GetPaymentBasketStatus(
				Basket.BasketId, Operation);

			switch (BasketStatus.Status)
			{
				case PaymentBasketStatus.RJCT:
					Assert.Fail("Payment basket was rejected.");
					break;
			}
		}

	}
}