using System.Collections.Generic;
using System.Threading.Tasks;

namespace TAG.Networking.OpenPaymentsPlatform
{
	/// <summary>
	/// Country record
	/// </summary>
	public class Country
	{
		/// <summary>
		/// Country record
		/// </summary>
		/// <param name="Client">Client object.</param>
		/// <param name="IsoCode">Country ISO Code</param>
		/// <param name="Name">Country Name</param>
		public Country(OpenPaymentsPlatformClient Client, string IsoCode, string Name)
		{
			this.Client = Client;
			this.IsoCode = IsoCode;
			this.Name = Name;
		}

		/// <summary>
		/// Client object.
		/// </summary>
		public OpenPaymentsPlatformClient Client { get; }

		/// <summary>
		/// Country ISO Code
		/// </summary>
		public string IsoCode { get; }

		/// <summary>
		/// Country Name
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Gets available cities.
		/// </summary>
		/// <returns>Array of cities</returns>
		public Task<City[]> GetCities()
		{
			return this.Client.GetCities(this.IsoCode);
		}

		/// <summary>
		/// Gets available cities, sorted per Name.
		/// </summary>
		/// <returns>Dictionary of countries</returns>
		public Task<SortedDictionary<string, City>> GetCitiesPerName()
		{
			return this.Client.GetCitiesPerName(this.IsoCode);
		}

		/// <summary>
		/// Gets available Account Servicing Payment Service Providers.
		/// </summary>
		/// <returns>Array of Account Servicing Payment Service Providers</returns>
		public Task<AspServiceProvider[]> GetAspServiceProviders()
		{
			return this.Client.GetAspServiceProviders(this.IsoCode);
		}

		/// <summary>
		/// Gets available Account Servicing Payment Service Providers, sorted per Name.
		/// </summary>
		/// <returns>Dictionary of countries</returns>
		public Task<SortedDictionary<string, AspServiceProvider>> GetAspServiceProvidersPerName()
		{
			return this.Client.GetAspServiceProvidersPerName(this.IsoCode);
		}
	}
}
