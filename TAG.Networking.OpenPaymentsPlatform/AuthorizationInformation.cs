namespace TAG.Networking.OpenPaymentsPlatform
{
	/// <summary>
	/// Type of authorization flow to request
	/// </summary>
	public enum AuthorizationFlow
	{
		/// <summary>
		/// Via web page and redirect.
		/// </summary>
		Redirect,

		/// <summary>
		/// Decoupled
		/// </summary>
		Decoupled
	}

	/// <summary>
	/// Possible consent authorization status values
	/// </summary>
	public enum AuthorizationStatusValue
	{
		/// <summary>
		/// Received
		/// </summary>
		received,

		/// <summary>
		/// Authentication started
		/// </summary>
		authenticationStarted,

		/// <summary>
		/// Debtor needs to authorize payment to Creditor
		/// </summary>
		authoriseCreditorAccountStarted,

		/// <summary>
		/// PSU Identified
		/// </summary>
		psuIdentified,

		/// <summary>
		/// PSU Authenticated
		/// </summary>
		psuAuthenticated,

		/// <summary>
		/// SCA Method Selected
		/// </summary>
		scaMethodSelected,

		/// <summary>
		/// Started
		/// </summary>
		started,

		/// <summary>
		/// Finalized
		/// </summary>
		finalised,

		/// <summary>
		/// Failed
		/// </summary>
		failed,

		/// <summary>
		/// Exempted
		/// </summary>
		exempted,
	}

	/// <summary>
	/// Contains the status of a consent authorization request.
	/// </summary>
	public class AuthorizationInformation : ObjectWithLinks
	{
		/// <summary>
		/// Contains the status of a consent authorization request.
		/// </summary>
		/// <param name="AuthorizationId">Authorization ID</param>
		/// <param name="Status">Status</param>
		/// <param name="AuthenticationMethods">Authentication Methods</param>
		/// <param name="Links">Links</param>
		public AuthorizationInformation(string AuthorizationId, AuthorizationStatusValue Status,
			AuthenticationMethod[] AuthenticationMethods, Links Links)
			: base(Links)
		{
			this.AuthorizationID = AuthorizationId;
			this.Status = Status;
			this.AuthenticationMethods = AuthenticationMethods;
		}

		/// <summary>
		/// Authorization ID
		/// </summary>
		public string AuthorizationID { get; }

		/// <summary>
		/// Status
		/// </summary>
		public AuthorizationStatusValue Status { get; }

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
			return GetAuthenticationMethod(MethodId, this.AuthenticationMethods);
		}

		internal static AuthenticationMethod GetAuthenticationMethod(string MethodId,
			params AuthenticationMethod[] Methods)
		{
			if (Methods is null)
				return null;

			foreach (AuthenticationMethod Method in Methods)
			{
				if (Method.MethodId == MethodId)
					return Method;
			}

			return null;
		}
	}
}
