using System;
using System.Collections.Generic;

namespace TAG.Networking.OpenPaymentsPlatform
{
	public class ChallengeData
	{
		private string bankIdUrl;

		private ChallengeData()
		{
		}

		/// <summary>
		/// Tries to parse challenge data from a response.
		/// </summary>
		/// <param name="ChallengeData">Challenge data object.</param>
		/// <param name="Parsed">Parsed result</param>
		/// <returns>If able to parse challenge data.</returns>
		public static bool TryParse(Dictionary<string, object> ChallengeData, out ChallengeData Parsed)
		{
			bool Result = false;

			Parsed = new ChallengeData();

			if (!(ChallengeData is null))
			{
				if (ChallengeData.TryGetValue("data", out object Obj) &&
					Obj is Array Data &&
					Data.Length > 0 &&
					Data.GetValue(0) is string AutoStartToken)
				{
					Parsed.AutoStartToken = AutoStartToken;
					Result = true;
				}

				if (ChallengeData.TryGetValue("image", out Obj) && Obj is string Image)
				{
					Parsed.ImageUrl = Image;
					Result = true;
				}
			}

			return Result;
		}

		/// <summary>
		/// Auto-start-token, if available. Can be used to start Bank-ID app
		/// on device, by opening an URL on the phone, with the following syntax:
		/// 
		/// https://app.bankid.com/?autostarttoken=[AutoStartToken]&redirect=null
		/// </summary>
		public string AutoStartToken
		{
			get;
			private set;
		}

		/// <summary>
		/// URL to image to display
		/// </summary>
		public string ImageUrl
		{
			get;
			private set;
		}

		/// <summary>
		/// URL for opening Bank-ID app on device, or null if not available.
		/// </summary>
		public string BankIdURL
		{
			get
			{
				if (!string.IsNullOrEmpty(this.bankIdUrl))
					return this.bankIdUrl;

				if (string.IsNullOrEmpty(this.AutoStartToken))
					return null;

				this.bankIdUrl = CreateBankIdUrl(this.AutoStartToken);

				return this.bankIdUrl;
			}
		}

		/// <summary>
		/// Creates a Bank-ID URL from a challenge (auto-start token).
		/// </summary>
		/// <param name="AutoStartToken">Auto-start token (challenge data)</param>
		/// <returns>URL</returns>
		public static string CreateBankIdUrl(string AutoStartToken)
		{
			return "https://app.bankid.com/?autostarttoken=" + AutoStartToken + "&redirect=null";
		}
	}
}
