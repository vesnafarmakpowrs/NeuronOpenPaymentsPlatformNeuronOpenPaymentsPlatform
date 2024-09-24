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
        private List<PaymentOption> OngoingPayments;
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
            try
            {
                BasketTransactionStatus BasketStatus = await Client.GetPaymentBasketStatus(Id, Operation);
                bool IsPaid = Status == AuthorizationStatusValue.finalised &&
                    BasketStatus.Status != PaymentBasketStatus.RJCT;

                Log.Informational("PaymentBasketStatus:" + BasketStatus.Status);

                if (!IsPaid)
                {
                    throw new Exception($"Payment not completed. AuthorizationStatusValue: {Status} , BasketStatus: {BasketStatus.Status}");
                }

                if (OngoingPayments.Any() != true)
                {
                    throw new Exception("Ongoing payments not populated properly.");
                }

                await Parallel.ForEachAsync(OngoingPayments, async (Payment, cancellationToken) =>
                {
                    var paymentStatus = await Client.GetPaymentInitiationStatus(Product, Payment.PaymentId, Operation);
                    Payment.Status = paymentStatus.Status;

                    if (paymentStatus?.Messages?.Length > 0)
                    {
                        Payment.ErrorMessage = string.Join(Environment.NewLine, paymentStatus.Messages.Select(m => m.Text));
                    }
                });

                if (OngoingPayments.Any(transaction =>
                        transaction.Status == PaymentStatus.RJCT || transaction.Status == PaymentStatus.CANC || !string.IsNullOrEmpty(transaction.ErrorMessage)))
                {
                    throw new Exception("One ore more transactions are not completed. Check the logs.");
                }
            }
            finally
            {
                BasketId = string.Empty;
                OngoingPayments = null;
            }
        }

        protected override async Task<PaymentServiceUserDataResponse> PutUserData(string Id, string AuthorizationId, string AuthenticationMethodId)
        {
            return await Client.PutPaymentBasketUserData(Id, AuthorizationId,
                            AuthenticationMethodId, Operation);
        }

        protected override async Task<(string, AuthenticationMethod, AuthorizationInformation)> StartPaymentAndChoseAuthenticationMethod(ValidationResult validationResult, decimal Amount, string Currency)
        {
            await Parallel.ForEachAsync(validationResult.SplitPaymentOptions, async (option, cancellationToken) =>
             {
                 var paymentInitiation = await Client.CreatePaymentInitiation(Product, option.Amount, Currency, validationResult.BankAccount, Currency,
                     option.BankAccount, Currency, option.AccountName, option.Description, Operation);

                 option.PaymentId = paymentInitiation.PaymentId;
             });

            var paymentIds = validationResult.SplitPaymentOptions.Select(m => m.PaymentId).ToArray();
            var Basket = await Client.CreatePaymentBasket(paymentIds, Operation);

            BasketId = Basket.BasketId;
            OngoingPayments = validationResult.SplitPaymentOptions;

            var AuthorizationStatus = await Client.StartPaymentBasketAuthorization(Basket.BasketId, Operation);
            AuthenticationMethod authenticationMethod = SelectAuthenticationMethod(AuthorizationStatus, validationResult.RequestFromMobilePhone);

            return (Basket.BasketId, authenticationMethod, AuthorizationStatus);
        }
    }
}
