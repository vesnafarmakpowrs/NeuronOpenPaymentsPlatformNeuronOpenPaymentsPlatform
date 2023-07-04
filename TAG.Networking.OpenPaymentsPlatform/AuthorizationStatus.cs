namespace TAG.Networking.OpenPaymentsPlatform
{
	/// <summary>
	/// Contains information about an authorization process.
	/// </summary>
	public class AuthorizationStatus
	{
		/// <summary>
		/// Contains information about an authorization process.
		/// </summary>
		/// <param name="Status">Status</param>
		/// <param name="ChallengeData">Challenge data to display to the end-user.</param>
		/// <param name="Messages">Any (error) messages.</param>
		public AuthorizationStatus(AuthorizationStatusValue Status,
			ChallengeData ChallengeData, TppMessage[] Messages)
		{
			this.Status = Status;
			this.ChallengeData = ChallengeData;
			this.Messages = Messages;
		}

		/// <summary>
		/// Status
		/// </summary>
		public AuthorizationStatusValue Status { get; }

		/// <summary>
		/// Challenge data to display to the end-user.
		/// </summary>
		public ChallengeData ChallengeData { get; }

		/// <summary>
		/// Any (error) messages
		/// </summary>
		public TppMessage[] Messages { get; }
	}
}
