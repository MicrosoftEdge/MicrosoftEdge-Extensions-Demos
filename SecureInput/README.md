# SecureInput
The SecureInput sample illustrates the use of Native Messaging APIs that allows an Edge extension to communicate with a companion Universal Windows Platform (UWP) application. It also include a [Desktop Bridge](https://developer.microsoft.com/windows/bridges/desktop) (Win32) component, which is written in C# to access functionality that isn't available to the extension or the UWP app.

This sample has five main components:

1. Test web page
2. Edge extension
2. UWP hosting `AppService` in the main app
3. UWP hosting `AppService` as a background task
4. Desktop Bridge exe


## Test web page

The test web page ([`SecureInput.html`](./SecureInput.html)) illustrates how to configure a website to interact with the content script of an extension. By using custom events, the web page can pass and receive messages from the content script of the extension, thereby allowing user input to be encrypted via the extension.


## Edge extension

The extension is a basic extension that uses both a background and content script. The content script's main functionality is to detect when the user is entering data that needs to be secured. The extension communicates this to the Desktop Bridge component via native messaging. When the user is ready to submit the data, the extension will return an encrypted value back to the website.

## UWP hosting AppService in the main app

By default this sample is setup to have the native messaging host run in the main app. This is implemented in the project's `NativeMessagingHostInProcess`. Native messaging on Edge is supported via [`AppService`](https://msdn.microsoft.com/windows/uwp/launch-resume/how-to-create-and-consume-an-app-service), a mechanism that allows a UWP app to provide service to another UWP app. In this case, Edge, as the host of the extension, is brokering the `AppService` call to the companion UWP that is packaged with the extension.

Note the registration of the `AppService` in the `package.appxmanifest` file that is part of the project. The AppService Name specified in the manifest `"NativeMessagingHostInProcessService"` has to match the parameter that is used in the extension's call to [`runtime.connectNative`](https://developer.mozilla.org/Add-ons/WebExtensions/API/runtime/connectNative) or [`runtime.sendNativeMessage`](https://developer.mozilla.org/en-US/Add-ons/WebExtensions/API/runtime/sendNativeMessage). 

JSON messages from the extension are stringified as a value in the first [KeyValue pair](https://msdn.microsoft.com//library/windows/apps/5tbh8a42) of the [ValueSet](https://msdn.microsoft.com/library/windows/apps/dn636131) object. 

## UWP hosting AppService in the background task

The only difference between hosting `AppService` in the main app and in the background task is that a background task has a few restrictions. See [Guidelines for background tasks](https://msdn.microsoft.com/windows/uwp/launch-resume/guidelines-for-background-tasks) to determine which option is ideal for your scenario.

To see the background task implementation of the native messaging host, you'll need to edit the extension's background script by changing the string within `port = browser.runtime.connectNative("NativeMessagingHostInProcessService");` to `"NativeMessagingHostOutOfProcess"`.

## Desktop Bridge
This component implements the core functionality that uses .NET Framework APIs to access functionality that are not available to UWP applications. However, because the executable is packaged in an AppX package, it will be able to communicate using `AppService` with the companion UWP application. 

To get started with converting a Win32 to Desktop Bridge app, head [here](https://msdn.microsoft.com/windows/uwp/porting/desktop-to-uwp-run-desktop-app-converter).

