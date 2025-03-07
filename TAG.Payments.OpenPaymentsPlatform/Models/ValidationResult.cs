﻿using System.Collections.Generic;
using TAG.Networking.OpenPaymentsPlatform;
using Waher.Persistence;

namespace TAG.Payments.OpenPaymentsPlatform.Models
{
    public class ValidationResult
    {
        public CaseInsensitiveString TokenId { get; set; }
        public CaseInsensitiveString PersonalNumber { get; set; }
        public string BankAccount { get; set; }
        public string AccountName { get; set; }
        public string TextMessage { get; set; }
        public string TabId { get; set; }
        public string CallBackUrl { get; set; }
        public bool RequestFromMobilePhone { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsInstantPayment { get; set; }
        public List<PaymentOption> SplitPaymentOptions { get; set; } = new List<PaymentOption>();
    }
}
