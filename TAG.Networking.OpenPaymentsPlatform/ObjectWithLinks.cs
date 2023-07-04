namespace TAG.Networking.OpenPaymentsPlatform
{
	/// <summary>
	/// Abstract base class of objects with links.
	/// </summary>
	public abstract class ObjectWithLinks
	{
		/// <summary>
		/// Abstract base class of objects with links.
		/// </summary>
		/// <param name="Links">Links</param>
		public ObjectWithLinks(Links Links)
		{
			this.Links = Links;
		}

		/// <summary>
		/// Resource links
		/// </summary>
		public Links Links { get; }
	}
}
