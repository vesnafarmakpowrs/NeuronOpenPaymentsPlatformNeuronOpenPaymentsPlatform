const isMobileDevice = window.navigator.userAgent.toLowerCase().includes("mobi");
function SelectAll()
{
	SetCheck(true, 1e9);
}

function Select99()
{
	SetCheck(true, 99);
}

function SetCheck(Value, MaxNr)
{
	var Inputs = document.getElementsByTagName("INPUT");
	var c = Inputs.length;
	var i;
	var Nr = 0;

	for (i = 0; i < c; i++)
	{
		var Input = Inputs[i];

		if (Input.checked !== Value)
		{
			Input.checked = Value;
			if (++Nr >= MaxNr)
			break;
		}
	}

	return Nr;
}

function DeselectAll()
{
	SetCheck(false, 1e9);
}

function SignSelected()
{
	if (!confirm("Are you sure you want to sign the selected payments? Money will be requested to be transferred accordingly."))
		return;

	var Nr = ProcessSelected("SignPayments", false);

	if (Nr === 0)
		window.alert("No payments selected to sign.");
}

function ReturnSelected()
{
	var Password = window.prompt("To return the selected payments, eDaler will be generated and returned to the corresponding users. You need to provide your password again to confirm:", "");
	if (!Password)
		return;

	var Nr = ProcessSelected("ReturnPayments", Password);

	if (Nr === 0)
		window.alert("No payments selected to return.");
}

function RetrySelected()
{
	if (!confirm("Are you sure you want to create new payment objects for the selected payments? Use this option if selected payments have failed."))
		return;

	var Nr = ProcessSelected("RetryPayments", false);

	if (Nr === 0)
		window.alert("No payments selected to retry.");
}

function ProcessSelected(CommandResource, Password)
{
	var Inputs = document.getElementsByTagName("INPUT");
	var ObjectIds = [];
	var SelectedInputs = [];
	var c = Inputs.length;
	var i;
	var Nr = 0;

	for (i = 0; i < c; i++)
	{
		var Input = Inputs[i];

		if (Input.checked && !Input.disabled)
		{
			SelectedInputs.push(Input);
			ObjectIds.push(Input.id.substring(1));
			Nr++;
			Input.disabled = true;
		}
	}

	if (Nr === 0)
		return 0;

	var xhttp = new XMLHttpRequest();
	xhttp.onreadystatechange = function ()
	{
		if (xhttp.readyState == 4)
		{
			var Div = document.getElementById("QrCode");
			Div.innerHTML = "";

			if (xhttp.status !== 200)
			{
				window.alert(xhttp.responseText);

				for (i = 0; i < Nr; i++)
					Inputs[i].disabled = false;
			}
		}
	};

	var Request =
	{
		"objectIds": ObjectIds,
		"tabId": TabID,
		"requestFromMobilePhone": Boolean(isMobileDevice)
	};

	if (Password)
		Request.password = Password;

	xhttp.open("POST", CommandResource, true);
	xhttp.setRequestHeader("Content-Type", "application/json");
	xhttp.send(JSON.stringify(Request));

	return Nr;
}

function PaymentUpdated(Data)
{
	var Input = document.getElementById("P" + Data.objectId);
	if (Input)
	{
		var Td = Input.parentElement;
		var Tr = Td.parentElement;
		var i = 0;
		var Loop = Tr.firstChild;

		if (Data.isPaid)
		{
			Td.removeChild(Input);
			Tr.setAttribute("style", "text-decoration: line-through");

			var Div = document.getElementById("QrCode");
			Div.innerHTML = "";
		}
		else
			Input.disabled = false;

		while (Loop)
		{
			if (Loop.tagName === "TD")
			{
				switch (++i)
				{
					case 4:
						Loop.innerText = Data.updatedDate;
						break;

					case 5:
						Loop.innerText = Data.updatedTime;
						break;

					case 6:
						Loop.innerText = Data.status;
						break;
				}
			}

			Loop = Loop.nextSibling;
		}
	}
}

function PaymentRetried(Data)
{
	var Input = document.getElementById("P" + Data.objectId);
	if (Input)
	{
		var Td = Input.parentElement;
		var Tr = Td.parentElement;
		var i = 0;
		var Loop = Tr.firstChild;

		Input.disabled = false;

		while (Loop)
		{
			if (Loop.tagName === "TD")
			{
				switch (++i)
				{
					case 1:
						Loop.innerText = Data.paymentId;
						break;

					case 4:
						Loop.innerText = Data.updatedDate;
						break;

					case 5:
						Loop.innerText = Data.updatedTime;
						break;

					case 6:
						Loop.innerText = Data.status;
						break;
				}
			}

			Loop = Loop.nextSibling;
		}
	}
}

function OpenUrl(Url)
{
	var Window = window.open(Url, "_blank");
	Window.focus();
}

function ShowQRCode(Data) {
	var Div = document.getElementById("QrCode");

	if (Data.ImageUrl) {
		Div.innerHTML = "<fieldset><legend>" + Data.title + "</legend><p>" + Data.message +
			"</p><p><img class='QrCodeImage' alt='Bank ID QR Code' src='" + Data.ImageUrl + "'/></p></fieldset>";
	}
	else if (Data.AutoStartToken) {
		Div.innerHTML = "<fieldset><legend>" + Data.title + "</legend><p>" + Data.message +
			"</p><p>" + "<a href='" + Data.AutoStartToken + "'><img alt='Bank ID QR Code' src='/QR/" +
			encodeURIComponent(Data.AutoStartToken) + "'/></a></p></fieldset>";
	}
}


function OpenBankIdApp(Data) {
	if (Data == null) {
		console.log("data is empty");
		return;
	}

	var link = Data.BankIdUrl;
	var mode = "_blank";
	var isIos = /^((?!chrome|android).)*safari/i.test(navigator.userAgent);

	if (isIos) {
		link = Data.MobileAppUrl;
		mode = "_self";
		let chromeAgent = navigator.userAgent.indexOf("Chrome") > -1;
		if (!chromeAgent) {
			link = link.replace('redirect=null', 'redirect=');
		}
	}
	window.open(link, mode);
}


function PaymentError(Data)
{
	var Div = document.getElementById("QrCode");
	Div.innerHTML = "<fieldset><legend>Error</legend><p>" + Data + "</p></fieldset>";
}