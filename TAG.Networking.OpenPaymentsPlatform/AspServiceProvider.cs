namespace TAG.Networking.OpenPaymentsPlatform
{
	/// <summary>
	/// Account Servicing Payment Service Provider (ASPSP) record
	/// </summary>
	public class AspServiceProvider
	{
		/// <summary>
		/// Account Servicing Payment Service Provider (ASPSP) record
		/// </summary>
		/// <param name="Client">Client object.</param>
		/// <param name="BicFi">Bank Identifier Code Financial Institution (BICFI)</param>
		/// <param name="LogoUrl">URL to logotype</param>
		/// <param name="Name">City Name</param>
		public AspServiceProvider(OpenPaymentsPlatformClient Client, string BicFi, string Name, string LogoUrl)
		{
			this.Client = Client;
			this.BicFi = BicFi;
			this.Name = Name;
			this.LogoUrl = LogoUrl;
		}

		/// <summary>
		/// Client object.
		/// </summary>
		public OpenPaymentsPlatformClient Client { get; }

		/// <summary>
		/// Bank Identifier Code Financial Institution (BICFI)
		/// </summary>
		public string BicFi { get; }

		/// <summary>
		/// City Name
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// URL to logotype
		/// </summary>
		public string LogoUrl { get; }
	}
}
