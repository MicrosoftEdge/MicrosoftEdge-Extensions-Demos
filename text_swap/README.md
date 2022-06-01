# Text Swap
The Text Swap extension takes advantage of [match patterns](https://developer.mozilla.org/Add-ons/WebExtensions/Match_patterns) to specify where content scripts are automatically run.
Text Swap has two content script:
- `content.js` - Changes all text in divs to have the Papyrus font family.
- `content-size.js` - Changes all text in divs to have a font size of 30px.

## content.js

The manifest.json file has set the content.js script to only run on the following URL pattern:

`"*://developer.microsoft.com/*"`

This means that `content.js` will run on URLS that:
- Start with HTTP or HTTPS 
- Are from developer.microsoft.com with any path

An example of a URL that will run this script is [https://developer.microsoft.com/en-us/windows/bridges/hosted-web-apps](https://developer.microsoft.com/en-us/windows/bridges/hosted-web-apps).

## content-size.js
The `content-size.js` script only run on pages with the following URL pattern:

`"*://developer.microsoft.com/*/microsoft-edge/*"`

This means that it will run on URLS that:
- Start with HTTP or HTTPS 
- Are only at `://developer.microsoft.com/`
- Have any locale (Based on the developer.microsoft.com URL structure)
- Have any path under `/microsoft-edge/`

Pages that match the `content-size.js` match pattern will also match the `content.js` match pattern.
This means that both scripts will be run on these pages. 

An example of a URL that meets this match is [https://developer.microsoft.com/en-us/microsoft-edge/platform/](https://developer.microsoft.com/en-us/microsoft-edge/platform/).

## APIs used
No extension APIs are used.
