URL:=Waher.Service.IoTBroker.EDaler.EDalerComponent.GenerateIssueUrl(
	To,
	Amount,
	Currency,
	ExpiresDays,
	FreeText,
	ManagerPassword,
	Request,
	User);

Msg:=Waher.Service.IoTBroker.XmppServerModule.Instance.EDaler.ProcessUri(URL,To);

if !empty(Msg) then error(Msg);