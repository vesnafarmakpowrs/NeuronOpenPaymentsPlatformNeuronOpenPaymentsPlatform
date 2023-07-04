namespace TAG.Networking.OpenPaymentsPlatform
{
	/// <summary>
	/// Possible Authentication Type values.
	/// </summary>
	public enum AuthenticationType
	{
		/// <summary>
		/// SMS
		/// </summary>
		SMS_OTP,

		/// <summary>
		/// Chip
		/// </summary>
		CHIP_OTP,

		/// <summary>
		/// Photo
		/// </summary>
		PHOTO_OTP,

		/// <summary>
		/// Push to app
		/// </summary>
		PUSH_OTP
	}

	/// <summary>
	/// Reference to an authentication method.
	/// </summary>
	public class AuthenticationMethod
	{
		/// <summary>
		/// Reference to an authentication method.
		/// </summary>
		/// <param name="MethodId">Authentication Type</param>
		/// <param name="Type">Method ID</param>
		/// <param name="Name">Name of method</param>
		public AuthenticationMethod(AuthenticationType Type, string MethodId, string Name)
		{
			this.Type = Type;
			this.MethodId = MethodId;
			this.Name = Name;
		}

		/// <summary>
		/// Authentication Type
		/// </summary>
		public AuthenticationType Type { get; }

		/// <summary>
		/// Method ID
		/// </summary>
		public string MethodId { get; }

		/// <summary>
		/// Name
		/// </summary>
		public string Name { get; }
	}
}
