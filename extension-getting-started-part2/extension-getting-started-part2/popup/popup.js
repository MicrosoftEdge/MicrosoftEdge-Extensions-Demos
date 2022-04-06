const sendMessageId = document.querySelector("#sendMessageId");
if (sendMessageId) {
    sendMessageId.onclick = function () {
        chrome.tabs.query({ active: true, currentWindow: true }, function (tabs) {
            function guidGenerator() {
                const S4 = function () {
                    return (((1 + Math.random()) * 0x10000) | 0)
                        .toString(16)
                        .substring(1);
                };
                return (S4() + S4() + "-" + S4() + "-" + S4() + "-" + S4() + "-" + S4() + S4() + S4());
            }

            chrome.tabs.sendMessage(
                tabs[0].id,
                {
                    url: chrome.runtime.getURL("images/stars.jpeg"),
                    imageDivId: `${guidGenerator()}`,
                    tabId: tabs[0].id,
                },
                function (response) {
                    window.close();
                }
            );
        });
    };
}
