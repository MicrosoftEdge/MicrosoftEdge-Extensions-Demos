/**
 * CSS to hide everything on the page,
 * except for elements that have the "beastify-image" class.
 */
const hidePage = `body > :not(.beastify-image) {
  display: none;
}`;

/**
 * Listen for clicks on the buttons, and send the appropriate message to
 * the content script in the page.
 */
function listenForClicks() {
  document.addEventListener("click", function(e) {
    /**
     * Given the name of a beast, get the URL to the corresponding image.
     */
    function beastNameToURL(beastName) {
      switch (beastName) {
        case "Frog":
          return browser.extension.getURL("beasts/frog.jpg");
        case "Snake":
          return browser.extension.getURL("beasts/snake.jpg");
        case "Turtle":
          return browser.extension.getURL("beasts/turtle.jpg");
      }
    }

    /**
     * Insert the page-hiding CSS into the active tab,
     * then get the beast URL and
     * send a "beastify" message to the content script in the active tab.
     */
    function beastify(tabs) {
      browser.tabs.insertCSS({code: hidePage});
      let url = beastNameToURL(e.target.textContent);
      browser.tabs.sendMessage(tabs[0].id, {
        command: "beastify",
        beastURL: url
      });
    }

    /**
     * Remove the page-hiding CSS from the active tab,
     * send a "reset" message to the content script in the active tab.
     */
    function reset(tabs) {
      browser.tabs.sendMessage(tabs[0].id, {
        command: "reset",
      });
      browser.tabs.executeScript(tabs[0].id, { code: 'window.location.reload();' });      
    }

    /**
     * Get the active tab,
     * then call "beastify()" or "reset()" as appropriate.
     */
    if (e.target.classList.contains("beast")) {
      browser.tabs.query({active: true, currentWindow: true}, function(tabs) {
        beastify(tabs);
      });
      return;
    }
    else if (e.target.classList.contains("reset")) {
      browser.tabs.query({ active: true, currentWindow: true }, function (tabs) {
        reset(tabs);
      });
      return;
    }
  });
}

/**
 * When the popup loads, inject a content script into the active tab,
 * and add a click handler.
 */
browser.tabs.executeScript({file: "/content_scripts/beastify.js"});
listenForClicks();