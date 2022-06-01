// Creating notification options object for different types of notifications
var allNotificationOptions = {
    "type": "basic",                                       // Types supported: [image, list, progress, basic]. REQUIRED PARAMETER.
    "iconUrl": "icon.png",                                 // Icon for the notification. REQUIRED PARAMETER.
    "title": "Title",                                      // Notification title. REQUIRED PARAMETER.
    "message": "Message",                                  // Notification message. REQUIRED PARAMETER.
    "contextMessage": "Context message",                   // Notification context message.
    "priority": 0,                                         // Values supported: [0, 1, 2].
    "buttons": [{ "title": "Title1", "iconUrl": "icon1.png" }, { "title": "Title2", "iconUrl": "icon2.png" }], // Max 2 buttons supported.
    "imageUrl": "image.png",                               // Only supported for image type notifications.
    "items": [{ "title": "Item1", "message": "Message1" }, { "title": "Item2", "message": "Message2" }], // Max 5 items supported. Only for list type notifications.
    "progress": 10,                                        // Values supported in the range [0-100]. Only for progress type notifications.
    "requireInteraction": false                            // Values supported: [true, false].
};

var basicNotificationOptions = {
    "type": "basic",
    "iconUrl": "icon.png",
    "title": "Basic title",
    "message": "Basic message",
    "contextMessage": "Basic context message",
    "buttons": [{ "title": "Yes", "iconUrl": "ok.png" }, { "title": "No", "iconUrl": "cancel.png" }],
    "requireInteraction": false
};

var updatedBasicNotificationOptions = {
    "title": "Updated title",
    "message": "Updated message"
};

var imageNotificationOptions = {
    "type": "image",
    "iconUrl": "icon.png",
    "title": "Image title",
    "message": "Image message",
    "priority": 1,
    "buttons": [{ "title": "Title1", "iconUrl": "ok.png" }],
    "imageUrl": "image.png"
};

var listNotificationOptions = {
    "type": "list",
    "iconUrl": "icon.png",
    "title": "List title",
    "message": "List message",
    "items": [{ "title": "Item1", "message": "Message1" }, { "title": "Item2", "message": "Message2" }]
};

var progressNotificationOptions = {
    "type": "progress",
    "iconUrl": "icon.png",
    "title": "Progress title",
    "message": "Progress message",
    "progress": 30
};

// Add listener for onClicked event.
browser.notifications.onClicked.addListener(function (notificationid) {
    window.alert(notificationid + " clicked");
});

// Add listener for onButtonClicked event.
browser.notifications.onButtonClicked.addListener(function (notificationid, buttonIndex) {
    window.alert("Button " + buttonIndex + " of " + notificationid + " clicked");
});

// Add listener for onClosed event.
browser.notifications.onClosed.addListener(function (notificationid, byUser) {
    window.alert("Notification " + notificationid + " closed " + (byUser ? "by user" : "automatically due to timeout"));
});

// Add listener for onPermissionLevelChanged event.
browser.notifications.onPermissionLevelChanged.addListener(function (level) {
    window.alert("Notification permission " + ((String(level) == "granted") ? "granted" : "denied"));
});

// Use a state machine to create different types of notifications and to use the different notifications API methods. The state is changed by clicking the browser action.
// State 0: Get permission level
// State 1: Create basic notification
// State 2: Create image notification
// State 3: Create list notification
// State 4: Create progress notification
// State 5: Update basic notification
// State 6: Get all notifications
// State 7: Clear all notifications
var state = 0;

browser.browserAction.onClicked.addListener(function () {
    if (state === 0){
        browser.notifications.getPermissionLevel(function (level) {
            window.alert("Notification permission: " + level);
        });
        state = 1;
    } else if (state === 1){
        browser.notifications.create("nid1", basicNotificationOptions, function (notifId) {
        });
        state = 2;
    } else if (state === 2) {
        browser.notifications.create("", imageNotificationOptions, function (notifId) {
            window.alert("Image notification " + notifId + " created");
        });
        state = 3;
    } else if (state === 3) {
        browser.notifications.create("", listNotificationOptions, function (notifId) {
        });
        state = 4;
    } else if (state === 4) {
        browser.notifications.create("", progressNotificationOptions, function (notifId) {
        });
        state = 5;
    } else if (state === 5) {
        browser.notifications.update("nid1", updatedBasicNotificationOptions , function (wasUpdated) {
            window.alert("Basic notification " + (wasUpdated ? "updated" : "NOT updated"));
        });
        state = 6;
    } else if (state === 6) {
        browser.notifications.getAll(function (notifIds) {
            var ids = "";
            for (var name in notifIds) {
                ids += name + ",";
            }              
            window.alert(Object.keys(notifIds).length + " notifications found. Their ids are " + ids);
        });
        state = 7;
    } else {
        browser.notifications.getAll(function (notifIds) {
            var ids = "";
            for (var name in notifIds) {
                browser.notifications.clear(name, function (wasCleared) {
					// window.alert("Notification " + (wasCleared ? "cleared" : "NOT cleared"));
				});
            }              
        });
        state = 0;
    }
});
