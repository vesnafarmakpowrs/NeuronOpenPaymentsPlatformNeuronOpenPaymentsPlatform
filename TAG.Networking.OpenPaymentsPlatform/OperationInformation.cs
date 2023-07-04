using System;
using System.Collections.Generic;
using System.Net;
using Waher.Content;

namespace TAG.Networking.OpenPaymentsPlatform
{
	/// <summary>
	/// Contains information about an operation
	/// </summary>
	public class OperationInformation
	{
		/// <summary>
		/// Contains information about an operation
		/// </summary>
		/// <param name="UserIpAddress">IP Address of user.</param>
		/// <param name="UserAgent">User-agent string of client used..</param>
		/// <param name="Flow">Authorization flow.</param>
		/// <param name="PersonalID">ID of person performing the authorization.</param>
		/// <param name="OrganizationID">Optional ID of organization owning the debtor account.</param>
		/// <param name="DebtorBank">Bank code (BIC) of bank or financial institution of the debtor (that payment is made from).</param>
		public OperationInformation(IPAddress UserIpAddress, string UserAgent, 
			AuthorizationFlow Flow, string PersonalID, string OrganizationID, string DebtorBank)
		{
			this.UserIpAddress = UserIpAddress;
			this.UserAgent = UserAgent;
			this.ServiceProvider = DebtorBank;
			this.Flow = Flow;
			this.PersonalID = PersonalID;
			this.OrganizationID = OrganizationID;

			List<KeyValuePair<string, string>> Headers = new List<KeyValuePair<string, string>>()
			{
				new KeyValuePair<string, string>("PSU-IP-Address", this.UserIpAddress.ToString()),
				new KeyValuePair<string, string>("PSU-User-Agent", this.UserAgent),
				new KeyValuePair<string, string>("X-BicFi", this.ServiceProvider),
				new KeyValuePair<string, string>("TPP-Redirect-Preferred", CommonTypes.Encode(this.Flow == AuthorizationFlow.Redirect))
			};

			if (!string.IsNullOrEmpty(this.PersonalID))
				Headers.Add(new KeyValuePair<string, string>("PSU-ID", this.PersonalID));

			if (!string.IsNullOrEmpty(this.OrganizationID))
				Headers.Add(new KeyValuePair<string, string>("PSU-Corporate-ID", this.OrganizationID));

			this.Headers = Headers.ToArray();
		}

		/// <summary>
		/// User IP Address
		/// </summary>
		public IPAddress UserIpAddress { get; }

		/// <summary>
		/// User Agwent
		/// </summary>
		public string UserAgent { get; }

		/// <summary>
		/// Service Provider
		/// </summary>
		public string ServiceProvider { get; }

		/// <summary>
		/// Authentication Flow
		/// </summary>
		public AuthorizationFlow Flow { get; }

		/// <summary>
		/// Personal ID (or number)
		/// </summary>
		public string PersonalID { get; }

		/// <summary>
		/// Organization ID (or number)
		/// </summary>
		public string OrganizationID { get; }

		/// <summary>
		/// Headers to include in requests
		/// </summary>
		public KeyValuePair<string, string>[] Headers { get; }

		/// <summary>
		/// Appends callback headers (if provided).
		/// </summary>
		/// <param name="CallbackUrlOk">Optional callback URL, if OK.</param>
		/// <param name="CallbackUrlFail">Optional callback URL, if failing.</param>
		/// <returns>Headers</returns>
		public KeyValuePair<string, string>[] AppendCallbackHeaders(string CallbackUrlOk, string CallbackUrlFail)
		{
			if (string.IsNullOrEmpty(CallbackUrlOk) && string.IsNullOrEmpty(CallbackUrlFail))
				return this.Headers;

			List<KeyValuePair<string, string>> ToAppend = new List<KeyValuePair<string, string>>();

			if (!string.IsNullOrEmpty(CallbackUrlOk))
				ToAppend.Add(new KeyValuePair<string, string>("TPP-Redirect-URI", CallbackUrlOk));

			if (!string.IsNullOrEmpty(CallbackUrlFail))
				ToAppend.Add(new KeyValuePair<string, string>("TPP-Nok-Redirect-URI", CallbackUrlFail));

			return this.AppendHeaders(ToAppend.ToArray());
		}

		/// <summary>
		/// Gets an array of headers appending custom headers to the headers defined
		/// in the operation.
		/// </summary>
		/// <param name="AdditionalHeaders">Headers to append.</param>
		/// <returns>Appended set of headers.</returns>
		public KeyValuePair<string, string>[] AppendHeaders(params KeyValuePair<string, string>[] AdditionalHeaders)
		{
			int d = AdditionalHeaders.Length;
			if (d == 0)
				return this.Headers;

			int c = this.Headers.Length;

			KeyValuePair<string, string>[] Result = new KeyValuePair<string, string>[c + d];

			Array.Copy(this.Headers, 0, Result, 0, c);
			Array.Copy(AdditionalHeaders, 0, Result, c, d);

			return Result;
		}
	}
}
