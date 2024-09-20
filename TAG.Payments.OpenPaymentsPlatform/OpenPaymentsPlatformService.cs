using Paiwise;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TAG.Networking.OpenPaymentsPlatform;
using TAG.Payments.OpenPaymentsPlatform.Models;
using TAG.Payments.OpenPaymentsPlatform.Service;
using Waher.Content;
using Waher.Content.Markdown;
using Waher.Events;
using Waher.IoTGateway;
using Waher.Persistence;
using Waher.Persistence.Filters;
using Waher.Persistence.Serialization;
using Waher.Runtime.Inventory;
using Waher.Script;

namespace TAG.Payments.OpenPaymentsPlatform
{
    /// <summary>
    /// Open Payments Platform service
    /// </summary>
    public class OpenPaymentsPlatformService : IBuyEDalerService, ISellEDalerService
    {
        private static readonly Dictionary<string, string> buyTemplateIdsProduction = new Dictionary<string, string>()
        {
            { "ELLFSESS", "2d292ada-2f18-adc9-7812-95eeca0f22c0@legal.se.neuron.vaulter.nu" },
            { "ESSESESS", "2d292af9-2f18-adcf-7812-95eeca49d029@legal.se.neuron.vaulter.nu" },
            { "HANDSESS", "2d292b16-2f18-add4-7812-95eeca4ec76d@legal.se.neuron.vaulter.nu" },
            { "NDEASESS", "2d292b3f-2f18-add9-7812-95eeca4722d8@legal.se.neuron.vaulter.nu" },
            { "SWEDSESS", "2d292b59-2f18-addc-7812-95eeca94f93d@legal.se.neuron.vaulter.nu" },
            { "NDEAFIHH", string.Empty },
            { "DABASESX", string.Empty },
            { "DNBANOKK", string.Empty },
            { "NDEANOKK", string.Empty },
            { "NDEADKKK", string.Empty },
            { "OKOYFIHH", null },
            { "DNBASESX", null },
            { "DNBADEHX", null },
            { "DNBAGB2L", null }
        };

        private static readonly Dictionary<string, string> buyTemplateIdsSandbox = new Dictionary<string, string>()
        {
            { "ELLFSESS", "2d5ddde1-5695-03b9-101a-54cb2fe14cfa@legal.lab.se.neuron.vaulter.nu" },
            { "ESSESESS", "2d5dddf7-5695-03bb-101a-54cb2f5c8a64@legal.lab.se.neuron.vaulter.nu" },
            { "HANDSESS", "2d5dde07-5695-03bf-101a-54cb2fbedbce@legal.lab.se.neuron.vaulter.nu" },
            { "NDEASESS", "2d5dde1d-5695-03c1-101a-54cb2f5ff536@legal.lab.se.neuron.vaulter.nu" },
            { "SWEDSESS", "2d5dde2b-5695-03c3-101a-54cb2f6e4253@legal.lab.se.neuron.vaulter.nu" },
            { "NDEAFIHH", string.Empty },
            { "DABASESX", string.Empty },
            { "DNBANOKK", string.Empty },
            { "NDEANOKK", string.Empty },
            { "NDEADKKK", string.Empty },
            { "OKOYFIHH", null },
            { "DNBASESX", null },
            { "DNBADEHX", null },
            { "DNBAGB2L", null }
        };

        private static readonly Dictionary<string, string> buyTemplateIdsLocalDev = new Dictionary<string, string>()
        {
            { "ELLFSESS", "2be5a9bc-0022-b121-8029-227baed8c9db@legal." },
            { "ESSESESS", "2be5a9dc-0022-b127-8029-227bae11c604@legal." },
            { "HANDSESS", "2be5a9e9-0022-b12a-8029-227baeb0c983@legal." },
            { "NDEASESS", "2be5a9fa-0022-b12c-8029-227baee190ce@legal." },
            { "SWEDSESS", "2be5aa07-0022-b12e-8029-227baeb2d454@legal." },
            { "NDEAFIHH", string.Empty },
            { "DABASESX", string.Empty },
            { "DNBANOKK", string.Empty },
            { "NDEANOKK", string.Empty },
            { "NDEADKKK", string.Empty },
            { "OKOYFIHH", null },
            { "DNBASESX", null },
            { "DNBADEHX", null },
            { "DNBAGB2L", null }
        };

        private static readonly Dictionary<string, string> sellTemplateIdsProduction = new Dictionary<string, string>()
        {
            { "ELLFSESS", "2bebf475-151c-76dd-180b-8e272c2cdda8@legal.paiwise.tagroot.io" },
            { "ESSESESS", "2bebf49a-151c-76e0-180b-8e272c2d1ec1@legal.paiwise.tagroot.io" },
            { "HANDSESS", "2bebf4a7-151c-76e2-180b-8e272c8433da@legal.paiwise.tagroot.io" },
            { "NDEASESS", "2bebf4b7-151c-76e6-180b-8e272cb7e7d1@legal.paiwise.tagroot.io" },
            { "SWEDSESS", "2bebf4c2-151c-76e8-180b-8e272cae5fcd@legal.paiwise.tagroot.io" },
            { "NDEAFIHH", string.Empty },
            { "DABASESX", string.Empty },
            { "DNBANOKK", string.Empty },
            { "NDEANOKK", string.Empty },
            { "NDEADKKK", string.Empty },
            { "OKOYFIHH", null },
            { "DNBASESX", null },
            { "DNBADEHX", null },
            { "DNBAGB2L", null }
        };

        private static readonly Dictionary<string, string> sellTemplateIdsSandbox = new Dictionary<string, string>()
        {
            { "ELLFSESS", "2ba7143d-5c13-3570-8409-54d68df89117@legal.lab.tagroot.io" },
            { "ESSESESS", "2ba71449-5c13-3572-8409-54d68ddf81b4@legal.lab.tagroot.io" },
            { "HANDSESS", "2ba71451-5c13-3574-8409-54d68d7d414a@legal.lab.tagroot.io" },
            { "NDEASESS", "2ba7145a-5c13-3576-8409-54d68d243631@legal.lab.tagroot.io" },
            { "SWEDSESS", "2ba71464-5c13-3578-8409-54d68d68c6a3@legal.lab.tagroot.io" },
            { "NDEAFIHH", string.Empty },
            { "DABASESX", string.Empty },
            { "DNBANOKK", string.Empty },
            { "NDEANOKK", string.Empty },
            { "NDEADKKK", string.Empty },
            { "OKOYFIHH", null },
            { "DNBASESX", null },
            { "DNBADEHX", null },
            { "DNBAGB2L", null }
        };

        private static readonly Dictionary<string, string> sellTemplateIdsLocalDev = new Dictionary<string, string>()
        {
            { "ELLFSESS", "2be5aa2d-0022-b13c-8029-227bae3aeeed@legal." },
            { "ESSESESS", "2be5aa3a-0022-b147-8029-227bae306db1@legal." },
            { "HANDSESS", "2be5aa46-0022-b14e-8029-227bae4f3831@legal." },
            { "NDEASESS", "2be5aa52-0022-b150-8029-227bae7fdd4d@legal." },
            { "SWEDSESS", "2be5aa5f-0022-b152-8029-227baef01fa8@legal." },
            { "NDEAFIHH", string.Empty },
            { "DABASESX", string.Empty },
            { "DNBANOKK", string.Empty },
            { "NDEANOKK", string.Empty },
            { "NDEADKKK", string.Empty },
            { "OKOYFIHH", null },
            { "DNBASESX", null },
            { "DNBADEHX", null },
            { "DNBAGB2L", null }
        };

        private readonly OpenPaymentsPlatformServiceProvider provider;
        private readonly CaseInsensitiveString country;
        private readonly AspServiceProvider service;
        private readonly OperationMode mode;
        private readonly string buyTemplateId;
        private readonly string sellTemplateId;
        private readonly string id;

        /// <summary>
        /// Open Payments Platform service
        /// </summary>
        /// <param name="Country">Country where service operates</param>
        /// <param name="Service">Service reference</param>
        /// <param name="Mode">Operation mode</param>
        /// <param name="Provider">Service provider.</param>
        public OpenPaymentsPlatformService(CaseInsensitiveString Country, AspServiceProvider Service, OperationMode Mode,
            OpenPaymentsPlatformServiceProvider Provider)
        {
            this.country = Country;
            this.service = Service;
            this.mode = Mode;
            this.provider = Provider;

            if (Mode == OperationMode.Production)
            {
                this.id = "Production." + this.service.BicFi;

                if (!buyTemplateIdsProduction.TryGetValue(Service.BicFi.ToUpper(), out this.buyTemplateId))
                    this.buyTemplateId = null;

                if (!sellTemplateIdsProduction.TryGetValue(Service.BicFi.ToUpper(), out this.sellTemplateId))
                    this.sellTemplateId = null;
            }
            else
            {
                this.id = "Sandbox." + this.service.BicFi;

                if (string.IsNullOrEmpty(Gateway.Domain))
                {
                    if (!buyTemplateIdsLocalDev.TryGetValue(Service.BicFi.ToUpper(), out this.buyTemplateId))
                        this.buyTemplateId = null;

                    if (!sellTemplateIdsLocalDev.TryGetValue(Service.BicFi.ToUpper(), out this.sellTemplateId))
                        this.sellTemplateId = null;
                }
                else
                {
                    if (!buyTemplateIdsSandbox.TryGetValue(Service.BicFi.ToUpper(), out this.buyTemplateId))
                        this.buyTemplateId = null;

                    if (!sellTemplateIdsSandbox.TryGetValue(Service.BicFi.ToUpper(), out this.sellTemplateId))
                        this.sellTemplateId = null;
                }
            }
        }

        #region IServiceProvider

        /// <summary>
        /// ID of service
        /// </summary>
        public string Id => this.id;

        /// <summary>
        /// Name of service
        /// </summary>
        public string Name => this.service.Name;

        /// <summary>
        /// Icon URL
        /// </summary>
        public string IconUrl => this.service.LogoUrl;

        /// <summary>
        /// Width of icon, in pixels.
        /// </summary>
        public int IconWidth => 181;

        /// <summary>
        /// Height of icon, in pixels
        /// </summary>
        public int IconHeight => 150;

        #endregion

        #region IProcessingSupport<CaseInsensitiveString>

        /// <summary>
        /// How well a currency is supported
        /// </summary>
        /// <param name="Currency">Currency</param>
        /// <returns>Support</returns>
        public Grade Supports(CaseInsensitiveString Currency)
        {
            string Expected;

            switch (this.country.LowerCase)
            {
                case "se":
                    Expected = "sek";
                    break;

                case "no":
                    Expected = "nok";
                    break;

                case "dk":
                    Expected = "dkk";
                    break;

                case "fi":
                case "de":
                    Expected = "eur";
                    break;

                case "uk":
                    Expected = "gbp";
                    break;

                default:
                    return Grade.NotAtAll;

            }

            if (Currency.LowerCase == Expected)
                return Grade.Excellent;

            switch (Currency.LowerCase)
            {
                case "sek":
                case "dkk":
                case "nok":
                case "eur":
                case "gbp":
                    return Grade.Ok;

                default:
                    return Grade.NotAtAll;
            }
        }

        #endregion

        #region IBuyEDalerService

        /// <summary>
        /// Contract ID of Template, for buying e-Daler
        /// </summary>
        public string BuyEDalerTemplateContractId => this.buyTemplateId ?? string.Empty;

        /// <summary>
        /// Reference to service provider
        /// </summary>
        public IBuyEDalerServiceProvider BuyEDalerServiceProvider => this.provider;

        /// <summary>
        /// If the service provider can be used to process a request to buy eDaler
        /// of a certain amount, for a given account.
        /// </summary>
        /// <param name="AccountName">Account Name</param>
        /// <returns>If service provider can be used.</returns>
        public Task<bool> CanBuyEDaler(CaseInsensitiveString AccountName)
        {
            if (this.buyTemplateId is null)
                return Task.FromResult(false);

            return this.IsConfigured();
        }

        private async Task<bool> IsConfigured()
        {
            ServiceConfiguration Configuration = await ServiceConfiguration.GetCurrent();
            return Configuration.IsWellDefined;
        }

        private async Task RequestClientVerification(ClientUrlEventHandler ClientUrlCallback,
                OpenPaymentsPlatformClient OPPClient,
                ChallengeData ChallengeData,
                string ScaOAuth,
                string TabId,
                object State,
                string SuccessUrl,
                AuthenticationMethod AuthenticationMethod,
                bool shouldRefreshBankIdUrl = true)
        {
            try
            {
                Log.Informational("RequestClientVerification started");

                if (ChallengeData is null)
                {
                    Log.Error("Challenge data is null...");
                    return;
                }

                if (ClientUrlCallback != null && shouldRefreshBankIdUrl)
                {
                    if (!string.IsNullOrEmpty(ChallengeData?.BankIdURL))
                    {
                        await ClientUrlCallback(this, new ClientUrlEventArgs(
                            ChallengeData.BankIdURL, State));
                    }
                    else if (!string.IsNullOrEmpty(ScaOAuth))
                    {
                        string Url = OPPClient.GetClientWebUrl(ScaOAuth,
                            "https://lab.tagroot.io/ReturnFromPayment.md", SuccessUrl);

                        await ClientUrlCallback(this, new ClientUrlEventArgs(Url, State));
                    }
                }

                if (string.IsNullOrEmpty(TabId))
                {
                    return;
                }

                Log.Informational("Chosen AuthenticationMethodId: " + AuthenticationMethod.MethodId);

                switch (AuthenticationMethod.MethodId)
                {
                    case AuthenticationMethodId.MBID_ANIMATED_QR_TOKEN:
                    case AuthenticationMethodId.MBID_ANIMATED_QR_IMAGE:
                        await Payment.RefreshQrCode(TabId, ChallengeData);
                        break;

                    case AuthenticationMethodId.MBID:
                    case AuthenticationMethodId.MBID_SAME_DEVICE:
                        if (!shouldRefreshBankIdUrl)
                        {
                            return;
                        }

                        await Payment.RequestOpenBankIdApp(TabId, ChallengeData);
                        break;
                }

                Log.Informational("RequestClientVerification finished");
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Processes payment for buying eDaler.
        /// </summary>
        /// <param name="ContractParameters">Parameters available in the
        /// contract authorizing the payment.</param>
        /// <param name="IdentityProperties">Properties engraved into the
        /// legal identity signing the payment request.</param>
        /// <param name="Amount">Amount to be paid.</param>
        /// <param name="Currency">Currency</param>
        /// <param name="SuccessUrl">Optional Success URL the service provider can open on the client from a client web page, if payment has succeeded.</param>
        /// <param name="FailureUrl">Optional Failure URL the service provider can open on the client from a client web page, if payment has succeeded.</param>
        /// <param name="CancelUrl">Optional Cancel URL the service provider can open on the client from a client web page, if payment has succeeded.</param>
        /// <param name="ClientUrlCallback">Method to call if the payment service
        /// requests an URL to be displayed on the client.</param>
        /// <param name="State">State object to pass on the callback method.</param>
        /// <returns>Result of operation.</returns>
        public async Task<PaymentResult> BuyEDaler(IDictionary<CaseInsensitiveString, object> ContractParameters,
                    IDictionary<CaseInsensitiveString, CaseInsensitiveString> IdentityProperties,
                    decimal Amount, string Currency, string SuccessUrl, string FailureUrl, string CancelUrl, ClientUrlEventHandler ClientUrlCallback, object State)
        {
            string TabId = string.Empty;
            OpenPaymentsPlatformClient Client = null;

            try
            {
                ServiceConfiguration Configuration = await ServiceConfiguration.GetCurrent();
                if (!Configuration.IsWellDefined)
                    throw new Exception("Service not configured properly.");

                Log.Informational("OPP started");

                AuthorizationFlow Flow = Configuration.AuthorizationFlow;

                if (string.IsNullOrEmpty(this.buyTemplateId) || Flow == AuthorizationFlow.Redirect)
                {
                    ContractParameters["Amount"] = Amount;
                    ContractParameters["Currency"] = Currency;
                }

                ValidationResult validatedParameters = this.ValidateParameters(ContractParameters, IdentityProperties, Amount, Currency);
                string Message = validatedParameters.ErrorMessage;

                if (!string.IsNullOrEmpty(Message))
                {
                    throw new Exception(Message);
                }

                Message = CheckJidHostedByServer(IdentityProperties, out CaseInsensitiveString Account);
                if (!string.IsNullOrEmpty(Message))
                {
                    throw new Exception(Message);
                }

                Client = OpenPaymentsPlatformServiceProvider.CreateClient(Configuration, this.mode,
                   ServicePurpose.Private);    // TODO: Contracts for corporate accounts (when using corporate IDs).

                if (Client is null)
                {
                    throw new Exception("Service not configured properly.");
                }

                string PersonalID = GetPersonalID(validatedParameters.PersonalNumber);
                if (mode == OperationMode.Sandbox)
                {
                    PersonalID = "";
                }

                KeyValuePair<IPAddress, PaymentResult> P = await GetRemoteEndpoint(Account);
                if (!(P.Value is null))
                    return P.Value;

                IPAddress ClientIpAddress = P.Key;

                OperationInformation Operation = new OperationInformation(
                    ClientIpAddress,
                    typeof(OpenPaymentsPlatformServiceProvider).Assembly.FullName,
                    Flow,
                    PersonalID,
                    null,
                    this.service.BicFi);

                PaymentProduct Product;

                if (Configuration.NeuronBankAccountIban.Substring(0, 2) == validatedParameters.BankAccount.Substring(0, 2))
                {
                    Product = PaymentProduct.domestic;
                }
                else if (Currency.ToUpper() == "EUR")
                {
                    Product = PaymentProduct.sepa_credit_transfers;
                }
                else
                {
                    Product = PaymentProduct.international;
                }

                Payment payment = validatedParameters.SplitPaymentOptions.Any() ?
                    new BulkPayment(Operation, Client, Product, State, SuccessUrl, ClientUrlCallback) :
                    new SinglePayment(Operation, Client, Product, State, SuccessUrl, ClientUrlCallback);

                await payment.InitiatePayment(validatedParameters, Amount, Currency);
                await Payment.NotifyTransactionState(TransactionState.TransactionCompleted, TabId);

                return new PaymentResult(Amount, Currency);
            }
            catch (Exception ex)
            {
                await Payment.NotifyTransactionState(TransactionState.TransactionFailed, TabId, ex.Message);
                return new PaymentResult(ex.Message);
            }
            finally
            {
                OpenPaymentsPlatformServiceProvider.Dispose(Client, this.mode);
            }
        }

        private static string CheckJidHostedByServer(IDictionary<CaseInsensitiveString, CaseInsensitiveString> IdentityProperties,
            out CaseInsensitiveString Account)
        {
            Account = null;

            if (!IdentityProperties.TryGetValue("JID", out CaseInsensitiveString JID))
                return "JID not encoded into identity.";

            int i = JID.IndexOf('@');
            if (i < 0)
                return "Invalid JID encoded into identity.";

            Account = JID.Substring(0, i);
            CaseInsensitiveString Domain = JID.Substring(i + 1);
            bool IsServerDomain = Domain == Gateway.Domain;

            if (!IsServerDomain)
            {
                foreach (CaseInsensitiveString AlternativeDomain in Gateway.AlternativeDomains)
                {
                    if (AlternativeDomain == Domain)
                    {
                        IsServerDomain = true;
                        break;
                    }
                }

                if (!IsServerDomain)
                    return "JID not registered on this server.";
            }

            return null;
        }

        private static string GetPersonalID(CaseInsensitiveString PersonalNumber)
        {
            return PersonalNumber?.Value?.
                Replace("-", string.Empty).
                Replace(".", string.Empty).
                Replace(" ", string.Empty);
        }

        private static async Task<KeyValuePair<IPAddress, PaymentResult>> GetRemoteEndpoint(CaseInsensitiveString Account)
        {
            IEnumerable<GenericObject> LoginRecords = await Database.Find<GenericObject>(
                "BrokerAccountLogins", 0, 1,
                new FilterFieldEqualTo("UserName", Account));

            string RemoteEndpoint = null;

            foreach (GenericObject LoginRecord in LoginRecords)
            {
                if (LoginRecord.TryGetFieldValue("RemoteEndpoint", out object Obj))
                {
                    RemoteEndpoint = Obj?.ToString();
                    break;
                }
            }

            if (string.IsNullOrEmpty(RemoteEndpoint))
                return new KeyValuePair<IPAddress, PaymentResult>(null, new PaymentResult("Client IP address was not found. Required to process payment."));

            int i = RemoteEndpoint.LastIndexOf(':');
            if (i > 0)
                RemoteEndpoint = RemoteEndpoint.Substring(0, i);

            if (!IPAddress.TryParse(RemoteEndpoint, out IPAddress ClientIpAddress))
                return new KeyValuePair<IPAddress, PaymentResult>(null, new PaymentResult("Client not connected via IP network. Required to process payment."));

            return new KeyValuePair<IPAddress, PaymentResult>(ClientIpAddress, null);
        }

        private ValidationResult ValidateParameters(
            IDictionary<CaseInsensitiveString, object> contractParameters,
            IDictionary<CaseInsensitiveString, CaseInsensitiveString> identityProperties,
            decimal amount, string currency)
        {
            var result = new ValidationResult();

            try
            {
                // Helper method to extract and convert a parameter
                string GetStringParameter(string key, string errorMessage)
                {
                    if (contractParameters.TryGetValue(key, out var value) && value is string strValue)
                        return strValue;

                    throw new Exception(errorMessage);
                }

                decimal GetDecimalParameter(string key, string errorMessage)
                {
                    if (contractParameters.TryGetValue(key, out var obj))
                    {
                        if (obj is decimal value)
                            return value;

                        try
                        {
                            return Expression.ToDecimal(obj);
                        }
                        catch
                        {
                            throw new Exception($"Value for {key} is not of the correct type. Value: {Expression.ToString(obj)}, Type: {obj?.GetType().FullName}");
                        }
                    }

                    throw new Exception(errorMessage);
                }

                // Get mandatory parameters
                var contractAmount = GetDecimalParameter("Amount", "Amount not available in contract.");
                var contractCurrency = GetStringParameter("Currency", "Currency not available in contract.");
                var contractAccount = GetStringParameter("Account", "Account not available in contract.");

                // Validate amounts and currencies
                if (contractAmount != amount)
                    throw new Exception("Amount in contract does not match amount in call.");

                if (contractCurrency != currency)
                    throw new Exception("Currency in contract does not match currency in call.");

                // Validate account
                if (contractAccount.Length <= 2)
                    throw new Exception("Invalid bank account.");

                result.BankAccount = contractAccount;

                // Optional parameters
                if (contractParameters.TryGetValue("tabId", out var tabIdObj) && tabIdObj is string tabId)
                    result.TabId = tabId;

                if (contractParameters.TryGetValue("callBackUrl", out var callBackUrlObj) && callBackUrlObj is string callBackUrl)
                    result.CallBackUrl = callBackUrl;

                if (contractParameters.TryGetValue("requestFromMobilePhone", out var isMobileObj))
                    result.RequestFromMobilePhone = Convert.ToBoolean(isMobileObj);

                if (contractParameters.TryGetValue("AccountName", out var accountNameObj))
                    result.AccountName = accountNameObj?.ToString();

                if (contractParameters.TryGetValue("SplitPaymentOptions", out var splitObj))
                {
                    if (splitObj is not string s)
                    {
                        throw new Exception("Split object must be a valid json string.");
                    }

                    Log.Informational(s);

                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true // Ignore case when matching properties
                        };

                        result.SplitPaymentOptions = JsonSerializer.Deserialize<List<SplitPaymentOption>>(s, options);
                    }
                    catch
                    {
                        throw new Exception("Split object must be a valid json string.");
                    }
                }

                if (result.SplitPaymentOptions.Any(m => string.IsNullOrEmpty(m.BankAccount) || string.IsNullOrEmpty(m.AccountName)
                || string.IsNullOrEmpty(m.Description) || m.Amount <= 0))
                {
                    throw new Exception("SplitPaymentOptions are not valid...");
                }

                // Personal number extraction
                result.PersonalNumber = GetPersonalNumber(identityProperties, contractParameters);

                // Message validation
                if (contractParameters.TryGetValue("Message", out var messageObj) && messageObj is string message)
                {
                    message = message.Trim();
                    if (message.Length > 10)
                        throw new Exception("Message cannot be longer than 10 characters.");

                    result.TextMessage = message;
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        private string GetPersonalNumber(IDictionary<CaseInsensitiveString, CaseInsensitiveString> identityProperties,
                                          IDictionary<CaseInsensitiveString, object> contractParameters)
        {
            if (contractParameters.TryGetValue("personalNumber", out var personalNumberObj) && personalNumberObj is string personalNumber && !string.IsNullOrEmpty(personalNumber))
                return personalNumber;

            identityProperties.TryGetValue("PNR", out var personalNumberKey);
            return personalNumberKey;
        }


        /// <summary>
        /// Gets available payment options for buying eDaler.
        /// </summary>
        /// <param name="IdentityProperties">Properties engraved into the legal identity that will performm the request.</param>
        /// <param name="SuccessUrl">Optional Success URL the service provider can open on the client from a client web page, if getting options has succeeded.</param>
        /// <param name="FailureUrl">Optional Failure URL the service provider can open on the client from a client web page, if getting options has succeeded.</param>
        /// <param name="CancelUrl">Optional Cancel URL the service provider can open on the client from a client web page, if getting options has succeeded.</param>
        /// <param name="TabId">Tab ID</param>
        /// <param name="RequestFromMobilePhone">If request originates from mobile phone. (true)
        /// <param name="RemoteEndpoint">Client IP adress
        /// <returns>Result of operation.</returns>
        public async Task<IDictionary<CaseInsensitiveString, object>[]> GetPaymentOptionsForBuyingEDaler(
            IDictionary<CaseInsensitiveString, CaseInsensitiveString> IdentityProperties,
            string SuccessUrl, string FailureUrl, string CancelUrl, string TabId, bool RequestFromMobilePhone, string RemoteEndpoint)
        {
            IPAddress.TryParse(RemoteEndpoint, out IPAddress ClientIpAddress);

            ServiceConfiguration Configuration = await ServiceConfiguration.GetCurrent();
            if (!Configuration.IsWellDefined)
                return new IDictionary<CaseInsensitiveString, object>[0];

            AuthorizationFlow Flow = Configuration.AuthorizationFlow;

            string Message = CheckJidHostedByServer(IdentityProperties, out CaseInsensitiveString Account);
            if (!string.IsNullOrEmpty(Message))
                return new IDictionary<CaseInsensitiveString, object>[0];

            if (!(IdentityProperties.TryGetValue("PNR", out CaseInsensitiveString PersonalNumber)))
                return new IDictionary<CaseInsensitiveString, object>[0];

            Log.Informational("Account" + Account + "PersonalNumber" + PersonalNumber);

            OpenPaymentsPlatformClient Client = OpenPaymentsPlatformServiceProvider.CreateClient(Configuration, this.mode,
                ServicePurpose.Private);    // TODO: Contracts for corporate accounts (when using corporate IDs).

            if (Client is null)
                return new IDictionary<CaseInsensitiveString, object>[0];


            Log.Informational("Client created ");
            try
            {
                string PersonalID = GetPersonalID(PersonalNumber);

                if (mode == OperationMode.Sandbox)
                {
                    PersonalID = "";
                }

                OperationInformation Operation = new OperationInformation(
                    ClientIpAddress,
                    typeof(OpenPaymentsPlatformServiceProvider).Assembly.FullName,
                    Flow,
                    PersonalID,
                    null,
                    this.service.BicFi);

                ConsentStatus Consent = await Client.CreateConsent(string.Empty, true, false, false,
                    DateTime.Today.AddDays(1), 1, false, Operation);

                Log.Informational("Consent created ");

                AuthorizationInformation Status = await Client.StartConsentAuthorization(Consent.ConsentID, Operation);

                AuthenticationMethod Method = null;

                if (!string.IsNullOrEmpty(TabId))
                    if (RequestFromMobilePhone)
                    {
                        Method = Status.GetAuthenticationMethod(AuthenticationMethodId.MBID_SAME_DEVICE)
                            ?? Status.GetAuthenticationMethod(AuthenticationMethodId.MBID);
                    }
                    else
                    {
                        Method = Status.GetAuthenticationMethod(AuthenticationMethodId.MBID_ANIMATED_QR_TOKEN)
                              ?? Status.GetAuthenticationMethod(AuthenticationMethodId.MBID_ANIMATED_QR_IMAGE)
                              ?? Status.GetAuthenticationMethod(AuthenticationMethodId.MBID)
                              ?? Status.GetAuthenticationMethod(AuthenticationMethodId.MBID_SAME_DEVICE);
                    }

                if (Method is null)
                    return new IDictionary<CaseInsensitiveString, object>[0];

                PaymentServiceUserDataResponse PsuDataResponse = await Client.PutConsentUserData(Consent.ConsentID,
                    Status.AuthorizationID, Method.MethodId, Operation);

                if (PsuDataResponse is null)
                    return new IDictionary<CaseInsensitiveString, object>[0];

                await RequestClientVerification(null, Client, PsuDataResponse.ChallengeData, null, TabId, null, SuccessUrl, Method);

                TppMessage[] ErrorMessages = PsuDataResponse.Messages;
                AuthorizationStatusValue AuthorizationStatusValue = PsuDataResponse.Status;
                DateTime Start = DateTime.Now;
                bool PaymentAuthorizationStarted = AuthorizationStatusValue == AuthorizationStatusValue.started ||
                        AuthorizationStatusValue == AuthorizationStatusValue.authenticationStarted;
                bool CreditorAuthorizationStarted = AuthorizationStatusValue == AuthorizationStatusValue.authoriseCreditorAccountStarted;

                int counter = 0;
                while (AuthorizationStatusValue != AuthorizationStatusValue.finalised &&
                    AuthorizationStatusValue != AuthorizationStatusValue.failed &&
                    DateTime.Now.Subtract(Start).TotalMinutes < Configuration.TimeoutMinutes)
                {
                    counter++;
                    await Task.Delay(Configuration.PollingIntervalSeconds);

                    AuthorizationStatus P2 = await Client.GetConsentAuthorizationStatus(Consent.ConsentID, Status.AuthorizationID, Operation);

                    AuthorizationStatusValue = P2.Status;

                    ErrorMessages = P2.Messages;

                    switch (AuthorizationStatusValue)
                    {
                        case AuthorizationStatusValue.started:
                        case AuthorizationStatusValue.authenticationStarted:
                            Log.Informational("authenticationStarted");

                            if (!PaymentAuthorizationStarted)
                            {
                                PaymentAuthorizationStarted = true;
                            }

                            await RequestClientVerification(null,
                                Client, P2.ChallengeData, string.Empty, TabId, null, string.Empty, Method, !PaymentAuthorizationStarted);
                            break;

                        case AuthorizationStatusValue.authoriseCreditorAccountStarted:
                            Log.Informational("authoriseCreditorAccountStarted");

                            if (!CreditorAuthorizationStarted)

                            {
                                CreditorAuthorizationStarted = true;
                            }

                            await RequestClientVerification(null,
                                Client, P2.ChallengeData, string.Empty, TabId, null, string.Empty, Method, !CreditorAuthorizationStarted);
                            break;
                    }
                }

                ConsentStatusValue ConsentStatusValue = await Client.GetConsentStatus(Consent.ConsentID, Operation);
                switch (ConsentStatusValue)
                {
                    case ConsentStatusValue.rejected:
                        Log.Informational("Consent was rejected.");
                        break;

                    case ConsentStatusValue.revokedByPsu:
                        Log.Informational("Consent was revoked.");
                        break;

                    case ConsentStatusValue.expired:
                        Log.Informational("Consent has expired.");
                        break;

                    case ConsentStatusValue.terminatedByTpp:
                        Log.Informational("Consent was terminated.");
                        break;

                    case ConsentStatusValue.valid:
                        Log.Informational("Consent is valid.");
                        break;

                    default:
                        Log.Informational("Consent was not valid.");
                        break;
                }

                Log.Informational("Consent was :" + ConsentStatusValue.ToString());

                if (!(ErrorMessages is null) && ErrorMessages.Length > 0)
                    throw new Exception(ErrorMessages[0].Text);

                AccountInformation[] Accounts = await Client.GetAccounts(Consent.ConsentID, Operation, true);
                List<IDictionary<CaseInsensitiveString, object>> Result = new List<IDictionary<CaseInsensitiveString, object>>();

                foreach (AccountInformation Account2 in Accounts)
                {
                    Log.Informational(Account2.Iban + "" + Account2.Name);
                    Result.Add(new Dictionary<CaseInsensitiveString, object>()
                    {
                        { "Account", Account2.Iban },
                        { "ResourceId", Account2.ResourceID },
                        { "Iban", Account2.Iban },
                        { "Bban", Account2.Bban },
                        { "Bic", Account2.Bic },
                        { "Balance", Account2.Balance},
                        { "Currency", Account2.Currency },
                        { "CashAccountType", Account2.CashAccountType },
                        { "Name", Account2.Name},
                        { "OwnerName", Account2.OwnerName },
                        { "Product", Account2.Product},
                        { "Status", Account2.Status },
                        { "Usage", Account2.Usage },
                });
                }

                await ClientEvents.PushEvent(new string[] { TabId }, "ShowAccountInfo",
                       JSON.Encode(new Dictionary<string, object>()
                       {
                                { "AccountInfo", Result.ToArray()},
                                { "message", "Account information the following QR-code with your Bank-ID app, or click on it if your Bank-ID is installed on your computer." },
                       }, false), true);

                return Result.ToArray();
            }
            finally
            {
                OpenPaymentsPlatformServiceProvider.Dispose(Client, this.mode);
            }
        }


        /// <summary>
		/// Gets available payment options for buying eDaler.
		/// </summary>
		/// <param name="IdentityProperties">Properties engraved into the legal identity that will performm the request.</param>
		/// <param name="SuccessUrl">Optional Success URL the service provider can open on the client from a client web page, if getting options has succeeded.</param>
		/// <param name="FailureUrl">Optional Failure URL the service provider can open on the client from a client web page, if getting options has succeeded.</param>
		/// <param name="CancelUrl">Optional Cancel URL the service provider can open on the client from a client web page, if getting options has succeeded.</param>
		/// <param name="ClientUrlCallback">Method to call if the payment service
		/// requests an URL to be displayed on the client.</param>
		/// <param name="State">State object to pass on the callback method.</param>
		/// <returns>Array of dictionaries, each dictionary representing a set of parameters that can be selected in the
		/// contract to sign.</returns>
		public async Task<IDictionary<CaseInsensitiveString, object>[]> GetPaymentOptionsForBuyingEDaler(
            IDictionary<CaseInsensitiveString, CaseInsensitiveString> IdentityProperties,
            string SuccessUrl, string FailureUrl, string CancelUrl, ClientUrlEventHandler ClientUrlCallback, object State)
        {
            ServiceConfiguration Configuration = await ServiceConfiguration.GetCurrent();
            if (!Configuration.IsWellDefined)
                return new IDictionary<CaseInsensitiveString, object>[0];

            AuthorizationFlow Flow = Configuration.AuthorizationFlow;

            string Message = CheckJidHostedByServer(IdentityProperties, out CaseInsensitiveString Account);
            if (!string.IsNullOrEmpty(Message))
                return new IDictionary<CaseInsensitiveString, object>[0];

            if (!(IdentityProperties.TryGetValue("PNR", out CaseInsensitiveString PersonalNumber)))
                return new IDictionary<CaseInsensitiveString, object>[0];

            OpenPaymentsPlatformClient Client = OpenPaymentsPlatformServiceProvider.CreateClient(Configuration, this.mode,
                ServicePurpose.Private);    // TODO: Contracts for corporate accounts (when using corporate IDs).

            if (Client is null)
                return new IDictionary<CaseInsensitiveString, object>[0];

            try
            {
                string PersonalID = GetPersonalID(PersonalNumber);

                KeyValuePair<IPAddress, PaymentResult> P = await GetRemoteEndpoint(Account);
                if (!(P.Value is null))
                    return new IDictionary<CaseInsensitiveString, object>[0];

                IPAddress ClientIpAddress = P.Key;

                OperationInformation Operation = new OperationInformation(
                    ClientIpAddress,
                    typeof(OpenPaymentsPlatformServiceProvider).Assembly.FullName,
                    Flow,
                    PersonalID,
                    null,
                    this.service.BicFi);

                ConsentStatus Consent = await Client.CreateConsent(string.Empty, true, false, false,
                    DateTime.Today.AddDays(1), 1, false, Operation);

                if (Consent.Status != ConsentStatusValue.received)
                    return new IDictionary<CaseInsensitiveString, object>[0];

                AuthorizationInformation Status = await Client.StartConsentAuthorization(Consent.ConsentID, Operation);

                AuthenticationMethod Method = Status.GetAuthenticationMethod(AuthenticationMethodId.MBID_SAME_DEVICE)
                    ?? Status.GetAuthenticationMethod(AuthenticationMethodId.MBID);

                if (Method is null)
                    return new IDictionary<CaseInsensitiveString, object>[0];

                PaymentServiceUserDataResponse PsuDataResponse = await Client.PutConsentUserData(Consent.ConsentID,
                    Status.AuthorizationID, Method.MethodId, Operation);

                if (PsuDataResponse is null)
                    return new IDictionary<CaseInsensitiveString, object>[0];

                if (!(ClientUrlCallback is null))
                {
                    if (!string.IsNullOrEmpty(PsuDataResponse.ChallengeData?.BankIdURL))
                    {
                        await ClientUrlCallback(this, new ClientUrlEventArgs(
                            PsuDataResponse.ChallengeData.BankIdURL, State));
                    }
                    else if (!string.IsNullOrEmpty(PsuDataResponse.Links.ScaOAuth))
                    {
                        string Url = Client.GetClientWebUrl(PsuDataResponse.Links.ScaOAuth,
                            "https://lab.tagroot.io/ReturnFromPayment.md", SuccessUrl);

                        await ClientUrlCallback(this, new ClientUrlEventArgs(Url, State));
                    }
                }

                TppMessage[] ErrorMessages = PsuDataResponse.Messages;
                AuthorizationStatusValue AuthorizationStatusValue = PsuDataResponse.Status;
                DateTime Start = DateTime.Now;
                bool PaymentAuthorizationStarted = AuthorizationStatusValue == AuthorizationStatusValue.started ||
                        AuthorizationStatusValue == AuthorizationStatusValue.authenticationStarted;
                bool CreditorAuthorizationStarted = AuthorizationStatusValue == AuthorizationStatusValue.authoriseCreditorAccountStarted;

                while (AuthorizationStatusValue != AuthorizationStatusValue.finalised &&
                    AuthorizationStatusValue != AuthorizationStatusValue.failed &&
                    DateTime.Now.Subtract(Start).TotalMinutes < Configuration.TimeoutMinutes)
                {
                    await Task.Delay(Configuration.PollingIntervalSeconds);

                    AuthorizationStatus P2 = await Client.GetConsentAuthorizationStatus(Consent.ConsentID, Status.AuthorizationID, Operation);

                    AuthorizationStatusValue = P2.Status;
                    ErrorMessages = P2.Messages;

                    if (!string.IsNullOrEmpty(P2.ChallengeData?.BankIdURL) && !(ClientUrlCallback is null))
                    {
                        switch (AuthorizationStatusValue)
                        {
                            case AuthorizationStatusValue.started:
                            case AuthorizationStatusValue.authenticationStarted:
                                if (!PaymentAuthorizationStarted)
                                {
                                    PaymentAuthorizationStarted = true;

                                    ClientUrlEventArgs e = new ClientUrlEventArgs(P2.ChallengeData.BankIdURL, State);
                                    await ClientUrlCallback(this, e);
                                }
                                break;

                            case AuthorizationStatusValue.authoriseCreditorAccountStarted:
                                if (!CreditorAuthorizationStarted)
                                {
                                    CreditorAuthorizationStarted = true;

                                    ClientUrlEventArgs e = new ClientUrlEventArgs(P2.ChallengeData.BankIdURL, State);
                                    await ClientUrlCallback(this, e);
                                }
                                break;
                        }
                    }
                }

                if (!(ErrorMessages is null) && ErrorMessages.Length > 0)
                    throw new Exception(ErrorMessages[0].Text);

                AccountInformation[] Accounts = await Client.GetAccounts(Consent.ConsentID, Operation, true);
                List<IDictionary<CaseInsensitiveString, object>> Result = new List<IDictionary<CaseInsensitiveString, object>>();

                foreach (AccountInformation Account2 in Accounts)
                {
                    Result.Add(new Dictionary<CaseInsensitiveString, object>()
                    {
                        { "Account", Account2.Iban },
                        { "ResourceId", Account2.ResourceID },
                        { "Iban", Account2.Iban },
                        { "Bban", Account2.Bban },
                        { "Bic", Account2.Bic },
                        { "Balance", Account2.Balance},
                        { "Currency", Account2.Currency },
                        { "CashAccountType", Account2.CashAccountType },
                        { "Name", Account2.Name},
                        { "OwnerName", Account2.OwnerName },
                        { "Product", Account2.Product},
                        { "Status", Account2.Status },
                        { "Usage", Account2.Usage },
                });
                }

                return Result.ToArray();
            }
            finally
            {
                OpenPaymentsPlatformServiceProvider.Dispose(Client, this.mode);
            }
        }

        #endregion

        #region ISellEDalerService

        /// <summary>
        /// Contract ID of Template, for selling e-Daler
        /// </summary>
        public string SellEDalerTemplateContractId => this.sellTemplateId ?? string.Empty;

        /// <summary>
        /// Reference to service provider
        /// </summary>
        public ISellEDalerServiceProvider SellEDalerServiceProvider => this.provider;

        /// <summary>
        /// If the service provider can be used to process a request to sell eDaler
        /// of a certain amount, for a given account.
        /// </summary>
        /// <param name="AccountName">Account Name</param>
        /// <returns>If service provider can be used.</returns>
        public Task<bool> CanSellEDaler(CaseInsensitiveString AccountName)
        {
            if (string.IsNullOrEmpty(this.sellTemplateId))
                return Task.FromResult(false);

            return this.IsConfigured();
        }

        /// <summary>
        /// Processes payment for selling eDaler.
        /// </summary>
        /// <param name="ContractParameters">Parameters available in the
        /// contract authorizing the payment.</param>
        /// <param name="IdentityProperties">Properties engraved into the
        /// legal identity signing the payment request.</param>
        /// <param name="Amount">Amount of eDaler to be sold.</param>
        /// <param name="Currency">Desired Currency</param>
        /// <param name="SuccessUrl">Optional Success URL the service provider can open on the client from a client web page, if payment has succeeded.</param>
        /// <param name="FailureUrl">Optional Failure URL the service provider can open on the client from a client web page, if payment has succeeded.</param>
        /// <param name="CancelUrl">Optional Cancel URL the service provider can open on the client from a client web page, if payment has succeeded.</param>
        /// <param name="ClientUrlCallback">Method to call if the payment service
        /// requests an URL to be displayed on the client.</param>
        /// <param name="State">State object to pass on the callback method.</param>
        /// <returns>Result of operation.</returns>
        public async Task<PaymentResult> SellEDaler(IDictionary<CaseInsensitiveString, object> ContractParameters,
            IDictionary<CaseInsensitiveString, CaseInsensitiveString> IdentityProperties,
            decimal Amount, string Currency, string SuccessUrl, string FailureUrl, string CancelUrl, ClientUrlEventHandler ClientUrlCallback, object State)
        {
            ServiceConfiguration Configuration = await ServiceConfiguration.GetCurrent();
            if (!Configuration.IsWellDefined)
                return new PaymentResult("Service not configured properly.");

            AuthorizationFlow Flow = Configuration.AuthorizationFlow;

            if (string.IsNullOrEmpty(this.sellTemplateId) || Flow == AuthorizationFlow.Redirect)
            {
                ContractParameters["Amount"] = Amount;
                ContractParameters["Currency"] = Currency;
            }

            var validationResult = this.ValidateParameters(ContractParameters, IdentityProperties, Amount, Currency);
            var Message = validationResult.ErrorMessage;

            if (!string.IsNullOrEmpty(Message))
            {
                return new PaymentResult(Message);
            }

            if (string.IsNullOrWhiteSpace(validationResult.AccountName))
            {
                return new PaymentResult("AccountName not available in contract parameters");
            }

            Message = CheckJidHostedByServer(IdentityProperties, out CaseInsensitiveString Account);
            if (!string.IsNullOrEmpty(Message))
            {
                return new PaymentResult(Message);
            }

            OpenPaymentsPlatformClient Client = OpenPaymentsPlatformServiceProvider.CreateClient(Configuration, this.mode);
            if (Client is null)
                return new PaymentResult("Service not configured properly.");
            try
            {
                string PersonalID = mode != OperationMode.Sandbox ? GetPersonalID(Configuration.PersonalID) : string.Empty;
                string OrganizationID = GetPersonalID(Configuration.OrganizationID);

                KeyValuePair<IPAddress, PaymentResult> P = await GetRemoteEndpoint(Account);
                if (!(P.Value is null))
                    return P.Value;

                IPAddress ClientIpAddress = P.Key;

                OperationInformation Operation = new OperationInformation(
                    ClientIpAddress,
                    typeof(OpenPaymentsPlatformServiceProvider).Assembly.FullName,
                    Flow,
                    PersonalID,
                    OrganizationID,
                    Configuration.NeuronBankBic);

                PaymentProduct Product;

                if (Configuration.NeuronBankAccountIban.Substring(0, 2) == validationResult.BankAccount.Substring(0, 2))
                    Product = PaymentProduct.domestic;
                else if (Currency.ToUpper() == "EUR")
                    Product = PaymentProduct.sepa_credit_transfers;
                else
                    Product = PaymentProduct.international;


                PaymentInitiationReference PaymentInitiationReference = await Client.CreatePaymentInitiation(
                    Product, Amount, Currency, Configuration.NeuronBankAccountIban, Currency,
                    validationResult.BankAccount, Currency, validationResult.AccountName, validationResult.TextMessage, Operation);

                DateTime TP = DateTime.UtcNow;
                OutboundPayment PaymentRecord = new OutboundPayment()
                {
                    Created = TP,
                    Updated = TP,
                    Account = Account,
                    Paid = DateTime.MinValue,
                    PaymentId = PaymentInitiationReference.PaymentId,
                    BasketId = null,
                    Message = PaymentInitiationReference.Message,
                    TransactionStatus = PaymentInitiationReference.TransactionStatus,
                    Product = Product,
                    Amount = Amount,
                    Currency = Currency,
                    FromBankAccount = Configuration.NeuronBankAccountIban,
                    FromBank = Operation.ServiceProvider,
                    ToBankAccount = validationResult.BankAccount,
                    ToBank = this.service.BicFi,
                    ToBankAccountName = validationResult.AccountName,
                    TextMessage = validationResult.TextMessage
                };

                await Database.Insert(PaymentRecord);
                StringBuilder Markdown = new StringBuilder();

                Markdown.Append("Outbound payment [available for approval](");
                Markdown.Append(Gateway.GetUrl("/OpenPaymentsPlatform/OutgoingPayments.md"));
                Markdown.AppendLine("):");
                Markdown.AppendLine();
                Markdown.AppendLine("| Payment information ||");
                Markdown.AppendLine("|:---------|:----------|");
                Markdown.Append("| Payment ID | `");
                Markdown.Append(PaymentInitiationReference.PaymentId);
                Markdown.AppendLine("` |");
                Markdown.Append("| Amount   | ");
                Markdown.Append(Amount.ToString());
                Markdown.AppendLine(" |");
                Markdown.Append("| Currency | ");
                Markdown.Append(MarkdownDocument.Encode(Currency));
                Markdown.AppendLine(" |");
                Markdown.Append("| Server Account | `");
                Markdown.Append(Account);
                Markdown.AppendLine("` |");
                Markdown.Append("| From Bank Account | ");
                Markdown.Append(MarkdownDocument.Encode(Configuration.NeuronBankAccountIban));
                Markdown.AppendLine(" |");
                Markdown.Append("| From Bank | ");
                Markdown.Append(MarkdownDocument.Encode(Operation.ServiceProvider));
                Markdown.AppendLine(" |");
                Markdown.Append("| To Bank Account | ");
                Markdown.Append(MarkdownDocument.Encode(validationResult.BankAccount));
                Markdown.AppendLine(" |");
                Markdown.Append("| To Bank | ");
                Markdown.Append(MarkdownDocument.Encode(this.service.BicFi));
                Markdown.AppendLine(" |");
                Markdown.Append("| To | ");
                Markdown.Append(MarkdownDocument.Encode(validationResult.AccountName));
                Markdown.AppendLine(" |");
                Markdown.Append("| Message | ");
                Markdown.Append(MarkdownDocument.Encode(validationResult.TextMessage.Replace('\n', ' ').Replace('\r', ' ')));
                Markdown.AppendLine(" |");

                string MarkdownMessage = Markdown.ToString();

                try
                {
                    await Gateway.SendNotification(MarkdownMessage);
                }
                catch (Exception ex)
                {
                    Log.Critical(ex);
                }

                SendNotificationEmail(MarkdownMessage);

                return new PaymentResult(Amount, Currency);
            }
            catch (Exception ex)
            {
                return new PaymentResult(ex.Message);
            }
            finally
            {
                OpenPaymentsPlatformServiceProvider.Dispose(Client, this.mode);
            }
        }

        private void SendNotificationEmail(string markdown)
        {
            Task.Run(async () =>
            {
                try
                {
                    var body = new Dictionary<string, object>
                    {
                        { "markdown", markdown }
                    };

                    await InternetContent.PostAsync(
                        new Uri(Gateway.GetUrl("/OpenPaymentsPlatform/NotifyOutboundPayment.ws")),
                        body, Gateway.Certificate, new KeyValuePair<string, string>("Accept", "application/json"));
                }
                catch (Exception ex)
                {
                    Log.Critical(ex.Message);
                }
            });
        }

        /// <summary>
        /// Gets available payment options for selling eDaler.
        /// </summary>
        /// <param name="IdentityProperties">Properties engraved into the legal identity that will performm the request.</param>
        /// <param name="SuccessUrl">Optional Success URL the service provider can open on the client from a client web page, if getting options has succeeded.</param>
        /// <param name="FailureUrl">Optional Failure URL the service provider can open on the client from a client web page, if getting options has succeeded.</param>
        /// <param name="CancelUrl">Optional Cancel URL the service provider can open on the client from a client web page, if getting options has succeeded.</param>
        /// <param name="ClientUrlCallback">Method to call if the payment service
        /// requests an URL to be displayed on the client.</param>
        /// <param name="State">State object to pass on the callback method.</param>
        /// <returns>Array of dictionaries, each dictionary representing a set of parameters that can be selected in the
        /// contract to sign.</returns>
        public Task<IDictionary<CaseInsensitiveString, object>[]> GetPaymentOptionsForSellingEDaler(
            IDictionary<CaseInsensitiveString, CaseInsensitiveString> IdentityProperties,
            string SuccessUrl, string FailureUrl, string CancelUrl, ClientUrlEventHandler ClientUrlCallback, object State)
        {
            return this.GetPaymentOptionsForBuyingEDaler(IdentityProperties, SuccessUrl, FailureUrl, CancelUrl,
                ClientUrlCallback, State);
        }

        #endregion
    }
}
