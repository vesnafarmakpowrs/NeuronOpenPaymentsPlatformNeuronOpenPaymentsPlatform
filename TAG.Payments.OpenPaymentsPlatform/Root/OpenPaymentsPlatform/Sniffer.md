Title: Open Payments Platform Sniffer
Description: Allows the user to view Open Payments Platform communication.
Date: 2023-05-03
Author: Peter Waher
Master: /Master.md
JavaScript: /Events.js
JavaScript: /Sniffers/Sniffer.js
CSS: /Sniffers/Sniffer.css
UserVariable: User
Privilege: Admin.Communication.Sniffer
Privilege: Admin.Communication.OpenPaymentsPlatform
Login: /Login.md
Parameter: SnifferId

========================================================================

Open Payments Platform Communication
=======================================

On this page, you can follow the Open Payments Platform API communication made from the 
machine to the Open Payments Platform back-end. The sniffer will automatically be 
terminated after some time to avoid performance degradation and leaks. Sniffers should 
only be used as a tool for troubleshooting.

{{
TAG.Payments.OpenPaymentsPlatform.OpenPaymentsPlatformServiceProvider.RegisterSniffer(SnifferId,Request,"User",["Admin.Communication.Sniffer"])
}}
