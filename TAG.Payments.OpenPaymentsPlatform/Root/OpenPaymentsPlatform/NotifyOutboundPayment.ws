AuthenticateMutualTls(Request,Waher.Security.Users.Users.Source,128);

({
    "markdown":Required(Str(PMarkdown))
}:=Posted) ??? BadRequest(Exception.Message);

recepientString:= GetSetting("TAG.Payments.OpenPaymentsPlatform.NotificationList", "");
if(System.String.IsNullOrEmpty(recepientString)) then 
(
    Error("No recepients defined.");
);

recepients:= recepientString.Split([";"],  System.StringSplitOptions.RemoveEmptyEntries);
if(recepients.Length == 0) then 
(
   Error("No recepients defined.");
);

foreach (recepient in recepients) do 
(
  try
  (
    XmppServerModule.SendMailMessage(recepient, "Outbound payments for signature", PMarkdown);
  )
  catch
  (
    Log.Error("Unable to send outbound payment to: " + recepient + ". " + Exception.Message, null);
  );
);

{
}