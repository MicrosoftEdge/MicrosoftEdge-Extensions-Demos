# Color Changer

This Microsoft Edge extension sample shows how to create an extension for Microsoft Edge.  Use the tutorial [Creating a Microsoft Edge extension](https://docs.microsoft.com/microsoft-edge/extensions/guides/creating-an-extension) with this sample. 

## Behavior 

This example extension will allow you to manipulate specific CSS for [docs.microsoft.com](https://docs.microsoft.com) pages, changing the header color to red, blue, and back to the original color. 

The sample was intended to be used with the [Creating a Microsoft Edge extension](https://docs.microsoft.com/microsoft-edge/extensions/guides/creating-an-extension) guide. 

## APIs used
* [`tabs.insertCSS()`](https://developer.mozilla.org/Add-ons/WebExtensions/API/tabs/insertCSS)
* [`runtime.sendMessage()`](https://developer.mozilla.org/Add-ons/WebExtensions/API/runtime/sendMessage)
* [`runtime.onMessage`](https://developer.mozilla.org/Add-ons/WebExtensions/API/runtime/onmessage)
* [`browserAction.setIcon()`](https://developer.mozilla.org/Add-ons/WebExtensions/API/browserAction/setIcon)
* [`browserAction.disable`](https://developer.mozilla.org/Add-ons/WebExtensions/API/browserAction/disable)
