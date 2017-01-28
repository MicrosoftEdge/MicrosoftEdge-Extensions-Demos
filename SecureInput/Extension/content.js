// Connect native app automatically when page is loaded
window.onload = connectNativeApp;

// Disconnect native app automatically when page is unloaded
window.onbeforeunload = disconnectNativeApp;

// Update UI element enable state depending on connectivity to UWP
function updateUiState(connected) {
    if (connected) {
        document.getElementById('ConnectButton').disabled = true;
        document.getElementById('DisconnectButton').disabled = false;
        document.getElementById('SubmitButton').disabled = false;
    } else {
        document.getElementById('ConnectButton').disabled = false;
        document.getElementById('DisconnectButton').disabled = true;
        document.getElementById('SubmitButton').disabled = true;
    }
}

// Connect to Native Messaging Host
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

// Disconnect from Native Messaging Host
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

// Submit password for encryption
function submitPassword() {
    var request = new Object();
    request.type = "SubmitPassword";
    request.message = { 'MessageType': 'submit' };

    browser.runtime.sendMessage(request,
        function (response) {
            document.getElementById("EncryptedPassword").value = response;
        });
}

// Notify Native Messaging Host that textbox has focus and we should start intercepting keys
function passwordInputFocus() {
    if (document.getElementById('ConnectButton').disabled) {
        var request = new Object();
        request.type = "PasswordInputFocus";
        request.message = { 'MessageType': 'focus' };

        browser.runtime.sendMessage(request,
            function (response) {
                document.getElementById("EncryptedPassword").value = response;
            });
    }
}

// Notify Native Messaging Host that textbox is out of focus, and we should stop intercepting keys
function passwordInputBlur() {
    if (document.getElementById('ConnectButton').disabled) {
        var request = new Object();
        request.type = "PasswordInputBlur";
        request.message = { 'MessageType': 'focusout' };

        browser.runtime.sendMessage(request,
            function (response) {
                document.getElementById("EncryptedPassword").value = response;
            });
    }
}

// Update HTML page UI depending on messages from background script
browser.runtime.onMessage.addListener(function (message) {
    var passwordField = document.getElementById("Password");
    if (message == "*") {
        passwordField.value += message;
    } else if (message == "delete") {
        passwordField.value = passwordField.value.slice(0, -1);
    } else if (message == "disconnected") {
        updateUiState(false);
    }

    // Move cursor to the end
    var endIndex = passwordField.value.length;
    if (passwordField.setSelectionRange) {
        passwordField.setSelectionRange(endIndex, endIndex);
    }
});

document.getElementById('ConnectButton').onclick = connectNativeApp;
document.getElementById('DisconnectButton').onclick = disconnectNativeApp;
document.getElementById('SubmitButton').onclick = submitPassword;
document.getElementById('Password').onfocus = passwordInputFocus;
document.getElementById('Password').onblur = passwordInputBlur;