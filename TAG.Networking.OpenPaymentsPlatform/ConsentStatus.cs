namespace TAG.Networking.OpenPaymentsPlatform
{
	/// <summary>
	/// Possible consent status values.
	/// </summary>
	public enum ConsentStatusValue
	{
		/// <summary>
		/// Recejved
		/// </summary>
		received,

		/// <summary>
		/// Rejected
		/// </summary>
		rejected,

		/// <summary>
		/// Valid
		/// </summary>
		valid,

		/// <summary>
		/// Revoked by PSU
		/// </summary>
		revokedByPsu,

		/// <summary>
		/// Expired
		/// </summary>
		expired,

		/// <summary>
		/// Terminated by TPP
		/// </summary>
		terminatedByTpp
	};

	/// <summary>
	/// Contains the status of a consent request.
	/// </summary>
	public class ConsentStatus : ObjectWithLinks
	{
		/// <summary>
		/// Contains the status of a consent request.
		/// </summary>
		/// <param name="ConsentID">Consent ID</param>
		/// <param name="Status">Status</param>
		/// <param name="AuthenticationMethods">Authentication Methods</param>
		/// <param name="Links">Links</param>
		public ConsentStatus(string ConsentID, ConsentStatusValue Status,
			AuthenticationMethod[] AuthenticationMethods, Links Links)
			: base(Links)
		{
			this.ConsentID = ConsentID;
			this.Status = Status;
			this.AuthenticationMethods = AuthenticationMethods;
		}

		/// <summary>
		/// Consent ID
		/// </summary>
		public string ConsentID { get; }

		/// <summary>
		/// Status
		/// </summary>
		public ConsentStatusValue Status { get; }

		/// <summary>
		/// Authentication Methods
		/// </summary>
		public AuthenticationMethod[] AuthenticationMethods { get; }

		/// <summary>
		/// Gets the authentication method given its ID.
		/// </summary>
		/// <param name="MethodId">Authentication method ID</param>
		/// <returns>Authentication methid, if found, null otherwise.</returns>
		public AuthenticationMethod GetAuthenticationMethod(string MethodId)
		{
			return AuthorizationInformation.GetAuthenticationMethod(MethodId, this.AuthenticationMethods);
		}
	}
}
