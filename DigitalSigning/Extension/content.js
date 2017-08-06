// Connect native app automatically when page loading
window.onload = connectNativeApp;

// Disconnect native app automatically when page unloading
window.onbeforeunload = disconnectNativeApp;

function updateUiState(connected) {
    if (connected) {
        document.getElementById('ConnectButton').disabled = true;
        document.getElementById('DisconnectButton').disabled = false;
        document.getElementById('SignButton').disabled = false;
    } else {
        document.getElementById('ConnectButton').disabled = false;
        document.getElementById('DisconnectButton').disabled = true;
        document.getElementById('SignButton').disabled = true;
    }
}

function connectNativeApp() {
    var request = new Object();
    request.type = "connect";

    browser.runtime.sendMessage(request,
        function (response) {
            if (response.success) {
                updateUiState(true);
            } else {
                console.error(response.message);
            }
        });
}

function disconnectNativeApp() {
    var request = new Object();
    request.type = "disconnect";

    browser.runtime.sendMessage(request,
        function (response) {
            if (response.success) {
                updateUiState(false);
            } else {
                console.error(response.message);
            }
        });
}

// Sign message
function SignMessage(message) {
    var request = new Object();
    request.type = "SignMessage";
    request.message = message;

    browser.runtime.sendMessage(request,
        function (response) {
            var event = new CustomEvent('SignResponse', { 'detail': response });
            document.dispatchEvent(event);
        });
}

browser.runtime.onMessage.addListener(function (message) {
    if (message == "disconnected") {
        updateUiState(false);
    }
});

document.getElementById('ConnectButton').onclick = connectNativeApp;
document.getElementById('DisconnectButton').onclick = disconnectNativeApp;

document.addEventListener('SignRequest',
    function (e) {
        SignMessage(e.detail);
    }, false);