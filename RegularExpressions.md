Regular expressions
========================

Countries supported by the Open Payments Platform (at time of writing):

```json
    	"countries": [
		{
			"isoCountryCode": "SE",
			"name": "Sweden"
		},
		{
			"isoCountryCode": "FI",
			"name": "Finland"
		},
		{
			"isoCountryCode": "DE",
			"name": "Germany"
		},
		{
			"isoCountryCode": "DK",
			"name": "Denmark"
		},
		{
			"isoCountryCode": "UK",
			"name": "United Kingdom"
		},
		{
			"isoCountryCode": "NO",
			"name": "Norway"
		}
	]
```

Source for BBAN and IBAN formats (Swift IBAN Registry):  
https://www.swift.com/swift-resource/9606/download

BIC
-----

Reference: [ISO 9362 on Wikipedia](https://en.wikipedia.org/wiki/ISO_9362)

Regular Expression:

`[A-Z]{4}(SE|FI|DE|DK|GB|NO)[A-Z0-9]{2}([A-Z0-9]{3})?`


IBAN By Country
--------------

### Sweden

IBAN structure:  
`SE2!n3!n16!n1!n`

IBAN Regular expression:  
`SE\s*\d{2}\s*\d{3}\s*\d{16}\s*\d`

### Finland

IBAN structure:  
`FI2!n3!n11!n`

IBAN Regular expression:  
`FI\s*\d{2}\s*\d{3}\s*\d{11}`

### Germany

IBAN structure:  
`DE2!n8!n10!n`

IBAN Regular expression:  
`DE\s*\d{2}\s*\d{8}\s*\d{10}`

### Denmark

IBAN structure:  
`DK2!n4!n9!n1!n`

IBAN Regular expression:  
`DK\s*\d{2}\s*\d{4}\s*\d{9}\s*\d`

### United Kingdom

IBAN structure:  
`GB2!n4!a6!n8!n`

IBAN Regular expression:  
`GB\s*\d{2}\s*[A-Z]{4}\s*\d{6}\s*\d{8}`

### Norway

IBAN structure:  
`NO2!n4!n6!n1!n`

IBAN Regular expression:  
`NO\s*\d{2}\s*\d{4}\s*\d{6}\s*\d`

All supported countries
-------------------------

IBAN Regular expression:  

`^(SE\s*\d{2}\s*\d{3}\s*\d{16}\s*\d)|(FI\s*\d{2}\s*\d{3}\s*\d{11})|(DE\s*\d{2}\s*\d{8}\s*\d{10})|(DK\s*\d{2}\s*\d{4}\s*\d{9}\s*\d)|(GB\s*\d{2}\s*[A-Z]{4}\s*\d{6}\s*\d{8})|(NO\s*\d{2}\s*\d{4}\s*\d{6}\s*\d)$`
