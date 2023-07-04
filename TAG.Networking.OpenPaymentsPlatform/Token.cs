using System;

namespace TAG.Networking.OpenPaymentsPlatform
{
	/// <summary>
	/// Contains information about a token.
	/// </summary>
	public class Token
	{
		/// <summary>
		/// Contains information about a token.
		/// </summary>
		/// <param name="Token">Bearer token.</param>
		/// <param name="Expires">When token expires.</param>
		public Token(string Token, DateTime Expires)
		{
			this.BearerToken = Token;
			this.Expires = Expires;
		}

		/// <summary>
		/// Bearer Token
		/// </summary>
		public string BearerToken { get; }

		/// <summary>
		/// When token expires
		/// </summary>
		public DateTime Expires { get; }
	}
}
