using System;
using System.Collections.Generic;

namespace TAG.Networking.OpenPaymentsPlatform
{
	public class TppMessage
	{
		private TppMessage()
		{
		}

		public static bool TryParse(Dictionary<string, object> Response, out TppMessage[] Messages)
		{
			if (!Response.TryGetValue("tppMessages", out object Obj) || !(Obj is Array A))
			{
				Messages = null;
				return false;
			}

			List<TppMessage> Result = new List<TppMessage>();

			foreach (object Item in A)
			{
				if (Item is Dictionary<string, object> ErrorMessage &&
					ErrorMessage.TryGetValue("category", out Obj) && Obj is string Category && Category == "ERROR" &&
					ErrorMessage.TryGetValue("text", out Obj) && Obj is string Text)
				{
					Result.Add(new TppMessage()
					{
						Category = Category,
						Text = Text
					});
				}
			}

			Messages = Result.ToArray();
			return true;
		}

		/// <summary>
		/// Error category
		/// </summary>
		public string Category
		{
			get;
			private set;
		}

		/// <summary>
		/// Error Message
		/// </summary>
		public string Text
		{
			get;
			private set;
		}
	}
}
