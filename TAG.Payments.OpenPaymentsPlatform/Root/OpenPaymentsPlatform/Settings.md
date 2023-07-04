Title: Open Payments Platform settings
Description: Configures integration with the Open Payments Platform backend payment service.
Date: 2023-03-15
Author: Peter Waher
Master: /Master.md
Cache-Control: max-age=0, no-cache, no-store
UserVariable: User
Privilege: Admin.Payments.Paiwise.OpenPaymentsPlatform
Login: /Login.md
JavaScript: Settings.js
JavaScript: /Sniffers/Sniffer.js

========================================================================

<form action="Settings.md" method="post" enctype="multipart/form-data">
<fieldset>
<legend>Open Payments Platform settings</legend>

The following settings are required by the integration of the neuron with the Open Payments Platform open banking service backend. 
By providing such an integration, direct bank payments can be performed on the neuron, allowing end-users to buy and sell eDaler(R).

{{
ModeEnum:=TAG.Payments.OpenPaymentsPlatform.OperationMode;
FlowEnum:=TAG.Networking.OpenPaymentsPlatform.AuthorizationFlow;

if exists(Posted) then
(
	CertError:=null;

	SetSetting("TAG.Payments.OpenPaymentsPlatform.ClientID",Posted.ClientID);
	SetSetting("TAG.Payments.OpenPaymentsPlatform.ClientSecret",Posted.ClientSecret);
	SetSetting("TAG.Payments.OpenPaymentsPlatform.Mode",System.Enum.Parse(ModeEnum,Posted.Mode));
	SetSetting("TAG.Payments.OpenPaymentsPlatform.Flow",System.Enum.Parse(FlowEnum,Posted.Flow));
	SetSetting("TAG.Payments.OpenPaymentsPlatform.Account",Posted.Account);
	SetSetting("TAG.Payments.OpenPaymentsPlatform.AccountName",Posted.AccountName);
	SetSetting("TAG.Payments.OpenPaymentsPlatform.AccountBank",Posted.AccountBank);
	SetSetting("TAG.Payments.OpenPaymentsPlatform.PersonalID",Posted.PersonalID);
	SetSetting("TAG.Payments.OpenPaymentsPlatform.OrganizationID",Posted.OrganizationID);
	SetSetting("TAG.Payments.OpenPaymentsPlatform.PollingIntervalSeconds",Num(Posted.PollingIntervalSeconds));
	SetSetting("TAG.Payments.OpenPaymentsPlatform.TimeoutMinutes",Num(Posted.TimeoutMinutes));
	SetSetting("TAG.Payments.OpenPaymentsPlatform.NotificationList",Posted.NotificationList);

	if exists(Posted.Certificate_Binary) and Posted.Certificate_Binary.Length>0 then
	(	
		try
		(
			Cert:=Create(System.Security.Cryptography.X509Certificates.X509Certificate2,Posted.Certificate_Binary,Posted.CertificatePassword);

			SetSetting("TAG.Payments.OpenPaymentsPlatform.Certificate",Base64Encode(Posted.Certificate_Binary));
			SetSetting("TAG.Payments.OpenPaymentsPlatform.CertificatePassword",Posted.CertificatePassword);
		)
		catch
		(
			CertError:=Exception.Message;
		)
	);

	TAG.Payments.OpenPaymentsPlatform.ServiceConfiguration.InvalidateCurrent();

	SeeOther("Settings.md");
);
}}


<p>
<label for="ClientID">Client ID:</label>  
<input type="text" id="ClientID" name="ClientID" value='{{GetSetting("TAG.Payments.OpenPaymentsPlatform.ClientID","")}}' autofocus required title="Client ID identifying the Trust Provider in the Open Payments Platform backend."/>
</p>

<p>
<label for="ClientSecret">Secret:</label>  
<input type="password" id="ClientSecret" name="ClientSecret" value='{{GetSetting("TAG.Payments.OpenPaymentsPlatform.ClientSecret","")}}' required title="Secret used to authenticate the Trust Provider with the backend."/>
</p>

<p>
<label for="Mode">Mode:</label>  
<select name="Mode" id="Mode" required>
<option value="Sandbox"{{Mode:=GetSetting("TAG.Payments.OpenPaymentsPlatform.Mode",ModeEnum.Sandbox); Mode=ModeEnum.Sandbox?" selected" : ""}}>Sandbox</option>
<option value="Production"{{Mode=ModeEnum.Production?" selected" : ""}}>Production</option>
</select>
</p>

<p>
<label for="Flow">Authorization Flow:</label>  
<select name="Flow" id="Flow" required>
<option value="Redirect"{{Flow:=GetSetting("TAG.Payments.OpenPaymentsPlatform.Flow",FlowEnum.Redirect); Flow=FlowEnum.Redirect?" selected" : ""}}>Prefer web redirection</option>
<option value="Decoupled"{{Flow=FlowEnum.Decoupled?" selected" : ""}}>Prefer smart contract</option>
</select>
</p>

<p>
<label for="Account">Bank Account: (IBAN)</label>  
<input type="text" id="Account" name="Account" value='{{GetSetting("TAG.Payments.OpenPaymentsPlatform.Account","")}}' required title="Bank Account of Trust Provider to hold client funds. Must be an IBAN bank account number." pattern="^(SE\s*\d{2}\s*\d{3}\s*\d{16}\s*\d)|(FI\s*\d{2}\s*\d{3}\s*\d{11})|(DE\s*\d{2}\s*\d{8}\s*\d{10})|(DK\s*\d{2}\s*\d{4}\s*\d{9}\s*\d)|(GB\s*\d{2}\s*[A-Z]{4}\s*\d{6}\s*\d{8})|(NO\s*\d{2}\s*\d{4}\s*\d{6}\s*\d)$"/>
</p>

<p>
<label for="AccountName">Name of account:</label>  
<input type="text" id="AccountName" name="AccountName" value='{{GetSetting("TAG.Payments.OpenPaymentsPlatform.AccountName","")}}' required title="Name of bank account."/>
</p>

<p>
<label for="AccountBank">Bank: (BIC)</label>  
<input type="text" id="AccountBank" name="AccountBank" value='{{GetSetting("TAG.Payments.OpenPaymentsPlatform.AccountBank","")}}' required title="Bank hosting the bank account. Must be a BIC bank identifier." pattern="^[A-Z]{4}(SE|FI|DE|DK|GB|NO)[A-Z0-9]{2}([A-Z0-9]{3})?$"/>
</p>

<p>
<label for="PersonalID">Personal ID: (This person will authorize access to account.)</label>  
<input type="text" id="PersonalID" name="PersonalID" value='{{GetSetting("TAG.Payments.OpenPaymentsPlatform.PersonalID","")}}' required title="Personal number of person that will authenticate payments made from the bank account."/>
</p>

<p>
<label for="OrganizationID">Organization ID: (Optional. Organization owning the account, if not the person above.)</label>  
<input type="text" id="OrganizationID" name="OrganizationID" value='{{GetSetting("TAG.Payments.OpenPaymentsPlatform.OrganizationID","")}}' title="Personal number of organization owning the account. Person above needs to be authorized to use the account."/>
</p>

<p>
<label for="Certificate">Certificate:</label>  
<input id="Certificate" name="Certificate" type="file" title="Certificate for authenticating service with Open Payments Platform backend." accept="*/*"/>
</p>

{{if exists(CertError) and !empty(CertError) then ]]
<p class="error">
((MarkdownEncode(CertError) ))
</p>
[[}}

<p>
<label for="CertificatePassword">Password:</label>  
<input type="password" id="CertificatePassword" name="CertificatePassword" value='{{GetSetting("TAG.Payments.OpenPaymentsPlatform.CertificatePassword","")}}' required title="Password for the certificate."/>
</p>

<p>
<label for="PollingIntervalSeconds">Polling interval: (seconds)</label>  
<input type="number" id="PollingIntervalSeconds" name="PollingIntervalSeconds" min="1" max="60" step="1" value='{{GetSetting("TAG.Payments.OpenPaymentsPlatform.PollingIntervalSeconds",2)}}' required title="Interval (in seconds) with which to check the status of an ongoing request."/>
</p>

<p>
<label for="TimeoutMinutes">Timeout: (minutes)</label>  
<input type="number" id="TimeoutMinutes" name="TimeoutMinutes" min="5" max="60" value='{{GetSetting("TAG.Payments.OpenPaymentsPlatform.TimeoutMinutes",5)}}' required title="Maximum amount of time to wait (in minutes) before cancelling an open banking request."/>
</p>

<p>
<label for="NotificationList">Notification recipients for payment authorizations:</label>  
<input type="text" id="NotificationList" name="NotificationList" value='{{GetSetting("TAG.Payments.OpenPaymentsPlatform.NotificationList","")}}' title="Can be XMPP Addresses or e-mail addresses. Separate using semicolon if more than one."/>
</p>

<button type="submit" class="posButton">Apply</button>
</fieldset>

<fieldset>
<legend>Tools</legend>
<button type="button" class="posButton" onclick="OpenPage('OutgoingPayments.md')">Outgoing Payments</button>
<button type="button" class="posButton"{{
if User.HasPrivilege("Admin.Communication.OpenPaymentsPlatform") and User.HasPrivilege("Admin.Communication.Sniffer") then
	" onclick=\"OpenSniffer('Sniffer.md')\""
else
	" disabled"
}}>Sniffer</button>
</fieldset>
</form>
