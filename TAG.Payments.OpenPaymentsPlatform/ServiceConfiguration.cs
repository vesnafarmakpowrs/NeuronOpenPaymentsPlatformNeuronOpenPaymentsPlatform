using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using TAG.Networking.OpenPaymentsPlatform;
using Waher.Events;
using Waher.Runtime.Settings;
using Waher.Script.Units;

namespace TAG.Payments.OpenPaymentsPlatform
{
    /// <summary>
    /// Contains the service configuration.
    /// </summary>
    public class ServiceConfiguration
    {
        private static ServiceConfiguration current = null;

        /// <summary>
        /// Contains the service configuration.
        /// </summary>
        public ServiceConfiguration()
        {
        }

        /// <summary>
        /// Client ID
        /// </summary>
        public string ClientID
        {
            get;
            private set;
        }

        /// <summary>
        /// Client secret
        /// </summary>
        public string ClientSecret
        {
            get;
            private set;
        }

        /// <summary>
        /// Operation mode
        /// </summary>
        public OperationMode OperationMode
        {
            get;
            private set;
        }

        /// <summary>
        /// Preferred authorization flow.
        /// </summary>
        public AuthorizationFlow AuthorizationFlow
        {
            get;
            private set;
        }

        /// <summary>
        /// Service certificate
        /// </summary>
        public X509Certificate2 Certificate
        {
            get;
            private set;
        }

        /// <summary>
        /// Associated Bank Account
        /// </summary>
        public string NeuronBankAccountIban
        {
            get;
            private set;
        }

        /// <summary>
        /// Name for associated Bank Account
        /// </summary>
        public string NeuronBankAccountName
        {
            get;
            private set;
        }

        /// <summary>
        /// Associated Bank or financial institution responsible for the back account.
        /// </summary>
        public string NeuronBankBic
        {
            get;
            private set;
        }

        /// <summary>
        /// Personal ID of person that will authenticate payment requests.
        /// </summary>
        public string PersonalID
        {
            get;
            private set;
        }

        /// <summary>
        /// Organization ID of organization that owns the account (if a corporate account).
        /// </summary>
        public string OrganizationID
        {
            get;
            private set;
        }

        /// <summary>
        /// Number of seconds between status polling requests.
        /// </summary>
        public int PollingIntervalSeconds
        {
            get;
            private set;
        }

        /// <summary>
        /// Number of minutes to wait before failing attempt.
        /// </summary>
        public int TimeoutMinutes
        {
            get;
            private set;
        }

        /// <summary>
        /// Purpose for accessing service.
        /// </summary>
        public ServicePurpose Purpose => string.IsNullOrEmpty(this.OrganizationID) ? ServicePurpose.Private : ServicePurpose.Corporate;

        /// <summary>
        /// If configuration is well-defined.
        /// </summary>
        public bool IsWellDefined
        {
            get
            {
                return
                    !string.IsNullOrEmpty(this.ClientID) &&
                    !string.IsNullOrEmpty(this.ClientSecret) &&
                    !string.IsNullOrEmpty(this.NeuronBankAccountIban) &&
                    this.NeuronBankAccountIban.Length > 2 &&
                    !string.IsNullOrEmpty(this.NeuronBankAccountName) &&
                    !string.IsNullOrEmpty(this.NeuronBankBic) &&
                    !string.IsNullOrEmpty(this.PersonalID) &&
                    (!(this.Certificate is null) || this.OperationMode == OperationMode.Sandbox) &&
                    this.PollingIntervalSeconds > 0 &&
                    this.TimeoutMinutes > 0;
            }
        }

        /// <summary>
        /// Loads the purpose used by the current configuratio.
        /// </summary>
        /// <returns>Purpose</returns>
        public static async Task<ServicePurpose> LoadPurpose()
        {
            string Prefix = OpenPaymentsPlatformServiceProvider.ServiceId;
            string OrganizationID = await RuntimeSettings.GetAsync(Prefix + ".OrganizationID", string.Empty);

            return string.IsNullOrEmpty(OrganizationID) ? ServicePurpose.Private : ServicePurpose.Corporate;
        }

        /// <summary>
        /// Loads configuration settings.
        /// </summary>
        /// <returns>Configuration settings.</returns>
        public static async Task<ServiceConfiguration> Load()
        {
            ServiceConfiguration Result = new ServiceConfiguration();
            string Prefix = OpenPaymentsPlatformServiceProvider.ServiceId;

            Result.ClientID = await RuntimeSettings.GetAsync(Prefix + ".ClientID", string.Empty);
            Result.ClientSecret = await RuntimeSettings.GetAsync(Prefix + ".ClientSecret", string.Empty);
            Result.OperationMode = (OperationMode)await RuntimeSettings.GetAsync(Prefix + ".Mode", OperationMode.Sandbox);
            Result.AuthorizationFlow = (AuthorizationFlow)await RuntimeSettings.GetAsync(Prefix + ".Flow", AuthorizationFlow.Decoupled);
            Result.NeuronBankAccountIban = await RuntimeSettings.GetAsync(Prefix + ".Account", string.Empty);
            Result.NeuronBankAccountName = await RuntimeSettings.GetAsync(Prefix + ".AccountName", string.Empty);
            Result.NeuronBankBic = await RuntimeSettings.GetAsync(Prefix + ".AccountBank", string.Empty);
            Result.PersonalID = await RuntimeSettings.GetAsync(Prefix + ".PersonalID", string.Empty);
            Result.OrganizationID = await RuntimeSettings.GetAsync(Prefix + ".OrganizationID", string.Empty);
            Result.PollingIntervalSeconds = (int)await RuntimeSettings.GetAsync(Prefix + ".PollingIntervalSeconds", 0.0) * 1000;
            Result.TimeoutMinutes = (int)await RuntimeSettings.GetAsync(Prefix + ".TimeoutMinutes", 0.0);

            string CertBase64 = await RuntimeSettings.GetAsync(Prefix + ".Certificate", string.Empty);
            string CertPassword = await RuntimeSettings.GetAsync(Prefix + ".CertificatePassword", string.Empty);

            if (string.IsNullOrEmpty(CertBase64))
            {
                Result.Certificate = null;
            }
            else
            {
                try
                {
                    Result.Certificate = new X509Certificate2(Convert.FromBase64String(CertBase64), CertPassword);
                }
                catch (Exception ex)
                {
                    Log.Critical(ex);
                }
            }

            return Result;
        }

        /// <summary>
        /// Gets the current configuration.
        /// </summary>
        /// <returns>Configuration</returns>
        public static async Task<ServiceConfiguration> GetCurrent()
        {
            if (current is null)
                current = await Load();

            return current;
        }

        /// <summary>
        /// Invalidates the current configuration, triggering a reload of the
        /// configuration for the next operation.
        /// </summary>
        public static void InvalidateCurrent()
        {
            current = null;
            OpenPaymentsPlatformServiceProvider.InvalidateServices();
        }
    }
}
