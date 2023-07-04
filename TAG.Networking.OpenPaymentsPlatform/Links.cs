using System.Collections;
using System.Collections.Generic;

namespace TAG.Networking.OpenPaymentsPlatform
{
	/// <summary>
	/// Available links in a response
	/// </summary>
	public class Links : IEnumerable<KeyValuePair<string, string>>
	{
		private readonly Dictionary<string, string> links;

		/// <summary>
		/// Available links in a response
		/// </summary>
		/// <param name="Object">Object of links</param>
		public Links(Dictionary<string, object> Object)
		{
			this.links = new Dictionary<string, string>();

			foreach (KeyValuePair<string, object> Link in Object)
			{
				if (Link.Value is Dictionary<string, object> Obj &&
					Obj.TryGetValue("href", out object Obj2))
				{
					this.links[Link.Key] = Obj2?.ToString() ?? string.Empty;
				}
			}
		}

		/// <summary>
		/// Returns a link, given its key.
		/// </summary>
		/// <param name="Key">Key of link</param>
		/// <returns>Link, if found, null otherwise.</returns>
		public string this[string Key]
		{
			get
			{
				if (this.links.TryGetValue(Key, out string Link))
					return Link;
				else
					return null;
			}
		}

		/// <summary>
		/// In case of an SCA Redirect Approach, the ASPSP is transmitting the link 
		/// to which to redirect the PSU browser.
		/// </summary>
		public string ScaRedirect => this["scaRedirect"];

		/// <summary>
		/// In case of a SCA OAuth2 Approach, the ASPSP is transmitting the URI where 
		/// the configuration of the Authorisation Server can be retrieved. The 
		/// configuration follows the OAuth 2.0 Authorisation Server Metadata 
		/// specification.
		/// </summary>
		public string ScaOAuth => this["scaOAuth"];

		/// <summary>
		/// In case, where an explicit start of the transaction authorisation is 
		/// needed, but no more data needs to be updated (no authentication method 
		/// to be selected, no PSU identification nor PSU authentication data to be 
		/// uploaded).
		/// </summary>
		public string StartAuthorisation => this["startAuthorisation"];

		/// <summary>
		/// The link to the authorisation or cancellation authorisation sub-resource,
		/// where PSU identification data needs to be uploaded.
		/// </summary>
		public string UpdatePsuIdentification => this["updatePsuIdentification"];

		/// <summary>
		/// The link to the authorisation end-point, where the authorisation 
		/// sub-resource has to be generated while uploading the PSU identification 
		/// data.
		/// </summary>
		public string StartAuthorisationWithPsuIdentification => this["startAuthorisationWithPsuIdentification"];

		/// <summary>
		/// The link to the authorisation or cancellation authorisation sub-resource,
		/// where PSU authentication data needs to be uploaded.
		/// </summary>
		public string StartAuthorisationWithPsuAuthentication => this["startAuthorisationWithPsuAuthentication"];

		/// <summary>
		/// Same as startAuthorisactionWithPsuAuthentication where the 
		/// authentication data need to be encrypted on application layer in 
		/// uploading.
		/// </summary>
		public string StartAuthorisationWithEncryptedPsuAuthentication => this["startAuthorisationWithEncryptedPsuAuthentication"];

		/// <summary>
		/// The link to the authorisation end-point, where the authorisation 
		/// sub-resource has to be generated while selecting the authentication 
		/// method. This link is contained under exactly the same conditions as the 
		/// data element 'scaMethods'
		/// </summary>
		public string StartAuthorisationWithAuthenticationMethodSelection => this["startAuthorisationWithAuthenticationMethodSelection"];

		/// <summary>
		/// The link to the authorisation end-point, where the authorisation 
		/// sub-resource has to be generated while authorising the transaction 
		/// e.g. by uploading an OTP received by SMS.
		/// </summary>
		public string StartAuthorisationWithTransactionAuthorisation => this["startAuthorisationWithTransactionAuthorisation"];

		/// <summary>
		/// The link to the authorisation or cancellation authorisation sub-resource, 
		/// where the selected authentication method needs to be uploaded. This link 
		/// is contained under exactly the same conditions as the data element 
		/// 'scaMethods'.
		/// </summary>
		public string SelectAuthenticationMethod => this["selectAuthenticationMethod"];

		/// <summary>
		/// The link to the authorisation or cancellation authorisation sub-resource, 
		/// where the authorisation data has to be uploaded, e.g. the TOP received by 
		/// SMS.
		/// </summary>
		public string AuthoriseTransaction => this["authoriseTransaction"];

		/// <summary>
		/// The link to retrieve the scaStatus of the corresponding authorisation 
		/// sub-resource. 
		/// </summary>
		public string ScaStatus => this["scaStatus"];

		/// <summary>
		/// The link to the payment initiation resource created by this request. 
		/// This link can be used to retrieve the resource data.
		/// </summary>
		public string Self => this["self"];

		/// <summary>
		/// The link to retrieve the transaction status of the payment initiation.
		/// </summary>
		public string Status => this["status"];

		/// <summary>
		/// Gets an enumerator of reported links.
		/// </summary>
		/// <returns>Enumerator of reported links.</returns>
		public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
		{
			return this.links.GetEnumerator();
		}

		/// <summary>
		/// Gets an enumerator of reported links.
		/// </summary>
		/// <returns>Enumerator of reported links.</returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.links.GetEnumerator();
		}
	}
}
