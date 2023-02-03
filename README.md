# Cisco Authenticaton Handler

This code is meant to be used alongside the Crestron _Cisco Room Devices_ module, to allow for sending pin codes for webex meetings and webinars. It parses responses from the Cisco to determind when authentication is needed. It handles building the pin code from a keypad, and sending the command to the Cisco device when the user submits the code. It will allow you to show a list if needed, and the user can pick between different options, for instance Host vs Guest. It supports joining as a guest with no pin code where avaliable.

## Use
Note: This applies when using SSH communications. The process will be slightly different when using RS-232

 - Hook `VtcRx` on the _ICD.CiscoAuthenticationHandler_ module to `SSH_FromDevice` on the _Cisco Room Devices_ module
 - Hook `VtcTx` on the _ICD.CiscoAuthenticationHandler_ module to `SSH_CustomCommandToDevice`on the _Cisco Room Devices_ module
 - Pulse `SendSubscribe` on the module once the SSH session is connected. Recommended to add a delay to wait for the _Cisco Room Devices_ module to finish it's instantiation
 - Hook the inputs/outputs of the module to your UI
 - For the keypad - the order of the buttons is designed to line up with the order when using the _Digital Press Start Join_ from a Simple Keypad in VTPro.
   - `KeypadPress[n]` for n=1 through n=10 correspond to n - 1 - for example `KeypadPress[1]` is 0, and `KeypadPress[10]` is 9
   - `KeypadPress[11]` is asterisk
   - `KeypadPress[12]` is the number sign/hash symbol
