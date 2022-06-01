# Quick Print

The Quick Print extension is a simple extension that executes `windows.print` when the browser action is clicked.
The printing code is placed within the [background script](https://developer.mozilla.org/Add-ons/WebExtensions/Anatomy_of_a_WebExtension#Background_scripts).

To see this extensions being made in a video, check out the
[Adding a Background Script to your Edge Extensions](https://channel9.msdn.com/Blogs/One-Dev-Minute/Adding-a-Background-Script-to-you-Edge-Extension) video.

## APIs used

- [`browserAction.onClicked`](https://developer.mozilla.org/Add-ons/WebExtensions/API/browserAction/onClicked)
- [`tabs.executeScript`](https://developer.mozilla.org/en-US/Add-ons/WebExtensions/API/tabs/executeScript)
