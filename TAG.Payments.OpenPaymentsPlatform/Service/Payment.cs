﻿using Paiwise;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using TAG.Networking.OpenPaymentsPlatform;
using TAG.Payments.OpenPaymentsPlatform.Models;
using Waher.Content;
using Waher.Events;
using Waher.IoTGateway;
using Waher.Persistence;

namespace TAG.Payments.OpenPaymentsPlatform.Service
{
    public abstract class Payment
    {
        protected OperationInformation Operation { get; init; }
        protected OpenPaymentsPlatformClient Client { get; init; }
        protected PaymentProduct Product { get; init; }
        protected object State { get; init; }
        protected string SuccessUrl { get; init; }
        protected ClientUrlEventHandler ClientUrlCallback { get; init; }
        public Payment(OperationInformation operation, OpenPaymentsPlatformClient client, PaymentProduct product, object state, string successUrl, ClientUrlEventHandler clientUrlCallback)
        {
            Operation = operation;
            Client = client;
            Product = product;
            State = state;
            SuccessUrl = successUrl;
            ClientUrlCallback = clientUrlCallback;
        }

        private void EnsureNoErrorMessages(TppMessage[] TppMessages)
        {
            if (TppMessages?.Length > 0)
            {
                var sb = new StringBuilder();
                foreach (var tppMessage in TppMessages)
                {
                    sb.AppendLine(tppMessage.Text);
                }

                throw new Exception(sb.ToString());
            }
        }

        public async Task InitiatePayment(ValidationResult validatedParameters, decimal amount, string currency)
        {
            var Configuration = await ServiceConfiguration.GetCurrent();
            if (!Configuration.IsWellDefined)
            {
                throw new Exception("Configuration is not well defined");
            }

            (string Id, AuthenticationMethod authenticationMethod, AuthorizationInformation AuthorizationInformation) = await StartPaymentAndChoseAuthenticationMethod(validatedParameters, amount, currency);
            var psuDataResponse = await PutUserData(Id, AuthorizationInformation.AuthorizationID, authenticationMethod.MethodId);

            TppMessage[] TppMessages = psuDataResponse.Messages;
            EnsureNoErrorMessages(TppMessages);

            await RequestClientVerification(psuDataResponse.ChallengeData, psuDataResponse.Links?.ScaOAuth, validatedParameters.TabId, authenticationMethod);

            AuthorizationStatusValue AuthorizationStatusValue = psuDataResponse.Status;
            DateTime Start = DateTime.Now;

            bool PaymentAuthorizationStarted = AuthorizationStatusValue == AuthorizationStatusValue.started ||
                        AuthorizationStatusValue == AuthorizationStatusValue.authenticationStarted;
            bool CreditorAuthorizationStarted = AuthorizationStatusValue == AuthorizationStatusValue.authoriseCreditorAccountStarted;

            while (AuthorizationStatusValue != AuthorizationStatusValue.finalised &&
                    AuthorizationStatusValue != AuthorizationStatusValue.failed &&
                    DateTime.Now.Subtract(Start).TotalMinutes < Configuration.TimeoutMinutes)
            {
                await Task.Delay(Configuration.PollingIntervalSeconds);

                AuthorizationStatus authorizationStatus = await GetAuthorizationStatus(Id, AuthorizationInformation.AuthorizationID);

                if (authorizationStatus is null)
                {
                    continue;
                }

                AuthorizationStatusValue = authorizationStatus.Status;
                TppMessages = authorizationStatus.Messages;

                if (AuthorizationStatusValue == AuthorizationStatusValue.started ||
                    AuthorizationStatusValue == AuthorizationStatusValue.authenticationStarted)
                {
                    await RequestClientVerification(authorizationStatus?.ChallengeData, string.Empty, validatedParameters.TabId, authenticationMethod, !PaymentAuthorizationStarted);
                    PaymentAuthorizationStarted = true;
                }
                else if (AuthorizationStatusValue == AuthorizationStatusValue.authoriseCreditorAccountStarted)
                {
                    await RequestClientVerification(authorizationStatus?.ChallengeData, string.Empty, validatedParameters.TabId, authenticationMethod, !CreditorAuthorizationStarted);
                    CreditorAuthorizationStarted = true;
                }
            }

            EnsureNoErrorMessages(TppMessages);

            await NotifyTransactionState(TransactionState.TransactionInProgress, validatedParameters.TabId);

            await OnFinalized(AuthorizationStatusValue, Id, validatedParameters.TokenId);
        }

        protected abstract Task<(string, AuthenticationMethod, AuthorizationInformation)> StartPaymentAndChoseAuthenticationMethod(ValidationResult validationResult, decimal amount, string currency);
        protected abstract Task<PaymentServiceUserDataResponse> PutUserData(string Id, string AuthorizationId, string AuthenticationMethodId);
        protected abstract Task<AuthorizationStatus> GetAuthorizationStatus(string Id, string AuthorizationId);
        protected abstract Task OnFinalized(AuthorizationStatusValue Status, string Id, CaseInsensitiveString TokenId);
        protected AuthenticationMethod SelectAuthenticationMethod(AuthorizationInformation AuthorizationStatus, bool isFromMobilePhone)
        {
            AuthenticationMethod AuthenticationMethod;
            if (isFromMobilePhone)
            {
                AuthenticationMethod = AuthorizationStatus.GetAuthenticationMethod(AuthenticationMethodId.MBID_SAME_DEVICE)
                    ?? AuthorizationStatus.GetAuthenticationMethod(AuthenticationMethodId.MBID);
            }
            else
            {
                AuthenticationMethod = AuthorizationStatus.GetAuthenticationMethod(AuthenticationMethodId.MBID_ANIMATED_QR_TOKEN)
                    ?? AuthorizationStatus.GetAuthenticationMethod(AuthenticationMethodId.MBID_ANIMATED_QR_IMAGE)
                    ?? AuthorizationStatus.GetAuthenticationMethod(AuthenticationMethodId.MBID)
                    ?? AuthorizationStatus.GetAuthenticationMethod(AuthenticationMethodId.MBID_SAME_DEVICE);
            }

            if (AuthenticationMethod is null)
            {
                throw new Exception("Unable to find a Mobile Bank ID authorization method for the operation.");
            }

            return AuthenticationMethod;
        }

        protected async Task SavePaymentStatusInToken(CaseInsensitiveString TokenId, IList<PaymentOption> payments)
        {
            if (string.IsNullOrEmpty(TokenId) || payments.Count == 0)
            {
                return;
            }

            try
            {
                var json = JsonSerializer.Serialize(payments);
                var xmlNote = $"<SavePaymentStatus xmlns='https://{Gateway.Domain}/Downloads/EscrowPaylinkSE.xsd' paymentStatusObject='{json}' />";

                XmlDocument xmlDocument = new();
                xmlDocument.LoadXml(xmlNote);

                await InternetContent.PostAsync(
                    new Uri("https://" + Gateway.Domain + ":8088/AddNote/" + TokenId),
                    xmlDocument,
                    Gateway.Certificate);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, nameof(OpenPaymentsPlatformService), TokenId);
            }
        }

        protected async Task RequestClientVerification(
               ChallengeData ChallengeData,
               string ScaOAuth,
               string TabId,
               AuthenticationMethod AuthenticationMethod,
               bool shouldRefreshBankIdUrl = true)
        {
            try
            {
                Log.Informational("shouldRefreshBankIdUrl: " + shouldRefreshBankIdUrl);
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
                        string Url = Client.GetClientWebUrl(ScaOAuth,
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
                        await RefreshQrCode(TabId, ChallengeData);
                        break;

                    case AuthenticationMethodId.MBID:
                    case AuthenticationMethodId.MBID_SAME_DEVICE:
                        if (!shouldRefreshBankIdUrl)
                        {
                            return;
                        }

                        await RequestOpenBankIdApp(TabId, ChallengeData);
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

        public static async Task RequestOpenBankIdApp(string TabId, ChallengeData ChallengeData)
        {
            string eventMessage = JSON.Encode(new Dictionary<string, object>()
                            {
                                { "BankIdUrl", ChallengeData.BankIdURL ?? string.Empty},
                                { "AutoStartToken", ChallengeData.AutoStartToken ?? string.Empty},
                                { "MobileAppUrl",  GetMobileAppUrl(null, ChallengeData.AutoStartToken)}
                            }, false);

            Log.Informational(eventMessage);
            await ClientEvents.PushEvent(new string[] { TabId }, "OpenBankIdApp", eventMessage, true);
        }

        public static async Task RefreshQrCode(string TabId, ChallengeData ChallengeData)
        {
            string eventMessage = JSON.Encode(new Dictionary<string, object>()
                            {
                                { "BankIdUrl", ChallengeData.BankIdURL ?? string.Empty},
                                { "MobileAppUrl",  GetMobileAppUrl(null, ChallengeData.AutoStartToken)},
                                { "AutoStartToken", ChallengeData.AutoStartToken ?? string.Empty},
                                { "ImageUrl",ChallengeData.ImageUrl ?? string.Empty},
                                { "title", "Authorize recipient" },
                                { "message", "Scan the following QR-code with your Bank-ID app, or click on it if your Bank-ID is installed on your computer." },
                            }, false);

            Log.Informational(eventMessage);
            await ClientEvents.PushEvent(new string[] { TabId }, "ShowQRCode", eventMessage, true);
        }

        public static string GetMobileAppUrl(string RedirectUrl, string AutoStartToken)
        {
            if (string.IsNullOrEmpty(AutoStartToken))
            {
                return string.Empty;
            }

            StringBuilder sb = new();

            sb.Append("bankid:///?autostarttoken=");
            sb.Append(System.Web.HttpUtility.UrlEncode(AutoStartToken));
            sb.Append("&redirect=");

            if (!string.IsNullOrEmpty(RedirectUrl))
                sb.Append(System.Web.HttpUtility.UrlEncode(RedirectUrl));
            else
                sb.Append("null");

            return sb.ToString();
        }

        public static async Task NotifyTransactionState(TransactionState TransactionState, string tabId, string errorMessage = null)
        {
            if (!string.IsNullOrEmpty(errorMessage))
            {
                Log.Error(new Exception(errorMessage));
            }
            if (string.IsNullOrEmpty(tabId))
            {
                return;
            }

            await ClientEvents.PushEvent(new string[] { tabId }, TransactionState.ToString(),
                  JSON.Encode(new Dictionary<string, object>()
                  {
                      { "ErrorMessage", errorMessage ?? string.Empty }
                  }, false), true);
        }
    }
}
