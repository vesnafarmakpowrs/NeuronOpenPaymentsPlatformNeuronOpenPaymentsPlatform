using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using Waher.Events;
using Waher.Events.Console;
using Waher.Networking.Sniffers;
using Waher.Persistence;
using Waher.Persistence.Files;
using Waher.Runtime.Inventory;
using Waher.Runtime.Inventory.Loader;
using Waher.Runtime.Settings;

namespace TAG.Networking.OpenPaymentsPlatform.Test
{
	[TestClass]
	public class ServiceProviderTests
	{
		private static OpenPaymentsPlatformClient? client;

		[AssemblyInitialize]
		public static async Task AssemblyInitialize(TestContext _)
		{
			// Create inventory of available classes.
			TypesLoader.Initialize();

			// Register console event log
			Log.Register(new ConsoleEventSink(true, true));

			// Instantiate local encrypted object database.
			FilesProvider DB = await FilesProvider.CreateAsync(Path.Combine(Directory.GetCurrentDirectory(), "Data"), "Default",
				8192, 10000, 8192, Encoding.UTF8, 10000, true, false);

			await DB.RepairIfInproperShutdown(string.Empty);

			Database.Register(DB);

			// Start embedded modules (database lifecycle)

			await Types.StartAllModules(60000);
		}

		[AssemblyCleanup]
		public static async Task AssemblyCleanup()
		{
			Log.Terminate();
			await Types.StopAllModules();
		}

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

			await client.CheckToken(ServiceApi.AspServiceProvidersInformation);
		}

		[TestMethod]
		public async Task Test_02_GetCountries()
		{
			Assert.IsNotNull(client);

			foreach (KeyValuePair<string, Country> Country in await client.GetCountriesPerCode())
			{
				Print("ISO Code", Country.Value.IsoCode);
				Print("Name", Country.Value.Name);
			}
		}

		internal static void Print(string Key, object Value)
		{
			Console.Out.Write(Key);
			Console.Out.Write(":\t");
			Console.Out.WriteLine(Value?.ToString() ?? "NULL");
		}

		internal static void Print(Links Links)
		{
			foreach (KeyValuePair<string, string> Link in Links)
				Print(Link.Key, Link.Value);
		}

		[TestMethod]
		public async Task Test_03_GetCountry()
		{
			Assert.IsNotNull(client);

			Country Country = await client.GetCountry("se");

			Print("ISO Code", Country.IsoCode);
			Print("Name", Country.Name);
		}

		[TestMethod]
		public async Task Test_04_GetCities()
		{
			Assert.IsNotNull(client);

			foreach (KeyValuePair<string, City> City in await client.GetCitiesPerName())
			{
				Print("City ID", City.Value.CityID);
				Print("Country ISO Code", City.Value.IsoCountryCode);
				Print("Name", City.Value.Name);
			}
		}

		[TestMethod]
		public async Task Test_05_GetCitiesInCountry()
		{
			Assert.IsNotNull(client);

			foreach (KeyValuePair<string, City> City in await client.GetCitiesPerName("se"))
			{
				Print("City ID", City.Value.CityID);
				Print("Country ISO Code", City.Value.IsoCountryCode);
				Print("Name", City.Value.Name);
			}
		}

		[TestMethod]
		public async Task Test_06_GetCity()
		{
			Assert.IsNotNull(client);

			City City = await client.GetCity("37efa883-c8ad-4ff7-927b-b11b02beb923");

			Print("City ID", City.CityID);
			Print("Country ISO Code", City.IsoCountryCode);
			Print("Name", City.Name);
		}

		[TestMethod]
		public async Task Test_07_GetServiceProviders()
		{
			Assert.IsNotNull(client);

			foreach (KeyValuePair<string, AspServiceProvider> ServiceProvider in await client.GetAspServiceProvidersPerName())
			{
				Print("BICFI", ServiceProvider.Value.BicFi);
				Print("Name", ServiceProvider.Value.Name);
				Print("Logo URL", ServiceProvider.Value.LogoUrl);
			}
		}

		[TestMethod]
		public async Task Test_08_GetServiceProvidersInCountry()
		{
			Assert.IsNotNull(client);

			foreach (KeyValuePair<string, AspServiceProvider> ServiceProvider in await client.GetAspServiceProvidersPerName("se"))
			{
				Print("BICFI", ServiceProvider.Value.BicFi);
				Print("Name", ServiceProvider.Value.Name);
				Print("Logo URL", ServiceProvider.Value.LogoUrl);
			}
		}

		[TestMethod]
		public async Task Test_09_GetServiceProvider()
		{
			Assert.IsNotNull(client);

			AspServiceProviderDetails ServiceProvider = await client.GetAspServiceProvider("ESSESESS");

			Print("BICFI", ServiceProvider.BicFi);
			Print("Name", ServiceProvider.Name);
			Print("Logo URL", ServiceProvider.LogoUrl);
			Print("City", ServiceProvider.City);
			Print("Country", ServiceProvider.Country);
			Print("Postal Code", ServiceProvider.PostalCode);
			Print("Street Address", ServiceProvider.StreetAddress);
			Print("Company Number", ServiceProvider.CompanyNumber);
			Print("Phone Number", ServiceProvider.PhoneNumber);
			Print("Website URL", ServiceProvider.WebsiteUrl);

			foreach (string s in ServiceProvider.GlobalPaymentProducts)
				Print("Global Payment Product", s);

			foreach (KeyValuePair<string, Uri> P in ServiceProvider.SupportedAuthorizationMethods)
				Print(P.Key, P.Value?.ToString() ?? "NULL");

			foreach (KeyValuePair<string, string> P in ServiceProvider.AffiliatedAspsps)
				Print(P.Key, P.Value);
		}

	}
}