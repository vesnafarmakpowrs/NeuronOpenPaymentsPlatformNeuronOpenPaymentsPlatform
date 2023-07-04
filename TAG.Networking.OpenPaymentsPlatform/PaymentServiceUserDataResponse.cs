namespace TAG.Networking.OpenPaymentsPlatform
{
	/// <summary>
	/// Payment Service User Data response.
	/// </summary>
	public class PaymentServiceUserDataResponse : ObjectWithLinks
	{
		/// <summary>
		/// Payment Service User Data response.
		/// </summary>
		/// <param name="Message">Message to present to user.</param>
		/// <param name="Status">Authorization status</param>
		/// <param name="ChosenMethod">Chosen authentication method</param>
		/// <param name="Links">Collection of links.</param>
		/// <param name="ChallengeData">Challenge data to display to the end user.</param>
		/// <param name="Messages">Any messages returned.</param>
		public PaymentServiceUserDataResponse(string Message, AuthorizationStatusValue Status,
			AuthenticationMethod ChosenMethod, Links Links, ChallengeData ChallengeData,
			TppMessage[] Messages)
			: base(Links)
		{
			this.Message = Message;
			this.Status = Status;
			this.ChosenMethod = ChosenMethod;
			this.ChallengeData = ChallengeData;
			this.Messages = Messages;
		}

		/// <summary>
		/// Message to present to user.
		/// </summary>
		public string Message { get; }

		/// <summary>
		/// Authorization status
		/// </summary>
		public AuthorizationStatusValue Status { get; }

		/// <summary>
		/// Chosen authentication method. (May be null.)
		/// </summary>
		public AuthenticationMethod ChosenMethod { get; }

		/// <summary>
		/// Challenge data that end-user needs to use to authorize access.
		/// </summary>
		public ChallengeData ChallengeData { get; }

		/// <summary>
		/// Any (error) messages.
		/// </summary>
		public TppMessage[] Messages { get; }
	}
}