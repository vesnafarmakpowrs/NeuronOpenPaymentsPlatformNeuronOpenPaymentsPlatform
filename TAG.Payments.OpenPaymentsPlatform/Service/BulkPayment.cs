using Paiwise;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TAG.Networking.OpenPaymentsPlatform;
using TAG.Payments.OpenPaymentsPlatform.Models;
using Waher.Events;

namespace TAG.Payments.OpenPaymentsPlatform.Service
{
    internal sealed class BulkPayment : Payment
    {
        private string[] OngoingPayments;
        private string BasketId;
        public BulkPayment(OperationInformation operation, OpenPaymentsPlatformClient client, PaymentProduct product,
                object state, string successUrl, ClientUrlEventHandler handler)
            : base(operation, client, product, state, successUrl, handler)
        {
        }

        protected override async Task<AuthorizationStatus> GetAuthorizationStatus(string Id, string AuthorizationId)
        {
            return await Client.GetPaymentBasketAuthorizationStatus(Id, AuthorizationId, Operation);
        }       

        protected override async Task OnFinalized(AuthorizationStatusValue Status, string Id)
        {
            BasketTransactionStatus BasketStatus = await Client.GetPaymentBasketStatus(Id, Operation);
            bool IsPaid = Status == AuthorizationStatusValue.finalised &&
                BasketStatus.Status != PaymentBasketStatus.RJCT;

            Log.Informational("PaymentBasketStatus:" + BasketStatus.Status);

            if (!IsPaid)
            {
                throw new Exception($"Payment not completed. AuthorizationStatusValue: {Status} , BasketStatus: {BasketStatus.Status}");
            }
        }

        protected override async Task<PaymentServiceUserDataResponse> PutUserData(string Id, string AuthorizationId, string AuthenticationMethodId)
        {
            return await Client.PutPaymentBasketUserData(Id, AuthorizationId,
                            AuthenticationMethodId, Operation);
        }

        protected override async Task<(string, AuthenticationMethod, AuthorizationInformation)> StartPaymentAndChoseAuthenticationMethod(ValidationResult validationResult, decimal Amount, string Currency)
        {
            var tasks = new List<Task<PaymentInitiationReference>>();

            foreach (var option in validationResult.SplitPaymentOptions)
            {
                var task = Client.CreatePaymentInitiation(Product, Amount, Currency, validationResult.BankAccount, Currency,
                    option.Iban, Currency, option.AccountName, validationResult.TextMessage, Operation);

                tasks.Add(task);
            }

            var results = await Task.WhenAll(tasks);

            OngoingPayments = results.Select(m => m.PaymentId).ToArray();
            var Basket = await Client.CreatePaymentBasket(OngoingPayments, Operation);

            var AuthorizationStatus = await Client.StartPaymentBasketAuthorization(Basket.BasketId, Operation);
            AuthenticationMethod authenticationMethod = SelectAuthenticationMethod(AuthorizationStatus, validationResult.RequestFromMobilePhone);

            return (Basket.BasketId, authenticationMethod, AuthorizationStatus);
        }
    }
}
