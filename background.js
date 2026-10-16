/* global InvoiceCore */
"use strict";
const displayedMessages = new Map();
const viewerMessages = new Map();
const scans = new Map();
browser.storage.local.remove("currentInvoice").catch(() => {});

async function findInvoices(message, allowAnySender, depth = 0, budget = { remaining: 200 }) {
    if (depth > 4 || budget.remaining <= 0) return [];
    const matches = InvoiceCore.isSdiSender(message.author);
    const attachments = await browser.messages.listAttachments(message.id);
    const invoices = [];
    for (const attachment of attachments) {
        if (--budget.remaining < 0) break;
        if ((allowAnySender || matches) && InvoiceCore.isInvoiceFilename(attachment.name) &&
            attachment.size <= InvoiceCore.MAX_FILE_BYTES &&
            (!attachment.type || attachment.type === "normal")) {
            invoices.push({ messageId: message.id, partName: attachment.partName,
                filename: attachment.name, senderMatches: matches });
        }
        // Let Thunderbird decode MIME and check the inner message's own sender.
        if (attachment.message && attachment.name?.toLowerCase() === "postacert.eml") {
            invoices.push(...await findInvoices(attachment.message, allowAnySender, depth + 1, budget));
        }
    }
    return invoices;
}

async function scanMessage(tab, message) {
    const generation = {};
    scans.set(tab.id, generation);
    displayedMessages.delete(tab.id);
    await browser.messageDisplayAction.disable(tab.id);
    await browser.messageDisplayAction.setTitle({ tabId: tab.id, title: "Nessuna fattura trovata" });
    try {
        const settings = await browser.storage.local.get("allowAnySender");
        const invoices = await findInvoices(message, settings.allowAnySender === true);
        if (scans.get(tab.id) !== generation) return;
        if (invoices.length) {
            displayedMessages.set(tab.id, { id: message.id, invoices });
            await browser.messageDisplayAction.setTitle({ tabId: tab.id,
                title: `Visualizza fatture (${invoices.length})` });
            await browser.messageDisplayAction.enable(tab.id);
        }
    } catch {
        if (scans.get(tab.id) === generation) {
            await browser.messageDisplayAction.setTitle({ tabId: tab.id,
                title: "Impossibile leggere gli allegati del messaggio" });
        }
    }
}
browser.messageDisplay.onMessageDisplayed.addListener(scanMessage);
browser.messageDisplayAction.onClicked.addListener(async tab => {
    const selection = displayedMessages.get(tab.id);
    if (!selection) return;
    const current = await browser.messageDisplay.getDisplayedMessage(tab.id);
    if (current?.id !== selection.id) return;
    const token = crypto.randomUUID();
    const entry = { ...selection, tabId: null };
    viewerMessages.set(token, entry);
    try {
        const viewer = await browser.tabs.create({ url: "about:blank" });
        entry.tabId = viewer.id;
        await browser.tabs.update(viewer.id, { url: browser.runtime.getURL(`viewer.html?invoice=${token}`) });
    } catch { viewerMessages.delete(token); }
});
browser.runtime.onMessage.addListener(async (request, sender) => {
    if (request?.type !== "getInvoice") return;
    const entry = viewerMessages.get(request.token);
    if (!entry || sender.id !== browser.runtime.id || sender.tab?.id !== entry.tabId ||
        !sender.url?.startsWith(browser.runtime.getURL("viewer.html?"))) {
        throw new Error("Fattura non disponibile. Riaprila dal messaggio originale.");
    }
    const index = request.index ?? 0;
    if (!Number.isInteger(index) || index < 0 || index >= entry.invoices.length) {
        throw new Error("Selezione fattura non valida.");
    }
    const selected = entry.invoices[index];
    const settings = await browser.storage.local.get("allowAnySender");
    if (!selected.senderMatches && settings.allowAnySender !== true) {
        throw new Error("Il filtro mittenti è attivo. Riapri un messaggio consentito.");
    }
    const file = await browser.messages.getAttachmentFile(selected.messageId, selected.partName);
    if (file.size > InvoiceCore.MAX_FILE_BYTES) throw new Error("Fattura troppo grande (massimo 20 MiB).");
    const xml = InvoiceCore.extractInvoiceXml(new Uint8Array(await file.arrayBuffer()), selected.filename);
    return { xml, filename: selected.filename, senderMatches: selected.senderMatches,
        filenames: entry.invoices.map(invoice => invoice.filename) };
});
browser.tabs.onRemoved.addListener(tabId => {
    scans.delete(tabId);
    displayedMessages.delete(tabId);
    for (const [token, entry] of viewerMessages) {
        if (entry.tabId === tabId) viewerMessages.delete(token);
    }
});
browser.storage.onChanged.addListener(async (changes, area) => {
    if (area !== "local" || !changes.allowAnySender) return;
    displayedMessages.clear();
    for (const tabId of [...scans.keys()]) {
        try {
            const message = await browser.messageDisplay.getDisplayedMessage(tabId);
            if (message) await scanMessage({ id: tabId }, message);
        } catch { /* The tab may have closed. */ }
    }
});

