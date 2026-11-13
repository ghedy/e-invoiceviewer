"use strict";
const checkbox = document.getElementById("allow-any");
checkbox.disabled = true;
browser.storage.local.get("allowAnySender").then(settings => {
    checkbox.checked = settings.allowAnySender === true;
    checkbox.disabled = false;
}).catch(() => {
    document.getElementById("status").textContent = browser.i18n.getMessage("prefsReadFailed");
});
checkbox.addEventListener("change", async () => {
    checkbox.disabled = true;
    try {
        await browser.storage.local.set({ allowAnySender: checkbox.checked });
        document.getElementById("status").textContent = browser.i18n.getMessage("prefsSaved");
    } catch {
        checkbox.checked = !checkbox.checked;
        document.getElementById("status").textContent = browser.i18n.getMessage("prefsSaveFailed");
    } finally { checkbox.disabled = false; }
});

