# SecureInput
The SecureInput sample illustrates the use of Native Messaging APIs that allows an Edge Extension to communicate with a companion Universal Windows Platform (UWP) application. It also include a Desktop Bridge component, which is written in C++ to access functionality that are not available to both the extension and the UWP modern app.

The sample constitute of 5 main components:

1. Test web page
2. Extension
2. UWP hosting AppService in the main app
3. UWP hosting AppService as a background task
4. Win32 exe


## Test web page

The test web page (`SecureInput.html`) illustrates how to configure a website to interact with the content script of an extension. By using custom events, the web page can pass and receive messages from the content script of the extension, thereby allowing user input to be encrypted via the extension.


## Extension

The extension is a simple extension that uses both background and content script. The content script's main functionality is to detect when the user is entering data that needs to be secured. The extension communicate this to the Win32 component via native messaging, and when user is ready to submit the data, the extension will return an encrypted value back to the website.

## UWP hosting AppService in the main app

This is implemented in the project `NativeMessagingHostInProcess`. Native messaging on Microsoft Edge is supported via [AppService](https://msdn.microsoft.com/en-us/windows/uwp/launch-resume/how-to-create-and-consume-an-app-service), a mechanism that allows a UWP app to provide service to another UWP. In this case, Microsoft Edge, as the host of the extension, is brokering the AppService call to the companion UWP that is packaged with the extension.

Note the registration of the AppService in the package.appxmanifest file that is part of the project. The AppService Name specified in the manifest `"NativeMessagingHostInProcessService"` has to match the parameter that is used in the extension's call to [connectNative](https://developer.mozilla.org/en-US/Add-ons/WebExtensions/API/runtime/connectNative) or [sendNativeMessage](https://developer.mozilla.org/en-US/Add-ons/WebExtensions/API/runtime/sendNativeMessage). 

JSON messages from the extension are stringified as a value in the first [KeyValue pair](https://msdn.microsoft.com/en-us/library/windows/apps/5tbh8a42) of the [ValueSet](https://msdn.microsoft.com/library/windows/apps/dn636131) object. 

## UWP hosting AppService in the main app

The only difference between hosting AppService in the main app and in the background task is that a background task has some restrictions. Please refer to the [Guidelines for background tasks](https://msdn.microsoft.com/en-us/windows/uwp/launch-resume/guidelines-for-background-tasks) to determine which option is ideal for your scenario.

## Win32 exe
This component implements the core functionality that uses .NET Framework APIs to access functionality that are not available to UWP applications. However, because the executable is packaged in an AppX package, it will be able to communicate using AppService with the companion UWP application. 


