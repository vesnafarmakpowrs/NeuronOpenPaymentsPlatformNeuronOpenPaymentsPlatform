namespace TAG.Networking.OpenPaymentsPlatform
{
	/// <summary>
	/// City record
	/// </summary>
	public class City
	{
		/// <summary>
		/// City record
		/// </summary>
		/// <param name="Client">Client object.</param>
		/// <param name="CityId">City ID</param>
		/// <param name="IsoCountryCode">Country ISO Code</param>
		/// <param name="Name">City Name</param>
		public City(OpenPaymentsPlatformClient Client, string CityId, string IsoCountryCode, string Name)
		{
			this.Client = Client;
			this.CityID = CityId;
			this.IsoCountryCode = IsoCountryCode;
			this.Name = Name;
		}

		/// <summary>
		/// Client object.
		/// </summary>
		public OpenPaymentsPlatformClient Client { get; }

		/// <summary>
		/// City ID
		/// </summary>
		public string CityID { get; }

		/// <summary>
		/// Country ISO Code
		/// </summary>
		public string IsoCountryCode { get; }

		/// <summary>
		/// City Name
		/// </summary>
		public string Name { get; }
	}
}
