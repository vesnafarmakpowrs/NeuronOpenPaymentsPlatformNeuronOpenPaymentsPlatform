using Paiwise;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TAG.Networking.OpenPaymentsPlatform;
using TAG.Payments.OpenPaymentsPlatform.Api;
using Waher.Events;
using Waher.IoTGateway;
using Waher.Networking.HTTP;
using Waher.Networking.Sniffers;
using Waher.Persistence;
using Waher.Runtime.Inventory;

namespace TAG.Payments.OpenPaymentsPlatform
{
	/// <summary>
	/// Modes of operation
	/// </summary>d
	public enum OperationMode
	{
		/// <summary>
		/// Executes against the sandbox environment
		/// </summary>
		Sandbox,

		/// <summary>
		/// Executes against the production environment
		/// </summary>
		Production
	}

	/// <summary>
	/// Open Payments Platform service provider
	/// </summary>
	public class OpenPaymentsPlatformServiceProvider : IConfigurableModule, IBuyEDalerServiceProvider, ISellEDalerServiceProvider
	{
		/// <summary>
		/// Reference to client sniffer for Production communication.
		/// </summary>
		internal static XmlFileSniffer SnifferProduction = null;

		/// <summary>
		/// Reference to client sniffer for Sandbox communication.
		/// </summary>
		internal static XmlFileSniffer SnifferSandbox = null;

		/// <summary>
		/// Sniffable object that can be sniffed on dynamically.
		/// </summary>
		private static readonly Sniffable sniffable = new Sniffable();

		/// <summary>
		/// Sniffer proxy, forwarding sniffer events to <see cref="sniffable"/>.
		/// </summary>
		private static readonly SnifferProxy snifferProxy = new SnifferProxy(sniffable);

		/// <summary>
		/// Users are required to have this privilege in order to show and sign payments using this service.
		/// </summary>
		internal const string RequiredPrivilege = "Admin.Payments.Paiwise.OpenPaymentsPlatform";

		/// <summary>
		/// Open Payments Platform service provider
		/// </summary>
		public OpenPaymentsPlatformServiceProvider()
		{
		}

		#region IModule

		private readonly static SignPayments signPayments = new SignPayments();
		private readonly static ReturnPayments returnPayments = new ReturnPayments();
		private readonly static RetryPayments retryPayments = new RetryPayments();

		/// <summary>
		/// Starts the service.
		/// </summary>
		public Task Start()
		{
			Gateway.HttpServer.Register(signPayments);
			Gateway.HttpServer.Register(returnPayments);
			Gateway.HttpServer.Register(retryPayments);

			return Task.CompletedTask;
		}

		/// <summary>
		/// Stops the service.
		/// </summary>
		public Task Stop()
		{
			Gateway.HttpServer.Unregister(signPayments);
			Gateway.HttpServer.Unregister(returnPayments);
			Gateway.HttpServer.Unregister(retryPayments);

			SnifferProduction?.Dispose();
			SnifferProduction = null;

			SnifferSandbox?.Dispose();
			SnifferSandbox = null;

			return Task.CompletedTask;
		}

		#endregion

		#region IConfigurableModule interface

		/// <summary>
		/// Gets an array of configurable pages for the module.
		/// </summary>
		/// <returns>Configurable pages</returns>
		public Task<IConfigurablePage[]> GetConfigurablePages()
		{
			return Task.FromResult(new IConfigurablePage[]
			{
				new ConfigurablePage("Open Payments Platform", "/OpenPaymentsPlatform/Settings.md", "Admin.Payments.Paiwise.OpenPaymentsPlatform")
			});
		}

		#endregion

		#region IServiceProvider

		/// <summary>
		/// ID of service
		/// </summary>
		public string Id => ServiceId;

		/// <summary>
		/// ID of service.
		/// </summary>
		public static string ServiceId = typeof(OpenPaymentsPlatformServiceProvider).Namespace;

		/// <summary>
		/// Name of service
		/// </summary>
		public string Name => "Open Payments Platform";

		/// <summary>
		/// Icon URL
		/// </summary>
		public string IconUrl => "https://docs.openpayments.io/img/header_logo.svg";

		/// <summary>
		/// Width of icon, in pixels.
		/// </summary>
		public int IconWidth => 181;

		/// <summary>
		/// Height of icon, in pixels
		/// </summary>
		public int IconHeight => 150;

		#endregion

		#region Open Payments Platform interface

		private readonly static Dictionary<CaseInsensitiveString, KeyValuePair<OpenPaymentsPlatformService[], DateTime>> productionServicesByCountry = new Dictionary<CaseInsensitiveString, KeyValuePair<OpenPaymentsPlatformService[], DateTime>>();
		private readonly static Dictionary<CaseInsensitiveString, KeyValuePair<OpenPaymentsPlatformService[], DateTime>> sandboxServicesByCountry = new Dictionary<CaseInsensitiveString, KeyValuePair<OpenPaymentsPlatformService[], DateTime>>();
		private readonly static object synchObj = new object();

		internal static void InvalidateServices()
		{
			lock (synchObj)
			{
				productionServicesByCountry.Clear();
				sandboxServicesByCountry.Clear();
			}
		}

		internal static OpenPaymentsPlatformClient CreateClient(ServiceConfiguration Configuration)
		{
			return CreateClient(Configuration, Configuration.OperationMode);
		}

		internal static OpenPaymentsPlatformClient CreateClient(ServiceConfiguration Configuration, OperationMode Mode)
		{
			return CreateClient(Configuration, Mode, Configuration.Purpose);
		}

		internal static OpenPaymentsPlatformClient CreateClient(ServiceConfiguration Configuration, OperationMode Mode,
			ServicePurpose Purpose)
		{
			if (!Configuration.IsWellDefined)
				return null;

			switch (Mode)
			{
				case OperationMode.Production:
					if (SnifferProduction is null)
					{
						SnifferProduction = new XmlFileSniffer(Gateway.AppDataFolder + "OPP_Production" + Path.DirectorySeparatorChar +
							"Log %YEAR%-%MONTH%-%DAY%T%HOUR%.xml",
							Gateway.AppDataFolder + "Transforms" + Path.DirectorySeparatorChar + "SnifferXmlToHtml.xslt",
							7, BinaryPresentationMethod.Base64);
					}

					return OpenPaymentsPlatformClient.CreateProduction(Configuration.ClientID, Configuration.ClientSecret,
						Configuration.Certificate, Purpose, SnifferProduction, snifferProxy);

				case OperationMode.Sandbox:
					if (SnifferSandbox is null)
					{
						SnifferSandbox = new XmlFileSniffer(Gateway.AppDataFolder + "OPP_Sandbox" + Path.DirectorySeparatorChar +
							"Log %YEAR%-%MONTH%-%DAY%T%HOUR%.xml",
							Gateway.AppDataFolder + "Transforms" + Path.DirectorySeparatorChar + "SnifferXmlToHtml.xslt",
							7, BinaryPresentationMethod.Base64);
					}

					return OpenPaymentsPlatformClient.CreateSandbox(Configuration.ClientID, Configuration.ClientSecret,
						Purpose, SnifferSandbox, snifferProxy);

				default:
					return null;
			}
		}

		private async Task<OpenPaymentsPlatformService[]> GetServices(CaseInsensitiveString Country, ServicePurpose Purpose)
		{
			ServiceConfiguration Configuration = await ServiceConfiguration.GetCurrent();
			OperationMode Mode = Configuration.OperationMode;

			return await this.GetServices(Country, Configuration, Mode, Purpose);
		}

		private async Task<OpenPaymentsPlatformService[]> GetServices(CaseInsensitiveString Country, OperationMode Mode, ServicePurpose Purpose)
		{
			ServiceConfiguration Configuration = await ServiceConfiguration.GetCurrent();
			return await this.GetServices(Country, Configuration, Mode, Purpose);
		}

		private async Task<OpenPaymentsPlatformService[]> GetServices(CaseInsensitiveString Country, ServiceConfiguration Configuration,
			OperationMode Mode, ServicePurpose Purpose)
		{
			try
			{
				OpenPaymentsPlatformService[] Services = null;
				Dictionary<CaseInsensitiveString, KeyValuePair<OpenPaymentsPlatformService[], DateTime>> ServicesByCountry;

				if (Mode == OperationMode.Production)
					ServicesByCountry = productionServicesByCountry;
				else
					ServicesByCountry = sandboxServicesByCountry;

				lock (synchObj)
				{
					if (!ServicesByCountry.TryGetValue(Country, out KeyValuePair<OpenPaymentsPlatformService[], DateTime> Rec) ||
						DateTime.Now.Subtract(Rec.Value).TotalDays >= 1)
					{
						Services = null;
					}
					else
						Services = Rec.Key;
				}

				if (Services is null)
				{
					OpenPaymentsPlatformClient Client = CreateClient(Configuration, Mode, Purpose);
					if (Client is null)
						return new OpenPaymentsPlatformService[0];

					try
					{
						AspServiceProvider[] Services2 = await Client.GetAspServiceProviders(Country);
						int i, c = Services2.Length;

						Services = new OpenPaymentsPlatformService[c];

						for (i = 0; i < c; i++)
							Services[i] = new OpenPaymentsPlatformService(Country, Services2[i], Mode, this);

						lock (synchObj)
						{
							ServicesByCountry[Country] = new KeyValuePair<OpenPaymentsPlatformService[], DateTime>(Services, DateTime.Now);
						}
					}
					finally
					{
						Dispose(Client, Mode);
					}
				}

				return Services;
			}
			catch (Exception ex)
			{
				Log.Critical(ex, this.Id);
				return new OpenPaymentsPlatformService[0];
			}
		}

		internal static void Dispose(OpenPaymentsPlatformClient Client, OperationMode Mode)
		{
			Client?.Remove(Mode == OperationMode.Production ? SnifferProduction : SnifferSandbox);
			Client?.Remove(snifferProxy);
			Client?.Dispose();
		}

		/// <summary>
		/// Registers a web sniffer on the stripe client.
		/// </summary>
		/// <param name="SnifferId">Sniffer ID</param>
		/// <param name="Request">HTTP Request for sniffer page.</param>
		/// <param name="UserVariable">Name of user variable.</param>
		/// <param name="Privileges">Privileges required to view content.</param>
		/// <returns>Code to embed into page.</returns>
		public static string RegisterSniffer(string SnifferId, HttpRequest Request,
			string UserVariable, params string[] Privileges)
		{
			return Gateway.AddWebSniffer(SnifferId, Request, sniffable, UserVariable, Privileges);
		}

		#endregion

		#region IBuyEDalerServiceProvider

		/// <summary>
		/// Gets available payment services.
		/// </summary>
		/// <param name="Currency">Currency to use.</param>
		/// <param name="Country">Country where service is to be used.</param>
		/// <returns>Available payment services.</returns>
		public async Task<IBuyEDalerService[]> GetServicesForBuyingEDaler(CaseInsensitiveString Currency, CaseInsensitiveString Country)
		{
			return await this.GetServices(Currency, Country, ServicePurpose.Private);	// TODO: Corprorate, if organization ID
		}

		private async Task<OpenPaymentsPlatformService[]> GetServices(CaseInsensitiveString Currency, CaseInsensitiveString Country, ServicePurpose Purpose)
		{
			List<OpenPaymentsPlatformService> Result = new List<OpenPaymentsPlatformService>();
			OpenPaymentsPlatformService[] Services = await this.GetServices(Country, Purpose);

			foreach (OpenPaymentsPlatformService Service in Services)
			{
				if (Service.Supports(Currency) > Grade.NotAtAll)
					Result.Add(Service);

			}

			return Services.ToArray();
		}

		/// <summary>
		/// Gets a payment service.
		/// </summary>
		/// <param name="ServiceId">Service ID</param>
		/// <param name="Currency">Currency to use.</param>
		/// <param name="Country">Country where service is to be used.</param>
		/// <returns>Service, if found, null otherwise.</returns>
		public async Task<IBuyEDalerService> GetServiceForBuyingEDaler(string ServiceId, CaseInsensitiveString Currency, CaseInsensitiveString Country)
		{
			return await this.GetService(ServiceId, Country, ServicePurpose.Private);	// TODO: Corporate, if corporate ID
		}

		private async Task<OpenPaymentsPlatformService> GetService(string ServiceId, CaseInsensitiveString Country, ServicePurpose Purpose)
		{
			OperationMode Mode;

			if (ServiceId.StartsWith("Production."))
				Mode = OperationMode.Production;
			else if (ServiceId.StartsWith("Sandbox."))
				Mode = OperationMode.Sandbox;
			else
				return null;

			foreach (OpenPaymentsPlatformService Service in await this.GetServices(Country, Mode, Purpose))
			{
				if (Service.Id == ServiceId)
					return Service;
			}

			return null;
		}

		#endregion

		#region ISellEDalerServiceProvider

		/// <summary>
		/// Gets available payment services.
		/// </summary>
		/// <param name="Currency">Currency to use.</param>
		/// <param name="Country">Country where service is to be used.</param>
		/// <returns>Available payment services.</returns>
		public async Task<ISellEDalerService[]> GetServicesForSellingEDaler(CaseInsensitiveString Currency, CaseInsensitiveString Country)
		{
			ServicePurpose Purpose = await ServiceConfiguration.LoadPurpose();
			return await this.GetServices(Currency, Country, Purpose);
		}

		/// <summary>
		/// Gets a payment service.
		/// </summary>
		/// <param name="ServiceId">Service ID</param>
		/// <param name="Currency">Currency to use.</param>
		/// <param name="Country">Country where service is to be used.</param>
		/// <returns>Service, if found, null otherwise.</returns>
		public async Task<ISellEDalerService> GetServiceForSellingEDaler(string ServiceId, CaseInsensitiveString Currency, CaseInsensitiveString Country)
		{
			ServicePurpose Purpose = await ServiceConfiguration.LoadPurpose();
			return await this.GetService(ServiceId, Country, Purpose);
		}

		#endregion
	}
}
