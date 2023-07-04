using System;
using System.Collections.Generic;

namespace TAG.Networking.OpenPaymentsPlatform
{
	/// <summary>
	/// Account Servicing Payment Service Provider (ASPSP) details
	/// </summary>
	public class AspServiceProviderDetails : AspServiceProvider
	{
		/// <summary>
		/// Account Servicing Payment Service Provider (ASPSP) details
		/// </summary>
		/// <param name="Client">Client object.</param>
		/// <param name="BicFi">Bank Identifier Code Financial Institution (BICFI)</param>
		/// <param name="LogoUrl">URL to logotype</param>
		/// <param name="Name">City Name</param>
		public AspServiceProviderDetails(OpenPaymentsPlatformClient Client,
			string BicFi, string Name, string LogoUrl, string City, string Country,
			string PostalCode, string StreetAddress, string CompanyNumber,
			string PhoneNumber, string WebsiteUrl, string[] GlobalPaymentProducts,
			Dictionary<string, Uri> SupportedAuthorizationMethods,
			Dictionary<string, string> AffiliatedAspsps)
			: base(Client, BicFi, Name, LogoUrl)
		{
			this.City = City;
			this.Country = Country;
			this.PostalCode = PostalCode;
			this.StreetAddress = StreetAddress;
			this.CompanyNumber = CompanyNumber;
			this.PhoneNumber = PhoneNumber;
			this.WebsiteUrl = WebsiteUrl;
			this.GlobalPaymentProducts = GlobalPaymentProducts;
			this.SupportedAuthorizationMethods = SupportedAuthorizationMethods;
			this.AffiliatedAspsps = AffiliatedAspsps;
		}

		/// <summary>
		/// City
		/// </summary>
		public string City { get; }

		/// <summary>
		/// Country
		/// </summary>
		public string Country { get; }

		/// <summary>
		/// Postal Code
		/// </summary>
		public string PostalCode { get; }

		/// <summary>
		/// Street Address
		/// </summary>
		public string StreetAddress { get; }

		/// <summary>
		/// Company Number
		/// </summary>
		public string CompanyNumber { get; }

		/// <summary>
		/// Phone Number
		/// </summary>
		public string PhoneNumber { get; }

		/// <summary>
		/// Website URL
		/// </summary>
		public string WebsiteUrl { get; }

		/// <summary>
		/// Global Payment Products
		/// </summary>
		public string[] GlobalPaymentProducts { get; }

		/// <summary>
		/// Supported Authroization Methods, as (Name, URL) pairs.
		/// </summary>
		public Dictionary<string, Uri> SupportedAuthorizationMethods { get; }

		/// <summary>
		/// Affiliated ASP Service Providers, as (ID, Name) pairs.
		/// </summary>
		public Dictionary<string, string> AffiliatedAspsps { get; }
	}
}
