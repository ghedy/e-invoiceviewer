"use strict";
const checkbox = document.getElementById("allow-any");
checkbox.disabled = true;
browser.storage.local.get("allowAnySender").then(settings => {
    checkbox.checked = settings.allowAnySender === true;
    checkbox.disabled = false;
}).catch(() => {
    document.getElementById("status").textContent = "Impossibile leggere le preferenze.";
});
checkbox.addEventListener("change", async () => {
    checkbox.disabled = true;
    try {
        await browser.storage.local.set({ allowAnySender: checkbox.checked });
        document.getElementById("status").textContent = "Preferenza salvata.";
    } catch {
        checkbox.checked = !checkbox.checked;
        document.getElementById("status").textContent = "Impossibile salvare la preferenza.";
    } finally { checkbox.disabled = false; }
});

