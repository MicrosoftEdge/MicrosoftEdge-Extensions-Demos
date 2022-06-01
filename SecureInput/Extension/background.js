// Store port for native messaging connections
var portDict = {};

// Listen for messages from content script
browser.runtime.onMessage.addListener(
    function (request, sender, sendResponse) {
        try {
            if (request.type == "connect") {
                connect(sender.tab);
                sendResponse({ success: true });
            } else if (request.type == "disconnect") {
                disconnect(sender.tab);
                sendResponse({ success: true });
            } else {
                sendNativeMessage(request.message, sender.tab);
                document.addEventListener('nativeMessage',
                    function handleNativeMessage(e) {
                        sendResponse(e.detail);
                        document.removeEventListener(e.type, handleNativeMessage);
                    }, false);
            }

            return true;
        }
        catch (e) {
            sendResponse({ success: false, message: e.message })
            return true;
        }
    });

function onDisconnected() {
    // Query current active window
    browser.tabs.query({ active: true, currentWindow: true }, function (tabs) {
        var lastTabId = tabs[0].id;
        browser.tabs.sendMessage(lastTabId, "disconnected");
    });
}

function onNativeMessage(message) {
    if ((message.length == 1) || (message == "delete")) // Handle intercepted key event from native app
    {
        // Query current active window
        browser.tabs.query({ active: true, currentWindow: true }, function (tabs) {
            var lastTabId = tabs[0].id;
            browser.tabs.sendMessage(lastTabId, message);
        })
    }
    else // Handle encrypted password 
    {
        var event = new CustomEvent('nativeMessage', { 'detail': message });
        document.dispatchEvent(event);
    }
}

// Connect to native app and get the communication port  
function connect(tab) {
    var port = null;

    // Connect the App Service in Native App, change to "NativeMessagingHostOutOfProcessService"
    // while switching to App Service out-of-proc model
    port = browser.runtime.connectNative("NativeMessagingHostInProcessService");
    port.onMessage.addListener(onNativeMessage);
    port.onDisconnect.addListener(onDisconnected);

    var id = tab.id;
    portDict[id] = port;
}

// Disconnect from native app
function disconnect(tab) {
    var id = tab.id;
    portDict[id].disconnect();
    delete portDict[id];
}

// Send message to native app
function sendNativeMessage(message, tab) {
    var id = tab.id;
    try {
        portDict[id].postMessage(message);
    }
    catch (e) {
        throw e;
    }
}