# Notifications

This Microsoft Edge extension sample shows the effects of all the Notifications APIs.
Used in the background script here, these API calls create, update, clear, and get info about notifications. 

## Behavior

Once you've sideloaded this extension, click on the browser action icon to trigger a notification.
Each click of the browser action icon will cycle through eight different Notification API calls.

1. Triggers an alert stating the notification permission level.
2. Creates a basic notification that displays a title, message, and two buttons.
3. Creates an image notification that displays an image and a button.
4. Creates a list notification with two items in the list.
5. Creates a progress notifications with a progress indicator that is set to 30.
6. Updates the basic notification that was created from the second click with a new title and message.
7. Gets all the names of the notifications that have been created and displays an alert stating the number of notifications and their IDs.
8. Clears all notifications.


## APIs used
- [`notifications.clear`](https://developer.mozilla.org/en-US/docs/Mozilla/Add-ons/WebExtensions/API/notifications/clear)
- [`notifications.create`](https://developer.mozilla.org/en-US/docs/Mozilla/Add-ons/WebExtensions/API/notifications/create)
- [`notifications.getAll`](https://developer.mozilla.org/en-US/docs/Mozilla/Add-ons/WebExtensions/API/notifications/getAll)
- [`notifications.getPermissionLevel`]()
- [`notifications.onButtonClicked`](https://developer.mozilla.org/en-US/docs/Mozilla/Add-ons/WebExtensions/API/notifications/onButtonClicked)
- [`notifications.onClicked`](https://developer.mozilla.org/en-US/docs/Mozilla/Add-ons/WebExtensions/API/notifications/onClicked)
- [`notifications.onClosed`](https://developer.mozilla.org/en-US/docs/Mozilla/Add-ons/WebExtensions/API/notifications/onClosed)
- [`notifications.onPermissionLevelChanged`]()
- [`notifications.update`](https://developer.mozilla.org/en-US/docs/Mozilla/Add-ons/WebExtensions/API/notifications/update)

- [`browserAction.onClicked`](https://developer.mozilla.org/en-US/Add-ons/WebExtensions/API/browserAction/onClicked)
