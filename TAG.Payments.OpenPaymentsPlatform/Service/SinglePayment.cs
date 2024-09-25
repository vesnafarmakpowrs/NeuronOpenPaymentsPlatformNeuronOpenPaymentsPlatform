using Paiwise;
using System;
using System.Linq;
using System.Threading.Tasks;
using TAG.Networking.OpenPaymentsPlatform;
using TAG.Payments.OpenPaymentsPlatform.Models;
using Waher.Persistence;
using Waher.Script.Operators.Arithmetics;

namespace TAG.Payments.OpenPaymentsPlatform.Service
{
    internal class SinglePayment : Payment
    {
        public SinglePayment(OperationInformation operation, OpenPaymentsPlatformClient client, 
            PaymentProduct product, object state, string successUrl, ClientUrlEventHandler clientUrlCallback)
            : base(operation, client, product, state, successUrl, clientUrlCallback)
        {
        }

        protected override async Task<AuthorizationStatus> GetAuthorizationStatus(string Id, string AuthorizationId)
        {
            return await Client.GetPaymentInitiationAuthorizationStatus(Product, Id, AuthorizationId, Operation);
        }

        protected override async Task OnFinalized(AuthorizationStatusValue Status, string Id, CaseInsensitiveString TokenId)
        {
            PaymentTransactionStatus TransactionStatus = await Client.GetPaymentInitiationStatus(Product, Id, Operation);

            if (TransactionStatus.Messages is not null && TransactionStatus.Messages.Length > 0)
            {
                var errorMessages = string.Join(Environment.NewLine, TransactionStatus.Messages.Select(m => m.Text));
                throw new Exception(errorMessages);
            }

            if (TransactionStatus.Status == PaymentStatus.RJCT)
            {
                throw new Exception("Payment was rejected.");
            }

            if (TransactionStatus.Status == PaymentStatus.CANC)
            {
                throw new Exception("Payment was cancelled.");
            }

            switch (Status)
            {
                case AuthorizationStatusValue.failed:
                    throw new Exception($"Payment failed. ({Status})");

                case AuthorizationStatusValue.finalised:
                    return;

                default:
                    throw new Exception("Transaction took too long to complete.");
            }
        }

        protected override async Task<PaymentServiceUserDataResponse> PutUserData(string Id, string AuthorizationId, string AuthenticationMethodId)
        {
            return await Client.PutPaymentInitiationUserData(Product, Id, AuthorizationId, AuthenticationMethodId, Operation);
        }

        protected override async Task<(string, AuthenticationMethod, AuthorizationInformation)> StartPaymentAndChoseAuthenticationMethod(ValidationResult validationResult, decimal amount, string currency)
        {
            var Configuration = await ServiceConfiguration.GetCurrent();

            PaymentInitiationReference PaymentInitiationReference = await Client.CreatePaymentInitiation(
            Product, amount, currency, validationResult.BankAccount, currency,
            Configuration.NeuronBankAccountIban, currency,
            Configuration.NeuronBankAccountName, validationResult.TextMessage, Operation);

            AuthorizationInformation AuthorizationStatus = await Client.StartPaymentInitiationAuthorization(
                       Product, PaymentInitiationReference.PaymentId, Operation,
                       SuccessUrl, string.Empty);

            AuthenticationMethod authenticationMethod = SelectAuthenticationMethod(AuthorizationStatus, validationResult.RequestFromMobilePhone);

            return (PaymentInitiationReference.PaymentId, authenticationMethod, AuthorizationStatus);
        }
    }
}
