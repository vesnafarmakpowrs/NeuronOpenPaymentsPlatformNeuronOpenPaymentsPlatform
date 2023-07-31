using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Waher.Content;
using Waher.Content.Getters;
using Waher.Content.Xml;
using Waher.Events;
using Waher.Networking.Sniffers;

namespace TAG.Networking.OpenPaymentsPlatform
{
	/// <summary>
	/// Service APIs available from Open Payments Platform
	/// </summary>
	public enum ServiceApi
	{
		AspServiceProvidersInformation,
		AccountInformation,
		PaymentInitiation
	}

	/// <summary>
	/// Purpose in which to use the requested service.
	/// </summary>
	public enum ServicePurpose
	{
		/// <summary>
		/// For private purposes
		/// </summary>
		Private,

		/// <summary>
		/// For corporate purposes
		/// </summary>
		Corporate
	}

	/// <summary>
	/// Implements the Open Payments Platform API
	/// 
	/// Reference:
	/// https://docs.openpayments.io/en/openpayments-NextGenPSD2-1.3.3.html
	/// </summary>
	public class OpenPaymentsPlatformClient : Sniffable, IDisposable
	{
		private readonly Dictionary<string, Token> tokens = new Dictionary<string, Token>();
		private readonly X509Certificate2 certificate;
		private readonly ServicePurpose purpose;
		private readonly string authenticationHost;
		private readonly string apiHost;
		private readonly string clientId;
		private readonly string clientSecret;

		/// <summary>
		/// Hosts used by the API
		/// </summary>
		public static class Hosts
		{
			/// <summary>
			/// Hosts used for authentication
			/// </summary>
			public static class Authentication
			{
				/// <summary>
				/// Sandbox server for authentication
				/// </summary>
				public static readonly string Sandbox = "https://auth.sandbox.openbankingplatform.com/";

				/// <summary>
				/// Production server for authentication
				/// </summary>
				public static readonly string Production = "https://auth.openbankingplatform.com/";
			}

			/// <summary>
			/// Hosts used for API
			/// </summary>
			public static class Api
			{
				/// <summary>
				/// Sandbox server for API
				/// </summary>
				public static readonly string Sandbox = "https://api.sandbox.openbankingplatform.com/";

				/// <summary>
				/// Production server for API
				/// </summary>
				public static readonly string Production = "https://api.openbankingplatform.com/";
			}
		}

		/// <summary>
		/// Implements the Open Payments Platform API
		/// </summary>
		/// <param name="AuthenticationHost">Host to use for authentication.</param>
		/// <param name="ApiHost">Host to use for API calls.</param>
		/// <param name="ClientId">Client ID</param>
		/// <param name="ClientSecret">Client Secret</param>
		/// <param name="Certificate">Certificate.</param>
		/// <param name="Purpose">Purpose for using service.</param>
		/// <param name="Sniffers">Sniffers.</param>
		private OpenPaymentsPlatformClient(string AuthenticationHost, string ApiHost, string ClientId, string ClientSecret,
			X509Certificate2 Certificate, ServicePurpose Purpose, params ISniffer[] Sniffers)
			: base(Sniffers)
		{
			this.authenticationHost = AuthenticationHost;
			this.apiHost = ApiHost;
			this.clientId = ClientId;
			this.clientSecret = ClientSecret;
			this.certificate = Certificate;
			this.purpose = Purpose;
		}

		/// <summary>
		/// Creates a sandbox client.
		/// </summary>
		/// <param name="ClientId">Client ID</param>
		/// <param name="ClientSecret">Client Secret</param>
		/// <param name="Purpose">Purpose for using service.</param>
		/// <param name="Sniffers">Sniffers.</param>
		/// <returns>Client object.</returns>
		public static OpenPaymentsPlatformClient CreateSandbox(string ClientId, string ClientSecret, ServicePurpose Purpose,
			params ISniffer[] Sniffers)
		{
			return new OpenPaymentsPlatformClient(Hosts.Authentication.Sandbox, Hosts.Api.Sandbox, ClientId, ClientSecret,
				null, Purpose, Sniffers);
		}

		/// <summary>
		/// Creates a production client.
		/// </summary>
		/// <param name="ClientId">Client ID</param>
		/// <param name="ClientSecret">Client Secret</param>
		/// <param name="Certificate">Certificate for authenticating service with back-end.</param>
		/// <param name="Purpose">Purpose for using service.</param>
		/// <param name="Sniffers">Sniffers.</param>
		/// <returns>Client object.</returns>
		public static OpenPaymentsPlatformClient CreateProduction(string ClientId, string ClientSecret,
			X509Certificate2 Certificate, ServicePurpose Purpose, params ISniffer[] Sniffers)
		{
			return new OpenPaymentsPlatformClient(Hosts.Authentication.Production, Hosts.Api.Production, ClientId, ClientSecret,
				Certificate, Purpose, Sniffers);
		}

		/// <summary>
		/// Disposes of the client.
		/// </summary>
		public void Dispose()
		{
		}

		private static Uri CreateUri(string Host, string Resource)
		{
			if (Resource.StartsWith("/"))
				return new Uri(Host + Resource.Substring(1));
			else
				return new Uri(Host + Resource);
		}

		private async Task<Dictionary<string, object>> GET(string Token, string Host,
			string Resource, params KeyValuePair<string, string>[] CustomHeaders)
		{
			Uri Uri = CreateUri(Host, Resource);
			string ReqId = Guid.NewGuid().ToString();

			if (this.HasSniffers)
				await this.Written("GET", Token, ReqId, Uri, null, CustomHeaders);

			try
			{
				object Response = await InternetContent.GetAsync(Uri, this.certificate,
					GetHeaders(Token, ReqId, CustomHeaders));

				if (this.HasSniffers)
					this.Received(Response);

				if (!(Response is Dictionary<string, object> DecodedResponse))
					throw new IOException("Unexpected response returned: " + Response.GetType().FullName);

				return DecodedResponse;
			}
			catch (WebException ex)
			{
				throw this.ProcessException(ex);
			}
		}

		private void Received(object Response)
		{
			this.ReceiveText(JSON.Encode(Response, true));
		}

		private Exception ProcessException(WebException ex)
		{
			if (this.HasSniffers)
			{
				StringBuilder sb = new StringBuilder();

				sb.Append(((int)ex.StatusCode).ToString());
				sb.Append(' ');
				sb.AppendLine(ex.Message);

				foreach (KeyValuePair<string, IEnumerable<string>> P in ex.Headers)
				{
					foreach (string Value in P.Value)
					{
						sb.Append(P.Key);
						sb.Append(": ");
						sb.AppendLine(Value);
					}
				}

				sb.AppendLine();

				if (ex.Content is string Text)
					sb.Append(Text);
				else
					sb.Append(JSON.Encode(ex.Content, true));

				this.ReceiveText(sb.ToString());
			}

			if (ex.Content is string s)
				return new IOException(s, ex);

			if (ex.Content is Dictionary<string, object> Obj)
			{
				if (Obj.TryGetValue("error", out object Obj2) && Obj2 is string Error)
					return new IOException(Error, ex);

				if (TppMessage.TryParse(Obj, out TppMessage[] Messages) && Messages.Length > 0)
					throw new IOException(Messages[0].Text, ex);
			}

			return ex;
		}

		private Task<Dictionary<string, object>> POST(string Host, string Resource, object Data)
		{
			return this.POST(null, Host, Resource, Data);
		}

		private async Task<Dictionary<string, object>> POST(string Token, string Host,
			string Resource, object Data, params KeyValuePair<string, string>[] CustomHeaders)
		{
			Uri Uri = CreateUri(Host, Resource);
			string ReqId = Guid.NewGuid().ToString();

			if (this.HasSniffers)
				await this.Written("POST", Token, ReqId, Uri, Data, CustomHeaders);

			try
			{
				object Response = await InternetContent.PostAsync(Uri, Data, this.certificate,
					GetHeaders(Token, ReqId, CustomHeaders));

				if (this.HasSniffers)
					this.Received(Response);

				if (!(Response is Dictionary<string, object> DecodedResponse))
					throw new IOException("Unexpected response returned: " + Response.GetType().FullName);

				return DecodedResponse;
			}
			catch (WebException ex)
			{
				throw this.ProcessException(ex);
			}
		}

		private static KeyValuePair<string, string>[] GetHeaders(string Token, string ReqId,
			params KeyValuePair<string, string>[] CustomHeaders)
		{
			if (string.IsNullOrEmpty(Token))
				return CustomHeaders;

			int c = CustomHeaders?.Length ?? 0;
			KeyValuePair<string, string>[] Headers = new KeyValuePair<string, string>[c + 2];

			if (c > 0)
				Array.Copy(CustomHeaders, 0, Headers, 0, c);

			Headers[c] = new KeyValuePair<string, string>("Authorization", "Bearer " + Token);
			Headers[c + 1] = new KeyValuePair<string, string>("X-Request-ID", ReqId);

			return Headers;
		}

		private async Task Written(string Method, string Token, string ReqId, Uri Uri,
			object Data, params KeyValuePair<string, string>[] CustomHeaders)
		{
			StringBuilder sb = new StringBuilder();
			string s;

			sb.Append(Method);
			sb.Append('(');
			sb.Append(Uri.ToString());

			if (!(Data is null))
			{
				if (Data is Dictionary<string, object> Obj)
					s = JSON.Encode(Obj, true);
				else
				{
					KeyValuePair<byte[], string> P = await InternetContent.EncodeAsync(Data, Encoding.UTF8);
					s = Encoding.UTF8.GetString(P.Key);
				}

				sb.AppendLine(",");
				sb.Append(s);
			}

			if (!string.IsNullOrEmpty(Token))
			{
				sb.AppendLine(",");
				sb.Append("Authorization:Bearer ");
				sb.Append(Token);
				sb.AppendLine(",");
				sb.Append("X-Request-ID:");
				sb.Append(ReqId);
			}

			if ((CustomHeaders?.Length ?? 0) > 0)
			{
				foreach (KeyValuePair<string, string> H in CustomHeaders)
				{
					sb.AppendLine(",");
					sb.Append(H.Key);
					sb.Append(':');
					sb.Append(H.Value);
				}
			}

			sb.Append(')');

			this.TransmitText(sb.ToString());
		}

		private async Task<object> DELETE(string Token, string Host,
			string Resource, params KeyValuePair<string, string>[] CustomHeaders)
		{
			Uri Uri = CreateUri(Host, Resource);
			string ReqId = Guid.NewGuid().ToString();

			if (this.HasSniffers)
				await this.Written("DELETE", Token, ReqId, Uri, null, CustomHeaders);

			try
			{
				object Response = await InternetContent.DeleteAsync(Uri, this.certificate,
					GetHeaders(Token, ReqId, CustomHeaders));

				if (this.HasSniffers)
					this.Received(Response);

				return Response;
			}
			catch (WebException ex)
			{
				throw this.ProcessException(ex);
			}
		}

		private async Task<Dictionary<string, object>> PUT(string Token, string Host,
			string Resource, object Data, params KeyValuePair<string, string>[] CustomHeaders)
		{
			Uri Uri = CreateUri(Host, Resource);
			string ReqId = Guid.NewGuid().ToString();

			if (this.HasSniffers)
				await this.Written("PUT", Token, ReqId, Uri, Data, CustomHeaders);

			try
			{
				object Response = await InternetContent.PutAsync(Uri, Data, this.certificate,
					GetHeaders(Token, ReqId, CustomHeaders));

				if (this.HasSniffers)
					this.Received(Response);

				if (!(Response is Dictionary<string, object> DecodedResponse))
					throw new IOException("Unexpected response returned: " + Response.GetType().FullName);

				return DecodedResponse;
			}
			catch (WebException ex)
			{
				throw this.ProcessException(ex);
			}
		}

		/// <summary>
		/// Gets the scope string, given the API and Purpose of the session.
		/// </summary>
		/// <param name="Api">API to use.</param>
		/// <param name="Purpose">Purpose of session.</param>
		/// <returns>Scope string.</returns>
		private string GetScopeString(ServiceApi Api)
		{
			StringBuilder sb = new StringBuilder();

			switch (Api)
			{
				case ServiceApi.AspServiceProvidersInformation:
					sb.Append("aspspinformation");
					break;

				case ServiceApi.AccountInformation:
					sb.Append("accountinformation");
					break;

				case ServiceApi.PaymentInitiation:
					sb.Append("paymentinitiation");
					break;
			}

			switch (this.purpose)
			{
				case ServicePurpose.Private:
					sb.Append(" private");
					break;

				case ServicePurpose.Corporate:
					sb.Append(" corporate");
					break;
			}

			return sb.ToString();
		}

		/// <summary>
		/// Checks there's a valid token available.
		/// </summary>
		/// <param name="Scope">Scope of token.</param>
		/// <returns>Bearer token.</returns>
		public async Task<string> CheckToken(ServiceApi Api)
		{
			string Scope = this.GetScopeString(Api);

			lock (this.tokens)
			{
				if (this.tokens.TryGetValue(Scope, out Token Token) && Token.Expires > DateTime.Now)
					return Token.BearerToken;
			}

			return await this.GetToken(Scope);
		}

		/// <summary>
		/// Gets a new token.
		/// </summary>
		/// <param name="Scope">Scope of token.</param>
		/// <returns>Bearer token.</returns>
		private async Task<string> GetToken(string Scope)
		{
			Dictionary<string, string> Form = new Dictionary<string, string>()
			{
				{ "client_id", this.clientId },
				{ "client_secret", this.clientSecret },
				{ "scope", Scope },
				{ "grant_type", "client_credentials" }
			};
            //Log.Informational("Scope" + Scope);
            Dictionary<string, object> Response = await this.POST(this.authenticationHost, "connect/token", Form);

			if (!Response.TryGetValue("access_token", out object Obj) || !(Obj is string Token))
				throw new IOException("Token not returned.");

			if (!Response.TryGetValue("expires_in", out Obj) || !(Obj is int ExpiresInSeconds))
				throw new IOException("Token expiry not returned.");

			if (!Response.TryGetValue("token_type", out Obj) || !(Obj is string TokenType))
				throw new IOException("Token type not returned.");

			if (TokenType != "Bearer")
				throw new IOException("Unrecognized token type returned: " + TokenType);
			//Log.Informational("ExpiresInSeconds" + ExpiresInSeconds);

			//foreach(var t in Response)
			//	Log.Informational($"{t.Key}:{t.Value}");

			lock (this.tokens)
			{
              //  Log.Informational("ExpiresInSeconds1" + ExpiresInSeconds);
                this.tokens[Scope] = new Token(Token, DateTime.Now.AddSeconds(ExpiresInSeconds / 2));
			}

			return Token;
		}

		/// <summary>
		/// Gets available countries.
		/// </summary>
		/// <returns>Array of countries</returns>
		public async Task<Country[]> GetCountries()
		{
			string Token = await this.CheckToken(ServiceApi.AspServiceProvidersInformation);

			Dictionary<string, object> Response = await this.GET(Token, this.apiHost, "psd2/aspspinformation/v1/countries");
			if (!Response.TryGetValue("countries", out object Obj) || !(Obj is Array A))
				throw new IOException("Countries not found.");

			List<Country> Countries = new List<Country>();

			foreach (object Item in A)
			{
				if (this.TryDecodeCountry(Item, out Country DecodedCountry))
					Countries.Add(DecodedCountry);
			}

			return Countries.ToArray();
		}

		/// <summary>
		/// Gets available countries, sorted per ISO Code.
		/// </summary>
		/// <returns>Dictionary of countries</returns>
		public async Task<SortedDictionary<string, Country>> GetCountriesPerCode()
		{
			SortedDictionary<string, Country> Result = new SortedDictionary<string, Country>();

			foreach (Country Country in await this.GetCountries())
				Result[Country.IsoCode] = Country;

			return Result;
		}

		/// <summary>
		/// Gets available countries, sorted per Name.
		/// </summary>
		/// <returns>Dictionary of countries</returns>
		public async Task<SortedDictionary<string, Country>> GetCountriesPerName()
		{
			SortedDictionary<string, Country> Result = new SortedDictionary<string, Country>();

			foreach (Country Country in await this.GetCountries())
				Result[Country.Name] = Country;

			return Result;
		}

		/// <summary>
		/// Gets information about a country, given its ISO code.
		/// </summary>
		/// <param name="IsoCode">Country ISO Code.</param>
		/// <returns>Country information, if available</returns>
		/// <exception cref="IOException">If not able to get country information.</exception>
		public async Task<Country> GetCountry(string IsoCode)
		{
			string Token = await this.CheckToken(ServiceApi.AspServiceProvidersInformation);

			Dictionary<string, object> Response = await this.GET(Token, this.apiHost, "psd2/aspspinformation/v1/countries/" + IsoCode.ToUpper());

			if (!this.TryDecodeCountry(Response, out Country DecodedCountry))
				throw new IOException("Unable to decode country information.");

			return DecodedCountry;
		}

		/// <summary>
		/// Tries to decode an item into a country definition.
		/// </summary>
		/// <param name="Item">Parsed item</param>
		/// <param name="Country">Decoded country.</param>
		/// <returns>If able to decode country.</returns>
		public bool TryDecodeCountry(object Item, out Country Country)
		{
			if (Item is Dictionary<string, object> Object &&
				Object.TryGetValue("isoCountryCode", out object Obj) && Obj is string IsoCountryCode &&
				Object.TryGetValue("name", out Obj) && Obj is string Name)
			{
				Country = new Country(this, IsoCountryCode, Name);
				return true;
			}
			else
			{
				Country = null;
				return false;
			}
		}

		/// <summary>
		/// Gets available cities.
		/// </summary>
		/// <returns>Array of cities</returns>
		public Task<City[]> GetCities()
		{
			return this.GetCities(string.Empty);
		}

		/// <summary>
		/// Gets available cities.
		/// </summary>
		/// <param name="CountryCode">ISO Country Code</param>
		/// <returns>Array of cities</returns>
		public async Task<City[]> GetCities(string CountryCode)
		{
			string Token = await this.CheckToken(ServiceApi.AspServiceProvidersInformation);

			string Resource = "psd2/aspspinformation/v1/cities";
			if (!string.IsNullOrEmpty(CountryCode))
				Resource += "?isoCountryCodes=" + CountryCode.ToUpper();

			Dictionary<string, object> Response = await this.GET(Token, this.apiHost, Resource);
			if (!Response.TryGetValue("cities", out object Obj) || !(Obj is Array A))
				throw new IOException("Cities not found.");

			List<City> Cities = new List<City>();

			foreach (object Item in A)
			{
				if (this.TryDecodeCity(Item, out City DecodedCity))
					Cities.Add(DecodedCity);
			}

			return Cities.ToArray();
		}

		/// <summary>
		/// Gets available cities, sorted per Name.
		/// </summary>
		/// <returns>Dictionary of countries</returns>
		public Task<SortedDictionary<string, City>> GetCitiesPerName()
		{
			return this.GetCitiesPerName(string.Empty);
		}

		/// <summary>
		/// Gets available cities, sorted per Name.
		/// </summary>
		/// <param name="CountryCode">ISO Country Code</param>
		/// <returns>Dictionary of countries</returns>
		public async Task<SortedDictionary<string, City>> GetCitiesPerName(string CountryCode)
		{
			SortedDictionary<string, City> Result = new SortedDictionary<string, City>();

			foreach (City City in await this.GetCities(CountryCode))
				Result[City.Name] = City;

			return Result;
		}

		/// <summary>
		/// Tries to decode an item into a city definition.
		/// </summary>
		/// <param name="Item">Parsed item</param>
		/// <param name="City">Decoded city.</param>
		/// <returns>If able to decode city.</returns>
		public bool TryDecodeCity(object Item, out City City)
		{
			if (Item is Dictionary<string, object> Object &&
				Object.TryGetValue("cityId", out object Obj) && Obj is string CityId &&
				Object.TryGetValue("isoCountryCode", out Obj) && Obj is string IsoCountryCode &&
				Object.TryGetValue("name", out Obj) && Obj is string Name)
			{
				City = new City(this, CityId, IsoCountryCode, Name);
				return true;
			}
			else
			{
				City = null;
				return false;
			}
		}

		/// <summary>
		/// Gets information about a city, given its ID.
		/// </summary>
		/// <param name="CityID">City ID.</param>
		/// <returns>City information, if available</returns>
		/// <exception cref="IOException">If not able to get city information.</exception>
		public async Task<City> GetCity(string CityID)
		{
			string Token = await this.CheckToken(ServiceApi.AspServiceProvidersInformation);

			Dictionary<string, object> Response = await this.GET(Token, this.apiHost, "psd2/aspspinformation/v1/cities/" + CityID);

			if (!this.TryDecodeCity(Response, out City DecodedCity))
				throw new IOException("Unable to decode city information.");

			return DecodedCity;
		}

		/// <summary>
		/// Gets available Account Servicing Payment Service Providers.
		/// </summary>
		/// <returns>Array of cities</returns>
		public Task<AspServiceProvider[]> GetAspServiceProviders()
		{
			return this.GetAspServiceProviders(string.Empty);
		}

		/// <summary>
		/// Gets available Account Servicing Payment Service Providers.
		/// </summary>
		/// <param name="CountryCode">ISO Country Code</param>
		/// <returns>Array of Account Servicing Payment Service Providers</returns>
		public async Task<AspServiceProvider[]> GetAspServiceProviders(string CountryCode)
		{
			string Token = await this.CheckToken(ServiceApi.AspServiceProvidersInformation);

			string Resource = "psd2/aspspinformation/v1/aspsps";
			if (!string.IsNullOrEmpty(CountryCode))
				Resource += "?isoCountryCodes=" + CountryCode.ToUpper();

			Dictionary<string, object> Response = await this.GET(Token, this.apiHost, Resource);
			if (!Response.TryGetValue("aspsps", out object Obj) || !(Obj is Array A))
				throw new IOException("Service Providers not found.");

			List<AspServiceProvider> AspServiceProviders = new List<AspServiceProvider>();

			foreach (object Item in A)
			{
				if (this.TryDecodeAspServiceProvider(Item, out AspServiceProvider DecodedAspServiceProvider))
					AspServiceProviders.Add(DecodedAspServiceProvider);
			}

			return AspServiceProviders.ToArray();
		}

		/// <summary>
		/// Gets available Account Servicing Payment Service Providers, sorted per Name.
		/// </summary>
		/// <returns>Dictionary of countries</returns>
		public Task<SortedDictionary<string, AspServiceProvider>> GetAspServiceProvidersPerName()
		{
			return this.GetAspServiceProvidersPerName(string.Empty);
		}

		/// <summary>
		/// Gets available Account Servicing Payment Service Providers, sorted per Name.
		/// </summary>
		/// <param name="CountryCode">ISO Country Code</param>
		/// <returns>Dictionary of Account Servicing Payment Service Providers</returns>
		public async Task<SortedDictionary<string, AspServiceProvider>> GetAspServiceProvidersPerName(string CountryCode)
		{
			SortedDictionary<string, AspServiceProvider> Result = new SortedDictionary<string, AspServiceProvider>();

			foreach (AspServiceProvider AspServiceProvider in await this.GetAspServiceProviders(CountryCode))
				Result[AspServiceProvider.Name] = AspServiceProvider;

			return Result;
		}

		/// <summary>
		/// Tries to decode an item into a Account Servicing Payment Service Provider definition.
		/// </summary>
		/// <param name="Item">Parsed item</param>
		/// <param name="AspServiceProvider">Decoded Account Servicing Payment Service Provider.</param>
		/// <returns>If able to decode Account Servicing Payment Service Provider.</returns>
		public bool TryDecodeAspServiceProvider(object Item, out AspServiceProvider AspServiceProvider)
		{
			if (Item is Dictionary<string, object> Object &&
				Object.TryGetValue("bicFi", out object Obj) && Obj is string AspServiceProviderId &&
				Object.TryGetValue("name", out Obj) && Obj is string Name &&
				Object.TryGetValue("logoUrl", out Obj) && Obj is string LogoUrl)
			{
				AspServiceProvider = new AspServiceProvider(this, AspServiceProviderId, Name, LogoUrl);
				return true;
			}
			else
			{
				AspServiceProvider = null;
				return false;
			}
		}

		/// <summary>
		/// Gets information about a city, given its ID.
		/// </summary>
		/// <param name="BicFi">Bank Identifier Code Financial Institution (BICFI).</param>
		/// <returns>AspServiceProvider information, if available</returns>
		/// <exception cref="IOException">If not able to get city information.</exception>
		public async Task<AspServiceProviderDetails> GetAspServiceProvider(string BicFi)
		{
			string Token = await this.CheckToken(ServiceApi.AspServiceProvidersInformation);

			Dictionary<string, object> Response = await this.GET(Token, this.apiHost, "psd2/aspspinformation/v1/aspsps/" + BicFi);

			if (!this.TryDecodeAspServiceProviderDetails(Response, out AspServiceProviderDetails DecodedAspServiceProvider))
				throw new IOException("Unable to decode Service Provider information.");

			return DecodedAspServiceProvider;
		}

		/// <summary>
		/// Tries to decode an item into a Account Servicing Payment Service Provider definition.
		/// </summary>
		/// <param name="Item">Parsed item</param>
		/// <param name="AspServiceProvider">Decoded Account Servicing Payment Service Provider.</param>
		/// <returns>If able to decode Account Servicing Payment Service Provider.</returns>
		public bool TryDecodeAspServiceProviderDetails(object Item, out AspServiceProviderDetails AspServiceProvider)
		{
			if (Item is Dictionary<string, object> Object &&
				Object.TryGetValue("bicFi", out object Obj) && Obj is string AspServiceProviderId &&
				Object.TryGetValue("name", out Obj) && Obj is string Name &&
				Object.TryGetValue("logoUrl", out Obj) && Obj is string LogoUrl &&
				Object.TryGetValue("city", out Obj) && Obj is string City &&
				Object.TryGetValue("country", out Obj) && Obj is string Country &&
				Object.TryGetValue("postalCode", out Obj) && Obj is string PostalCode &&
				Object.TryGetValue("streetAddress", out Obj) && Obj is string StreetAddress &&
				Object.TryGetValue("companyNumber", out Obj) && Obj is string CompanyNumber &&
				Object.TryGetValue("phone", out Obj) && Obj is string Phone &&
				Object.TryGetValue("websiteUrl", out Obj) && Obj is string WebsiteUrl)
			{
				List<string> GlobalPaymentProducts = new List<string>();
				Dictionary<string, Uri> SupportedAuthorizationMethods = new Dictionary<string, Uri>();
				Dictionary<string, string> AffiliatedAspsps = new Dictionary<string, string>();

				if (Object.TryGetValue("globalPaymentProducts", out Obj) && Obj is Array A)
				{
					foreach (object Item2 in A)
					{
						if (Item2 is string s)
							GlobalPaymentProducts.Add(s);
					}
				}

				if (Object.TryGetValue("supportedAuthorizationMethods", out Obj) && Obj is Array A2)
				{
					foreach (object Item2 in A2)
					{
						if (Item2 is Dictionary<string, object> Object2 &&
							Object2.TryGetValue("name", out Obj) && Obj is string Name2 &&
							Object2.TryGetValue("uri", out Obj) && Obj is string Uri)
						{
							if (string.IsNullOrEmpty(Uri))
								SupportedAuthorizationMethods[Name2] = null;
							else
								SupportedAuthorizationMethods[Name2] = new Uri(Uri);
						}
					}
				}

				if (Object.TryGetValue("affiliatedAspsps", out Obj) && Obj is Array A3)
				{
					foreach (object Item3 in A3)
					{
						if (Item3 is Dictionary<string, object> Object2 &&
							Object2.TryGetValue("id", out Obj) && Obj is string Id &&
							Object2.TryGetValue("name", out Obj) && Obj is string Name3)
						{
							AffiliatedAspsps[Id] = Name3;
						}
					}
				}

				AspServiceProvider = new AspServiceProviderDetails(this, AspServiceProviderId,
					Name, LogoUrl, City, Country, PostalCode, StreetAddress, CompanyNumber,
					Phone, WebsiteUrl, GlobalPaymentProducts.ToArray(),
					SupportedAuthorizationMethods, AffiliatedAspsps);

				return true;
			}
			else
			{
				AspServiceProvider = null;
				return false;
			}
		}

		/// <summary>
		/// Creates a consent request.
		/// </summary>
		/// <param name="AccountIBAN">IBAN account number.</param>
		/// <param name="Currency">Optional Currency.</param>
		/// <param name="AllowBalance">If access to balance is requested.</param>
		/// <param name="AllowTransactions">If access to transactions is requested.</param>
		/// <param name="Recurring">If the consent can be used multiple times or not.</param>
		/// <param name="ValidUntil">Until when the consent is requested.</param>
		/// <param name="FrequencyPerDay">Number of usages per day requested.</param>
		/// <param name="AllowInitiatePayment">If the session will be used to
		/// initiate a payment later.</param>
		/// <param name="Operation">Operation information.</param>
		/// <returns>Consent request object.</returns>
		public Task<ConsentStatus> CreateConsent(string AccountIBAN, bool AllowBalance,
			bool AllowTransactions, bool Recurring, DateTime ValidUntil,
			int FrequencyPerDay, bool AllowInitiatePayment, OperationInformation Operation)
		{
			return this.CreateConsent(AccountIBAN, string.Empty, AllowBalance,
				AllowTransactions, Recurring, ValidUntil, FrequencyPerDay,
				AllowInitiatePayment, Operation);
		}

		/// <summary>
		/// Creates a consent request.
		/// </summary>
		/// <param name="AccountIBAN">IBAN account number.</param>
		/// <param name="Currency">Optional Currency.</param>
		/// <param name="AllowBalance">If access to balance is requested.</param>
		/// <param name="AllowTransactions">If access to transactions is requested.</param>
		/// <param name="Recurring">If the consent can be used multiple times or not.</param>
		/// <param name="ValidUntil">Until when the consent is requested.</param>
		/// <param name="FrequencyPerDay">Number of usages per day requested.</param>
		/// <param name="AllowInitiatePayment">If the session will be used to
		/// initiate a payment later.</param>
		/// <param name="Operation">Operation information.</param>
		/// <returns>Consent request object.</returns>
		public async Task<ConsentStatus> CreateConsent(string AccountIBAN, string Currency, bool AllowBalance,
			bool AllowTransactions, bool Recurring, DateTime ValidUntil,
			int FrequencyPerDay, bool AllowInitiatePayment, OperationInformation Operation)
		{
			if (ValidUntil.TimeOfDay != TimeSpan.Zero)
				throw new ArgumentException("Must be a date only.", nameof(ValidUntil));

			string Token = await this.CheckToken(ServiceApi.AccountInformation);
			List<Dictionary<string, object>> Accounts = new List<Dictionary<string, object>>();
			List<Dictionary<string, object>> Balances = new List<Dictionary<string, object>>();
			List<Dictionary<string, object>> Transactions = new List<Dictionary<string, object>>();

			if (!string.IsNullOrEmpty(AccountIBAN))
				Accounts.Add(AccountCurrency(AccountIBAN, Currency));

			if (AllowBalance)
				Balances.Add(AccountCurrency(AccountIBAN, Currency));

			if (AllowTransactions)
				Transactions.Add(AccountCurrency(AccountIBAN, Currency));

			Dictionary<string, object> Request = new Dictionary<string, object>()
			{
				{ "access", new Dictionary<string,object>()
					{
						{ "accounts", Accounts.ToArray() },
						{ "balances", Balances.ToArray() },
						{ "transactions", Transactions.ToArray() }
					}
				},
				{ "recurringIndicator", Recurring },
				{ "validUntil", ValidUntil.ToString("yyyy-MM-dd") },
				{ "frequencyPerDay", FrequencyPerDay },
				{ "combinedServiceIndicator", AllowInitiatePayment }
			};

			Dictionary<string, object> Response = await this.POST(Token, this.apiHost,
				"psd2/consent/v1/consents", Request, Operation.Headers);

			if (!Response.TryGetValue("consentStatus", out object Obj) || !(Obj is string ConsentStatusStr) ||
				!Enum.TryParse(ConsentStatusStr, true, out ConsentStatusValue ConsentStatus) ||
				!Response.TryGetValue("consentId", out Obj) || !(Obj is string ConsentId) ||
				!Response.TryGetValue("scaMethods", out Obj) || !(Obj is Array ScaMethods) ||
				!Response.TryGetValue("_links", out Obj) || !(Obj is Dictionary<string, object> Links))
			{
				throw new IOException("Unable to decode response.");
			}

			AuthenticationMethod[] Methods = DecodeAuthenticationMethods(ScaMethods);

			return new ConsentStatus(ConsentId, ConsentStatus, Methods, new Links(Links));
		}

		private static AuthenticationMethod[] DecodeAuthenticationMethods(Array ScaMethods)
		{
			List<AuthenticationMethod> MethodTypes = new List<AuthenticationMethod>();

			foreach (object Item in ScaMethods)
			{
				if (TryDecodeAuthenticationMethod(Item, out AuthenticationMethod AuthenticationMethod))
					MethodTypes.Add(AuthenticationMethod);
			}

			return MethodTypes.ToArray();
		}

		private static Dictionary<string, object> AccountCurrency(string AccountIBAN, string Currency)
		{
			Dictionary<string, object> Result = new Dictionary<string, object>()
			{
				{ "iban", AccountIBAN }
			};

			if (!string.IsNullOrEmpty(Currency))
				Result["currency"] = Currency;

			return Result;
		}

		/// <summary>
		/// Tries to decode an item into an Authentication Method definition.
		/// </summary>
		/// <param name="Item">Parsed item</param>
		/// <param name="AuthenticationMethod">Decoded Authentication Method.</param>
		/// <returns>If able to decode Authentication Method.</returns>
		public static bool TryDecodeAuthenticationMethod(object Item, out AuthenticationMethod AuthenticationMethod)
		{
			if (Item is Dictionary<string, object> Object &&
				Object.TryGetValue("authenticationType", out object Obj) && Obj is string AuthenticationTypeStr &&
				Enum.TryParse(AuthenticationTypeStr, out AuthenticationType AuthenticationType) &&
				Object.TryGetValue("authenticationMethodId", out Obj) && Obj is string MethodId &&
				Object.TryGetValue("name", out Obj) && Obj is string Name)
			{
				AuthenticationMethod = new AuthenticationMethod(AuthenticationType, MethodId, Name);
				return true;
			}
			else
			{
				AuthenticationMethod = null;
				return false;
			}
		}

		/// <summary>
		/// Gets information about a consent request.
		/// </summary>
		/// <param name="ConsentId">Consent ID</param>
		/// <param name="Operation">Operation information.</param>
		/// <returns>Information about the consent request.</returns>
		public async Task<ConsentRequest> GetConsent(string ConsentId, OperationInformation Operation)
		{
			string Token = await this.CheckToken(ServiceApi.AccountInformation);

			Dictionary<string, object> Response = await this.GET(Token, this.apiHost,
				"psd2/consent/v1/consents/" + ConsentId, Operation.Headers);

			if (!Response.TryGetValue("consentId", out object Obj) || !(Obj is string consentId2) ||
				!Response.TryGetValue("consentStatus", out Obj) || !(Obj is string ConsentStatusStr) ||
				!Enum.TryParse(ConsentStatusStr, true, out ConsentStatusValue ConsentStatus) ||
				!Response.TryGetValue("validUntil", out Obj) || !(Obj is string ValidUntilStr) ||
				!XML.TryParse(ValidUntilStr, out DateTime ValidUntil) ||
				!Response.TryGetValue("lastActionDate", out Obj) || !(Obj is string LastActionDateStr) ||
				!XML.TryParse(LastActionDateStr, out DateTime LastActionDate) ||
				!Response.TryGetValue("frequencyPerDay", out Obj) || !(Obj is int FrequencyPerDay) ||
				!Response.TryGetValue("recurringIndicator", out Obj) || !(Obj is bool RecurringIndicator) ||
				!Response.TryGetValue("access", out Obj) || !(Obj is Dictionary<string, object> Access) ||
				!Access.TryGetValue("accounts", out Obj) || !(Obj is Array Accounts) ||
				!Access.TryGetValue("balances", out Obj) || !(Obj is Array Balances) ||
				!Access.TryGetValue("transactions", out Obj) || !(Obj is Array Transactions))
			{
				throw new IOException("Unable to decode response.");
			}

			AccountReference[] AccountReferences = DecodeAccountReferences(Accounts);
			AccountReference[] BalanceReferences = DecodeAccountReferences(Balances);
			AccountReference[] TransactionsReferences = DecodeAccountReferences(Transactions);

			return new ConsentRequest(consentId2, ConsentStatus, AccountReferences,
				BalanceReferences, TransactionsReferences, RecurringIndicator,
				ValidUntil, FrequencyPerDay, LastActionDate);
		}

		private static AccountReference[] DecodeAccountReferences(Array References)
		{
			List<AccountReference> Result = new List<AccountReference>();

			foreach (object Item in References)
			{
				if (TryDecodeAccountReference(Item, out AccountReference Ref))
					Result.Add(Ref);
			}

			return Result.ToArray();
		}

		/// <summary>
		/// Tries to decode an item into an Account Reference.
		/// </summary>
		/// <param name="Item">Parsed item</param>
		/// <param name="AccountReference">Decoded Account Reference.</param>
		/// <returns>If able to decode Account Reference.</returns>
		public static bool TryDecodeAccountReference(object Item, out AccountReference AccountReference)
		{
			if (Item is Dictionary<string, object> Object &&
				Object.TryGetValue("iban", out object Obj) && Obj is string Iban)
			{
				if (!Object.TryGetValue("currency", out Obj) ||
					!(Obj is string Currency))
				{
					Currency = null;
				}

				AccountReference = new AccountReference(Iban, Currency);
				return true;
			}
			else
			{
				AccountReference = null;
				return false;
			}
		}

		/// <summary>
		/// Tries to decode an item into an Account Information object.
		/// </summary>
		/// <param name="Item">Parsed item</param>
		/// <param name="AccountInformation">Decoded Account Information object.</param>
		/// <returns>If able to decode Account Information object.</returns>
		public static bool TryDecodeAccountInformation(object Item, out AccountInformation AccountInformation)
		{
			List<Balance> AccountBalance = new List<Balance>();
			if (Item is Dictionary<string, object> i && i.TryGetValue("balances", out object ObjBalances)
				&& ObjBalances is object[] Balances)
			{
				foreach (object Balance in Balances)
				{
					if (Balance is Dictionary<string, object> BalanceObject)
					{
                        Dictionary<string, object> BalanceAmount = BalanceObject.TryGetValue("balanceAmount", out object BalanceObjectObj) && BalanceObjectObj is Dictionary<string, object> balanceAmount ? balanceAmount : new Dictionary<string, object>();
                        string BalanceCurrency = BalanceAmount.TryGetValue("currency", out BalanceObjectObj) && BalanceObjectObj is string balanceCurrency ? balanceCurrency : string.Empty;
                        string Amount = BalanceAmount.TryGetValue("amount", out BalanceObjectObj) && BalanceObjectObj is string amount ? amount : string.Empty;
                        string BalanceType = BalanceObject.TryGetValue("balanceType", out BalanceObjectObj) && BalanceObjectObj is string balanceType ? balanceType : string.Empty;
                        bool CreditLimitIncluded = BalanceObject.TryGetValue("creditLimitIncluded", out BalanceObjectObj) && BalanceObjectObj is bool creditLimitIncluded ? creditLimitIncluded : true;

                        AccountBalance.Add(new Balance(new BalanceAmount(BalanceCurrency, decimal.Parse(Amount)), BalanceType, CreditLimitIncluded));
					}
				}
			}

			if (Item is Dictionary<string, object> Object)
			{
                string ResourceId = Object.TryGetValue("resourceId", out object Obj) && Obj is string resourceId ? resourceId : string.Empty;
                string Iban = Object.TryGetValue("iban", out Obj) && Obj is string iban ? iban : string.Empty;
                string Bban = Object.TryGetValue("bban", out Obj) && Obj is string bban ? bban : string.Empty;
                string Currency = Object.TryGetValue("currency", out Obj) && Obj is string currency ? currency : string.Empty;
                string Bic = Object.TryGetValue("bic", out Obj) && Obj is string bic ? bic : string.Empty;
				string Name = Object.TryGetValue("name", out Obj) && Obj is string name ? name : string.Empty;
				string Product = Object.TryGetValue("product", out Obj) && Obj is string product ? product : string.Empty;
				string CashAccountType = Object.TryGetValue("cashAccountType", out Obj) && Obj is string cashAccountType ? cashAccountType : string.Empty;
				string Status = Object.TryGetValue("status", out Obj) && Obj is string status ? status : string.Empty;
				string Usage = Object.TryGetValue("usage", out Obj) && Obj is string usage ? usage : string.Empty;

                string OwnerName = Object.TryGetValue("ownerName", out Obj) && Obj is string ownerName ? ownerName : string.Empty;

                AccountInformation = new AccountInformation(ResourceId, Iban, Currency, Bban, Bic, AccountBalance.ToArray(),
					CashAccountType, Name, OwnerName, Product, Status, Usage);

				return true;
			}
			else
			{
				AccountInformation = null;
				return false;
			}
		}

		/// <summary>
		/// Creates a payment initiation.
		/// </summary>
		/// <param name="ConsentId">Consent ID</param>
		/// <param name="Operation">Information about operation.</param>
		/// <returns>Payment initiation reference object.</returns>
		public async Task<ConsentStatusValue> GetConsentStatus(string ConsentId, OperationInformation Operation)
		{
			string Token = await this.CheckToken(ServiceApi.AccountInformation);

			Dictionary<string, object> Response = await this.GET(Token, this.apiHost,
				"psd2/consent/v1/consents/" + ConsentId + "/status", Operation.Headers);

			if (!Response.TryGetValue("consentStatus", out object Obj) || !(Obj is string ConsentStatusStr))
				throw new IOException("Unable to decode response.");

			if (!Enum.TryParse(ConsentStatusStr, true, out ConsentStatusValue ConsentStatus))
				throw new IOException("Unrecognized status code received: " + ConsentStatusStr);

			return ConsentStatus;
		}

		/// <summary>
		/// Deletes a consent request.
		/// </summary>
		/// <param name="ConsentId">Consent ID</param>
		/// <param name="Operation">Operation information.</param>
		public async Task DeleteConsent(string ConsentId, OperationInformation Operation)
		{
			string Token = await this.CheckToken(ServiceApi.AccountInformation);

			await this.DELETE(Token, this.apiHost, "psd2/consent/v1/consents/" + ConsentId,
				Operation.Headers);
		}

		/// <summary>
		/// Starts the consent authorization process.
		/// </summary>
		/// <param name="ConsentId">Consent ID</param>
		/// <param name="Operation">Operation information.</param>
		/// <returns>Authorization Status</returns>
		public Task<AuthorizationInformation> StartConsentAuthorization(string ConsentId,
			OperationInformation Operation)
		{
			return this.StartConsentAuthorization(ConsentId, Operation, null, null);
		}

		/// <summary>
		/// Starts the consent authorization process.
		/// </summary>
		/// <param name="ConsentId">Consent ID</param>
		/// <param name="Operation">Operation information.</param>
		/// <param name="CallbackUrlOk">Optional callback URL, if OK.</param>
		/// <param name="CallbackUrlFail">Optional callback URL, if failing.</param>
		/// <returns>Authorization Status</returns>
		public async Task<AuthorizationInformation> StartConsentAuthorization(string ConsentId,
			OperationInformation Operation, string CallbackUrlOk, string CallbackUrlFail)
		{
			string Token = await this.CheckToken(ServiceApi.AccountInformation);

			Dictionary<string, object> Response = await this.POST(
				Token, this.apiHost, "psd2/consent/v1/consents/" + ConsentId + "/authorisations",
				new Dictionary<string, object>(), Operation.AppendCallbackHeaders(CallbackUrlOk, CallbackUrlFail));

			if (!Response.TryGetValue("authorisationId", out object Obj) || !(Obj is string AuthorisationId) ||
				!Response.TryGetValue("scaStatus", out Obj) || !(Obj is string ScaStatusStr) ||
				!Enum.TryParse(ScaStatusStr, out AuthorizationStatusValue ScaStatus) ||
				!Response.TryGetValue("scaMethods", out Obj) || !(Obj is Array ScaMethods) ||
				!Response.TryGetValue("_links", out Obj) || !(Obj is Dictionary<string, object> Links))
			{
				throw new IOException("Unable to decode response.");
			}

			AuthenticationMethod[] Methods = DecodeAuthenticationMethods(ScaMethods);

			return new AuthorizationInformation(AuthorisationId, ScaStatus, Methods,
				new Links(Links));
		}

		/// <summary>
		/// Starts the consent authorization process.
		/// </summary>
		/// <param name="ConsentId">Consent ID</param>
		/// <param name="AuthorizationId">Authorization ID</param>
		/// <param name="AuthenticationMethodId">Authentication Method ID</param>
		/// <param name="Operation">Operation information.</param>
		/// <returns>Authorization Status</returns>
		public async Task<PaymentServiceUserDataResponse> PutConsentUserData(string ConsentId,
			string AuthorizationId, string AuthenticationMethodId, OperationInformation Operation)
		{
			string Token = await this.CheckToken(ServiceApi.AccountInformation);

			Dictionary<string, object> Response = await this.PUT(
				Token, this.apiHost, "psd2/consent/v1/consents/" + ConsentId + "/authorisations/" + AuthorizationId,
				new Dictionary<string, object>()
				{
					{ "authenticationMethodId", AuthenticationMethodId }
				}, Operation.Headers);

			if (!TryDecode(Response, out PaymentServiceUserDataResponse PsuDataResponse))
				throw new IOException("Unable to decode response.");

			return PsuDataResponse;
		}

		private static bool TryDecode(Dictionary<string, object> Response,
			out PaymentServiceUserDataResponse PsuDataResponse)
		{
			if (!Response.TryGetValue("scaStatus", out object Obj) || !(Obj is string ScaStatusStr) ||
				!Enum.TryParse(ScaStatusStr, out AuthorizationStatusValue ScaStatus) ||
				!Response.TryGetValue("_links", out Obj) || !(Obj is Dictionary<string, object> Links))
			{
				PsuDataResponse = null;
				return false;
			}

			if (!Response.TryGetValue("chosenScaMethod", out Obj) ||
				!TryDecodeAuthenticationMethod(Obj, out AuthenticationMethod AuthenticationMethod))
			{
				AuthenticationMethod = null;
			}

			if (!Response.TryGetValue("psuMessage", out Obj) || !(Obj is string PsuMessage))
				PsuMessage = string.Empty;

			if (!Response.TryGetValue("challengeData", out Obj) ||
				!(Obj is Dictionary<string, object> ChallengeDataObj) ||
				!ChallengeData.TryParse(ChallengeDataObj, out ChallengeData ParsedChallengeData))
			{
				ParsedChallengeData = null;
			}

			if (!TppMessage.TryParse(Response, out TppMessage[] Messages))
				Messages = new TppMessage[0];

			PsuDataResponse = new PaymentServiceUserDataResponse(PsuMessage, ScaStatus,
				AuthenticationMethod, new Links(Links), ParsedChallengeData, Messages);

			return true;
		}

		/// <summary>
		/// Gets a URL that a client can open to authenticate itself.
		/// </summary>
		/// <param name="Url">Client URL from API</param>
		/// <param name="RedirectUrl">Redirection URL</param>
		/// <param name="RedirectState">Redirection state</param>
		/// <returns>URL to send to client.</returns>
		public string GetClientWebUrl(string Url, string RedirectUrl, string RedirectState)
		{
			return Url.
				Replace("[CLIENT_ID]", HttpUtility.UrlEncode(this.clientId)).
				Replace("[TPP_REDIRECT_URI]", HttpUtility.UrlEncode(RedirectUrl)).
				Replace("[TPP_STATE]", HttpUtility.UrlEncode(RedirectState));
		}

		/// <summary>
		/// Loads a client web page.
		/// </summary>
		/// <param name="Url">Client URL</param>
		/// <param name="RedirectUrl">Redirection URL</param>
		/// <param name="RedirectState">Redirection state</param>
		/// <returns>Loaded web page.</returns>
		public async Task<object> GetWebPage(string Url, string RedirectUrl, string RedirectState)
		{
			string s = this.GetClientWebUrl(Url, RedirectUrl, RedirectState);

			if (this.HasSniffers)
			{
				StringBuilder sb = new StringBuilder();

				sb.Append("GET(");
				sb.Append(s);
				sb.Append(')');

				this.TransmitText(sb.ToString());
			}

			try
			{
				object Obj = await InternetContent.GetAsync(new Uri(s),
					new KeyValuePair<string, string>("PSU-User-Agent", "Trust Anchor Group Open Payments Platform Client"),
					new KeyValuePair<string, string>("Accept", "text/html"));

				if (this.HasSniffers)
				{
					KeyValuePair<byte[], string> P = await InternetContent.EncodeAsync(Obj, Encoding.UTF8);
					s = Encoding.UTF8.GetString(P.Key);
					this.ReceiveText(s);
				}

				return Obj;
			}
			catch (WebException ex)
			{
				throw this.ProcessException(ex);
			}
		}

		/// <summary>
		/// Gets available consent authorization IDs.
		/// </summary>
		/// <param name="ConsentId">Consent ID</param>
		/// <param name="Operation">Operation information.</param>
		/// <returns>Authorization IDs</returns>
		public async Task<string[]> GetConsentAuthorizationIDs(string ConsentId,
			OperationInformation Operation)
		{
			string Token = await this.CheckToken(ServiceApi.AccountInformation);

			Dictionary<string, object> Response = await this.GET(
				Token, this.apiHost, "psd2/consent/v1/consents/" + ConsentId + "/authorisations",
				Operation.Headers);

			if (!Response.TryGetValue("authorisationIds", out object Obj) || !(Obj is Array AuthorisationIds))
				throw new IOException("Unable to decode response.");

			List<string> Result = new List<string>();

			foreach (object Item in AuthorisationIds)
			{
				if (Item is string s)
					Result.Add(s);
			}

			return Result.ToArray();
		}

		/// <summary>
		/// Gets the status of a consent authorization.
		/// </summary>
		/// <param name="ConsentId">Consent ID</param>
		/// <param name="AuthorizationId">Authorization ID</param>
		/// <param name="Operation">Operation information</param>
		/// <returns>Authorization Status</returns>
		public async Task<AuthorizationStatus> GetConsentAuthorizationStatus(string ConsentId,
			string AuthorizationId, OperationInformation Operation)
		{
			string Token = await this.CheckToken(ServiceApi.AccountInformation);

			Dictionary<string, object> Response = await this.GET(
				Token, this.apiHost, "psd2/consent/v1/consents/" + ConsentId + "/authorisations/" + AuthorizationId,
				Operation.Headers);

			if (!Response.TryGetValue("scaStatus", out object Obj) || !(Obj is string ScaStatusStr) ||
				!Enum.TryParse(ScaStatusStr, out AuthorizationStatusValue ScaStatus))
			{
				throw new IOException("Unable to decode response.");
			}

			if (!Response.TryGetValue("challengeData", out Obj) ||
				!(Obj is Dictionary<string, object> ChallengeDataObj) ||
				!ChallengeData.TryParse(ChallengeDataObj, out ChallengeData ParsedChallengeData))
			{
				ParsedChallengeData = null;
			}

			if (!TppMessage.TryParse(Response, out TppMessage[] Messages))
				Messages = new TppMessage[0];

			return new AuthorizationStatus(ScaStatus, ParsedChallengeData, Messages);
		}

		/// <summary>
		/// Gets accounts a client can see.
		/// </summary>
		/// <param name="ConsentId">Consent ID</param>
		/// <param name="Operation">Operation information.</param>
		/// <param name="WithBalance">If balance should be included</param>
		/// <returns>Array of accounts</returns>
		public async Task<AccountInformation[]> GetAccounts(string ConsentId,
			OperationInformation Operation, bool WithBalance)
		{
			string Token = await this.CheckToken(ServiceApi.AccountInformation);

			Dictionary<string, object> Response = await this.GET(
				Token, this.apiHost, "psd2/accountinformation/v1/accounts?withBalance=" + WithBalance,
				Operation.AppendHeaders(new KeyValuePair<string, string>("Consent-ID", ConsentId)));

			if (!Response.TryGetValue("accounts", out object Obj) || !(Obj is Array Accounts))
				throw new IOException("Unable to decode response.");

			List<AccountInformation> Result = new List<AccountInformation>();

			foreach (object Item in Accounts)
			{
				if (TryDecodeAccountInformation(Item, out AccountInformation AccountInformation))
					Result.Add(AccountInformation);
			}

			return Result.ToArray();
		}

		/// <summary>
		/// Creates a payment initiation.
		/// </summary>
		/// <param name="Product">Payment product to use.</param>
		/// <param name="Amount">Amount</param>
		/// <param name="Currency">Currency</param>
		/// <param name="FromIbanAccount">IBAN Account number to send money from.</param>
		/// <param name="FromCurrency">Currency to send.</param>
		/// <param name="ToIBanAccount">IBAN Account number to send money to.</param>
		/// <param name="ToCurrency">Currency to receive.</param>
		/// <param name="ToName">Name of recipient.</param>
		/// <param name="Message">Unstructured message.</param>
		/// <param name="Operation">Information about operation.</param>
		/// <returns>Payment initiation reference object.</returns>
		public async Task<PaymentInitiationReference> CreatePaymentInitiation(PaymentProduct Product,
			decimal Amount, string Currency, string FromIbanAccount, string FromCurrency,
			string ToIBanAccount, string ToCurrency, string ToName, string Message,
			OperationInformation Operation)
		{
			string Token = await this.CheckToken(ServiceApi.PaymentInitiation);

			Dictionary<string, object> Request = new Dictionary<string, object>()
			{
				{ "instructedAmount", new Dictionary<string,object>()
					{
						{ "currency", Currency },
						{ "amount", CommonTypes.Encode(Amount) }
					}
				},
				{ "debtorAccount", new Dictionary<string,object>()
					{
						{ "iban", FromIbanAccount },
						{ "currency", FromCurrency }
					}
				},
				{ "creditorName", ToName },
				{ "creditorAccount", new Dictionary<string,object>()
					{
						{ "iban", ToIBanAccount },
						{ "currency", ToCurrency }
					}
				},
				{ "remittanceInformationUnstructured", Message}
			};

			Dictionary<string, object> Response = await this.POST(Token, this.apiHost,
				"psd2/paymentinitiation/v1/payments/" + Product.ToString().Replace("_", "-"),
				Request, Operation.Headers);

			if (!Response.TryGetValue("transactionStatus", out object Obj) || !(Obj is string TransactionStatusStr) ||
				!Enum.TryParse(TransactionStatusStr, true, out PaymentStatus TransactionStatus) ||
				!Response.TryGetValue("paymentId", out Obj) || !(Obj is string PaymentId) ||
				!Response.TryGetValue("_links", out Obj) || !(Obj is Dictionary<string, object> Links))
			{
				throw new IOException("Unable to decode response.");
			}

			if (!Response.TryGetValue("psuMessage", out Obj) || !(Obj is string PsuMessage))
				PsuMessage = string.Empty;

			if (!TppMessage.TryParse(Response, out TppMessage[] Messages))
				Messages = new TppMessage[0];

			return new PaymentInitiationReference(TransactionStatus, PaymentId,
				new Links(Links), PsuMessage, Messages);
		}

		/// <summary>
		/// Deletes a payment initiation.
		/// </summary>
		/// <param name="Product">Payment product to use.</param>
		/// <param name="PaymentId">Payment ID</param>
		/// <param name="Operation">Operation information.</param>
		public async Task DeletePaymentInitiation(PaymentProduct Product,
			string PaymentId, OperationInformation Operation)
		{
			string Token = await this.CheckToken(ServiceApi.PaymentInitiation);

			await this.DELETE(Token, this.apiHost,
				"psd2/paymentinitiation/v1/payments/" + Product.ToString().Replace("_", "-") +
				"/" + PaymentId, Operation.Headers);
		}

		/// <summary>
		/// Gets payment initiation status.
		/// </summary>
		/// <param name="Product">Payment product to use.</param>
		/// <param name="PaymentId">Payment ID.</param>
		/// <param name="Operation">Information about operation.</param>
		/// <returns>Payment initiation status.</returns>
		public async Task<PaymentTransactionStatus> GetPaymentInitiationStatus(PaymentProduct Product,
			string PaymentId, OperationInformation Operation)
		{
			string Token = await this.CheckToken(ServiceApi.PaymentInitiation);

			Dictionary<string, object> Response = await this.GET(Token, this.apiHost,
				"psd2/paymentinitiation/v1/payments/" + Product.ToString().Replace("_", "-") +
				"/" + PaymentId + "/status", Operation.Headers);

			if (!Response.TryGetValue("transactionStatus", out object Obj) || !(Obj is string TransactionStatusStr))
				throw new IOException("Unable to decode response.");

			if (!Enum.TryParse(TransactionStatusStr, true, out PaymentStatus TransactionStatus))
				throw new IOException("Unrecognized status code received: " + TransactionStatusStr);

			if (!TppMessage.TryParse(Response, out TppMessage[] Messages))
				Messages = new TppMessage[0];

			return new PaymentTransactionStatus(TransactionStatus, Messages);
		}

		/// <summary>
		/// Starts the payment initiation authorization process.
		/// </summary>
		/// <param name="Product">Payment product to use.</param>
		/// <param name="PaymentId">Payment ID.</param>
		/// <param name="Operation">Operation information.</param>
		/// <returns>Authorization Status</returns>
		public Task<AuthorizationInformation> StartPaymentInitiationAuthorization(PaymentProduct Product,
			string PaymentId, OperationInformation Operation)
		{
			return this.StartPaymentInitiationAuthorization(Product, PaymentId, Operation, null, null);
		}

		/// <summary>
		/// Starts the payment initiation authorization process.
		/// </summary>
		/// <param name="Product">Payment product to use.</param>
		/// <param name="PaymentId">Payment ID.</param>
		/// <param name="Operation">Operation information.</param>
		/// <param name="CallbackUrlOk">Optional callback URL, if OK.</param>
		/// <param name="CallbackUrlFail">Optional callback URL, if failing.</param>
		/// <returns>Authorization Status</returns>
		public async Task<AuthorizationInformation> StartPaymentInitiationAuthorization(PaymentProduct Product,
			string PaymentId, OperationInformation Operation, string CallbackUrlOk,
			string CallbackUrlFail)
		{
			string Token = await this.CheckToken(ServiceApi.PaymentInitiation);

			Dictionary<string, object> Response = await this.POST(
				Token, this.apiHost, "psd2/paymentinitiation/v1/payments/" + Product.ToString().Replace("_", "-") +
				"/" + PaymentId + "/authorisations", new Dictionary<string, object>(),
				Operation.AppendCallbackHeaders(CallbackUrlOk, CallbackUrlFail));

			if (!Response.TryGetValue("authorisationId", out object Obj) || !(Obj is string AuthorisationId) ||
				!Response.TryGetValue("scaStatus", out Obj) || !(Obj is string ScaStatusStr) ||
				!Enum.TryParse(ScaStatusStr, out AuthorizationStatusValue ScaStatus) ||
				!Response.TryGetValue("scaMethods", out Obj) || !(Obj is Array ScaMethods) ||
				!Response.TryGetValue("_links", out Obj) || !(Obj is Dictionary<string, object> Links))
			{
				throw new IOException("Unable to decode response.");
			}

			AuthenticationMethod[] Methods = DecodeAuthenticationMethods(ScaMethods);

			return new AuthorizationInformation(AuthorisationId, ScaStatus, Methods,
				new Links(Links));
		}

		/// <summary>
		/// Gets available payment initiation authorization IDs.
		/// </summary>
		/// <param name="Product">Payment product to use.</param>
		/// <param name="PaymentId">Payment ID.</param>
		/// <param name="Operation">Operation information.</param>
		/// <returns>Authorization IDs</returns>
		public async Task<string[]> GetPaymentAuthorizationIDs(PaymentProduct Product,
			string PaymentId, OperationInformation Operation)
		{
			string Token = await this.CheckToken(ServiceApi.PaymentInitiation);

			Dictionary<string, object> Response = await this.GET(Token, this.apiHost,
				"psd2/paymentinitiation/v1/payments/" + Product.ToString().Replace("_", "-") +
				"/" + PaymentId + "/authorisations", Operation.Headers);

			if (!Response.TryGetValue("authorisationIds", out object Obj) || !(Obj is Array AuthorisationIds))
				throw new IOException("Unable to decode response.");

			List<string> Result = new List<string>();

			foreach (object Item in AuthorisationIds)
			{
				if (Item is string s)
					Result.Add(s);
			}

			return Result.ToArray();
		}

		/// <summary>
		/// Gets the status of a payment initiation authorization.
		/// </summary>
		/// <param name="Product">Payment product to use.</param>
		/// <param name="PaymentId">Payment ID.</param>
		/// <param name="AuthorizationId">Authorization ID</param>
		/// <param name="Operation">Operation information</param>
		/// <returns>Authorization Status, together with any challenge data available and error messages.</returns>
		public async Task<AuthorizationStatus> GetPaymentInitiationAuthorizationStatus(
			PaymentProduct Product, string PaymentId, string AuthorizationId, OperationInformation Operation)
		{
			string Token = await this.CheckToken(ServiceApi.PaymentInitiation);

			Dictionary<string, object> Response = await this.GET(
				Token, this.apiHost, "psd2/paymentinitiation/v1/payments/" + Product.ToString().Replace("_", "-") +
				"/" + PaymentId + "/authorisations/" + AuthorizationId, Operation.Headers);

			if (!Response.TryGetValue("scaStatus", out object Obj) || !(Obj is string ScaStatusStr) ||
				!Enum.TryParse(ScaStatusStr, out AuthorizationStatusValue ScaStatus))
			{
				throw new IOException("Unable to decode response.");
			}

			if (!Response.TryGetValue("challengeData", out Obj) ||
				!(Obj is Dictionary<string, object> ChallengeDataObj) ||
				!ChallengeData.TryParse(ChallengeDataObj, out ChallengeData ParsedChallengeData))
			{
				ParsedChallengeData = null;
			}

			if (!TppMessage.TryParse(Response, out TppMessage[] Messages))
				Messages = new TppMessage[0];

			return new AuthorizationStatus(ScaStatus, ParsedChallengeData, Messages);
		}

		/// <summary>
		/// Starts the payment initiation authorization process.
		/// </summary>
		/// <param name="Product">Payment product to use.</param>
		/// <param name="PaymentId">Payment ID.</param>
		/// <param name="AuthorizationId">Authorization ID</param>
		/// <param name="AuthenticationMethodId">Authentication Method ID</param>
		/// <param name="Operation">Operation information.</param>
		/// <returns>Authorization Status</returns>
		public async Task<PaymentServiceUserDataResponse> PutPaymentInitiationUserData(PaymentProduct Product,
			string PaymentId, string AuthorizationId, string AuthenticationMethodId, OperationInformation Operation)
		{
			string Token = await this.CheckToken(ServiceApi.PaymentInitiation);

			Dictionary<string, object> Response = await this.PUT(Token, this.apiHost,
				"psd2/paymentinitiation/v1/payments/" + Product.ToString().Replace("_", "-") +
				"/" + PaymentId + "/authorisations/" + AuthorizationId,
				new Dictionary<string, object>()
				{
					{ "authenticationMethodId", AuthenticationMethodId }
				}, Operation.Headers);

			if (!TryDecode(Response, out PaymentServiceUserDataResponse PsuDataResponse))
				throw new IOException("Unable to decode response.");

			return PsuDataResponse;
		}

		/// <summary>
		/// Starts the payment initiation authorization process.
		/// </summary>
		/// <param name="PaymentIds">Payment IDs to put in basket.</param>
		/// <param name="Operation">Operation information.</param>
		/// <returns>Authorization Status</returns>
		public Task<PaymentBasketReference> CreatePaymentBasket(string[] PaymentIds, OperationInformation Operation)
		{
			return this.CreatePaymentBasket(PaymentIds, Operation, null, null);
		}

		/// <summary>
		/// Starts the payment initiation authorization process.
		/// </summary>
		/// <param name="PaymentIds">Payment IDs to put in basket.</param>
		/// <param name="Operation">Operation information.</param>
		/// <param name="CallbackUrlOk">Optional callback URL, if OK.</param>
		/// <param name="CallbackUrlFail">Optional callback URL, if failing.</param>
		/// <returns>Authorization Status</returns>
		public async Task<PaymentBasketReference> CreatePaymentBasket(string[] PaymentIds, OperationInformation Operation, string CallbackUrlOk,
			string CallbackUrlFail)
		{
			string Token = await this.CheckToken(ServiceApi.PaymentInitiation);

			Dictionary<string, object> Response = await this.POST(
				Token, this.apiHost, "psd2/paymentinitiation/v1/signing-baskets/",
				new Dictionary<string, object>()
				{
					{ "paymentIds", PaymentIds }
				},
				Operation.AppendCallbackHeaders(CallbackUrlOk, CallbackUrlFail));

			if (!Response.TryGetValue("transactionStatus", out object Obj) || !(Obj is string TransactionStatusStr) ||
				!Enum.TryParse(TransactionStatusStr, true, out PaymentBasketStatus TransactionStatus) ||
				!Response.TryGetValue("basketId", out Obj) || !(Obj is string BasketId) ||
				!Response.TryGetValue("_links", out Obj) || !(Obj is Dictionary<string, object> Links))
			{
				throw new IOException("Unable to decode response.");
			}

			if (!Response.TryGetValue("psuMessage", out Obj) || !(Obj is string PsuMessage))
				PsuMessage = string.Empty;

			if (!TppMessage.TryParse(Response, out TppMessage[] Messages))
				Messages = new TppMessage[0];

			return new PaymentBasketReference(TransactionStatus, BasketId,
				new Links(Links), PsuMessage, Messages);
		}

		/// <summary>
		/// Deletes a payment basket.
		/// </summary>
		/// <param name="BasketId">Basket ID</param>
		/// <param name="Operation">Operation information.</param>
		public async Task DeletePaymentBasket(string BasketId, OperationInformation Operation)
		{
			string Token = await this.CheckToken(ServiceApi.PaymentInitiation);

			await this.DELETE(Token, this.apiHost, "psd2/paymentinitiation/v1/signing-baskets/" + BasketId,
				Operation.Headers);
		}

		/// <summary>
		/// Gets payment basket status.
		/// </summary>
		/// <param name="BasketId">Basket ID.</param>
		/// <param name="Operation">Information about operation.</param>
		/// <returns>Payment basket status.</returns>
		public async Task<BasketTransactionStatus> GetPaymentBasketStatus(string BasketId,
			OperationInformation Operation)
		{
			string Token = await this.CheckToken(ServiceApi.PaymentInitiation);

			Dictionary<string, object> Response = await this.GET(Token, this.apiHost,
				"psd2/paymentinitiation/v1/signing-baskets/" + BasketId + "/status",
				Operation.Headers);

			if (!Response.TryGetValue("transactionStatus", out object Obj) || !(Obj is string TransactionStatusStr))
				throw new IOException("Unable to decode response.");

			if (!Enum.TryParse(TransactionStatusStr, true, out PaymentBasketStatus Status))
				throw new IOException("Unrecognized status code received: " + TransactionStatusStr);

			if (!TppMessage.TryParse(Response, out TppMessage[] Messages))
				Messages = new TppMessage[0];

			return new BasketTransactionStatus(Status, Messages);
		}

		/// <summary>
		/// Starts the payment basket authorization process.
		/// </summary>
		/// <param name="BasketId">Payment Basket ID.</param>
		/// <param name="Operation">Operation information.</param>
		/// <returns>Authorization Status</returns>
		public Task<AuthorizationInformation> StartPaymentBasketAuthorization(string BasketId,
			OperationInformation Operation)
		{
			return this.StartPaymentBasketAuthorization(BasketId, Operation, null, null);
		}

		/// <summary>
		/// Starts the payment basket authorization process.
		/// </summary>
		/// <param name="Product">Payment product to use.</param>
		/// <param name="BasketId">Payment Basket ID.</param>
		/// <param name="Operation">Operation information.</param>
		/// <param name="CallbackUrlOk">Optional callback URL, if OK.</param>
		/// <param name="CallbackUrlFail">Optional callback URL, if failing.</param>
		/// <returns>Authorization Status</returns>
		public async Task<AuthorizationInformation> StartPaymentBasketAuthorization(string BasketId,
			OperationInformation Operation, string CallbackUrlOk, string CallbackUrlFail)
		{
			string Token = await this.CheckToken(ServiceApi.PaymentInitiation);

			Dictionary<string, object> Response = await this.POST(
				Token, this.apiHost, "psd2/paymentinitiation/v1/signing-baskets/" +
				BasketId + "/authorisations", new Dictionary<string, object>(),
				Operation.AppendCallbackHeaders(CallbackUrlOk, CallbackUrlFail));

			if (!Response.TryGetValue("authorisationId", out object Obj) || !(Obj is string AuthorisationId) ||
				!Response.TryGetValue("scaStatus", out Obj) || !(Obj is string ScaStatusStr) ||
				!Enum.TryParse(ScaStatusStr, out AuthorizationStatusValue ScaStatus) ||
				!Response.TryGetValue("scaMethods", out Obj) || !(Obj is Array ScaMethods) ||
				!Response.TryGetValue("_links", out Obj) || !(Obj is Dictionary<string, object> Links))
			{
				throw new IOException("Unable to decode response.");
			}

			AuthenticationMethod[] Methods = DecodeAuthenticationMethods(ScaMethods);

			return new AuthorizationInformation(AuthorisationId, ScaStatus, Methods,
				new Links(Links));
		}

		/// <summary>
		/// Starts the payment basket authorization process.
		/// </summary>
		/// <param name="BasketId">Basket ID.</param>
		/// <param name="AuthorizationId">Authorization ID</param>
		/// <param name="AuthenticationMethodId">Authentication Method ID</param>
		/// <param name="Operation">Operation information.</param>
		/// <returns>Authorization Status</returns>
		public async Task<PaymentServiceUserDataResponse> PutPaymentBasketUserData(string BasketId,
			string AuthorizationId, string AuthenticationMethodId, OperationInformation Operation)
		{
			string Token = await this.CheckToken(ServiceApi.PaymentInitiation);

			Dictionary<string, object> Response = await this.PUT(Token, this.apiHost,
				"psd2/paymentinitiation/v1/signing-baskets/" + BasketId +
				"/authorisations/" + AuthorizationId,
				new Dictionary<string, object>()
				{
					{ "authenticationMethodId", AuthenticationMethodId }
				}, Operation.Headers);

			if (!TryDecode(Response, out PaymentServiceUserDataResponse PsuDataResponse))
				throw new IOException("Unable to decode response.");

			return PsuDataResponse;
		}

		/// <summary>
		/// Gets the status of a payment basket authorization.
		/// </summary>
		/// <param name="BasketId">Payment Basket ID.</param>
		/// <param name="AuthorizationId">Authorization ID</param>
		/// <param name="Operation">Operation information</param>
		/// <returns>Authorization Status, together with any challenge data available, and error messages.</returns>
		public async Task<AuthorizationStatus> GetPaymentBasketAuthorizationStatus(string BasketId,
			string AuthorizationId, OperationInformation Operation)
		{
			string Token = await this.CheckToken(ServiceApi.PaymentInitiation);

			Dictionary<string, object> Response = await this.GET(
				Token, this.apiHost, "psd2/paymentinitiation/v1/signing-baskets/" +
				BasketId + "/authorisations/" + AuthorizationId, Operation.Headers);

			if (!Response.TryGetValue("scaStatus", out object Obj) || !(Obj is string ScaStatusStr) ||
				!Enum.TryParse(ScaStatusStr, out AuthorizationStatusValue ScaStatus))
			{
				throw new IOException("Unable to decode response.");
			}

			if (!Response.TryGetValue("challengeData", out Obj) ||
				!(Obj is Dictionary<string, object> ChallengeDataObj) ||
				!ChallengeData.TryParse(ChallengeDataObj, out ChallengeData ParsedChallengeData))
			{
				ParsedChallengeData = null;
			}

			if (!TppMessage.TryParse(Response, out TppMessage[] Messages))
				Messages = new TppMessage[0];

			return new AuthorizationStatus(ScaStatus, ParsedChallengeData, Messages);
		}

	}
}
