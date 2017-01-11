browser.browserAction.onClicked.addListener(function(tab) {
   browser.tabs.executeScript(null,{code:"window.print()"});
});
