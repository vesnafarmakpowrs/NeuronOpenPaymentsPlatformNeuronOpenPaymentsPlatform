Title: Pending Payments
Description: Shows pending payments that need to be signed by the operator
Date: 2023-03-20
Author: Peter Waher
Master: /Master.md
Cache-Control: max-age=0, no-cache, no-store
UserVariable: User
Privilege: Admin.Payments.Paiwise.OpenPaymentsPlatform
Login: /Login.md
JavaScript: OutgoingPayments.js
JavaScript: /Events.js

========================================================================

Pending Payments
==================

Following is a list of payments that are pending approval from the server operator. Select the payments you wish to sign, and
press the **Sign** button. Note that, depending on your bank, the number of payments you can select is limited. Most banks support
payment baskets of **99** payments however.

<form>

| Payment ID | Created  || Updated || Status | Product | Amount | Currency | Account | Bank | Bank Account | Name | Message |
|:-----------|:----:|:---|:---|:----|:-------|:--------|-------:|:---------|:--------|:-----|:-------------|:-----|:--------|
{{
foreach Rec in select * from OPP_OutboundPayments where Paid=System.DateTime.MinValue order by Created do
(
	]]| <input type="checkbox" id='P((Rec.ObjectId))' name='P((Rec.ObjectId))'/><label for='P((Rec.ObjectId))'>((Rec.PaymentId))</label> | ((Rec.Created.Date.ToShortDateString() )) | ((Rec.Created.ToLongTimeString() )) | ((Rec.Updated.Date.ToShortDateString() )) | ((Rec.Updated.ToLongTimeString() )) | ((Rec.TransactionStatus)) | ((Rec.Product)) | ((Rec.Amount)) | ((Rec.Currency)) | ((before(Rec.Account,"@") )) | ((Rec.ToBank)) | ((Rec.ToBankAccount)) | ((Rec.ToBankAccountName)) | ((Rec.TextMessage)) |
[[;
)
}}

<button type="button" onclick="SelectAll()" class="posButton">Select All</button>
<button type="button" onclick="Select99()" class="posButton">Select 99</button>
<button type="button" onclick="DeselectAll()" class="negButton">Deselect All</button>
<button type="button" onclick="SignSelected()" class="posButton">Sign Selected</button>
<button type="button" onclick="ReturnSelected()" class="negButton">Return Selected</button>
<button type="button" onclick="RetrySelected()" class="posButton">Retry Selected</button>

<div id="QrCode"></div>
</form>	
