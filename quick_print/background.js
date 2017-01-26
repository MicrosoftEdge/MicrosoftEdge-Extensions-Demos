// Listen for when the browser action is clicked
browser.browserAction.onClicked.addListener(function(tab) {
   // Browser action has been clicked, so execute the print script
   browser.tabs.executeScript(null,{code:"window.print()"});
});
