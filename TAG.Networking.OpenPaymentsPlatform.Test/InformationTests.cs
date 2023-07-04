using Microsoft.VisualStudio.TestTools.UnitTesting;
using Waher.Networking.Sniffers;
using Waher.Runtime.Settings;

namespace TAG.Networking.OpenPaymentsPlatform.Test
{
	[TestClass]
	public class InformationTests
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
		public async Task Test_01_GetAccounts()
		{
			await GetAccounts();
		}

		[TestMethod]
		public async Task Test_02_GetAccountsWithBalance()
		{
			await GetAccounts(true);
		}

		private static async Task<AccountInformation[]> GetAccounts()
		{
			return await GetAccounts(false);
		}

		private static async Task<AccountInformation[]> GetAccounts(bool WithBalance)
		{
			Assert.IsNotNull(client);

			OperationInformation Operation = await AccountTests.GetOperation(AuthorizationFlow.Decoupled);
			ConsentStatus Consent = await AccountTests.CreateConsent(client, Operation);

			PaymentServiceUserDataResponse? PsuDataResponse;
			AuthorizationInformation Status = await client.StartConsentAuthorization(Consent.ConsentID, Operation);

			AuthenticationMethod Method = Status.GetAuthenticationMethod("mbid_same_device")
				?? Status.GetAuthenticationMethod("mbid");

			Assert.IsNotNull(Method);

			PsuDataResponse = await client.PutConsentUserData(Consent.ConsentID,
				Status.AuthorizationID, Method.MethodId, Operation);

			Assert.IsNotNull(PsuDataResponse);

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

			AccountInformation[] Accounts = await client.GetAccounts(Consent.ConsentID, Operation, WithBalance);

			foreach (AccountInformation Account in Accounts)
			{
				ServiceProviderTests.Print("ResourceID", Account.ResourceID);
				ServiceProviderTests.Print("IBAN", Account.Iban);
				ServiceProviderTests.Print("BBAN", Account.Bban);
				ServiceProviderTests.Print("Currency", Account.Currency);
				ServiceProviderTests.Print("BIC", Account.Bic);
                ServiceProviderTests.Print("Balance", Account.Balance);
                if (!(Account.Balances is null))
				{
					foreach (Balance balance in Account.Balances)
					{
						ServiceProviderTests.Print("BalanceAmount", balance.BalanceAmount.Amount.ToString() + balance.BalanceAmount.Currency);
						ServiceProviderTests.Print("BalanceType", balance.BalanceType);
						ServiceProviderTests.Print("CreditLimitIncluded", balance.CreditLimitIncluded);
					}
				}
				ServiceProviderTests.Print("Currency", Account.Currency);
				ServiceProviderTests.Print("BIC", Account.Bic);
				ServiceProviderTests.Print("Name", Account.Name);

				ServiceProviderTests.Print("Product", Account.Product);
				ServiceProviderTests.Print("CashAccountType", Account.CashAccountType);

				ServiceProviderTests.Print("Usage", Account.Usage);
				ServiceProviderTests.Print("OwnerName", Account.OwnerName);
			}

			return Accounts;
		}

	}
}