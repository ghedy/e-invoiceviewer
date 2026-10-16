/* global InvoiceCore */
"use strict";
const openedAttachmentUrls = [];
const token = new URLSearchParams(location.search).get("invoice");
let selectionGeneration = 0;
let printableFrame = null;
const printButton = document.getElementById("print-invoice");

function printInvoice() {
    if (!printableFrame?.isConnected || printButton.disabled) return;
    document.getElementById("print-status").textContent = "";
    try {
        // Print the complete iframe document, not the parent page or its 80vh viewport.
        printableFrame.contentWindow.focus();
        printableFrame.contentWindow.print();
    } catch {
        document.getElementById("print-status").textContent =
            "Impossibile avviare la stampa. Riapri la fattura e riprova.";
    }
}
printButton.addEventListener("click", printInvoice);
window.addEventListener("unload", () => openedAttachmentUrls.forEach(url => URL.revokeObjectURL(url)));

function childText(parent, localName) {
    return Array.from(parent.children).find(node => node.localName === localName)?.textContent.trim() || "";
}
function renderAttachments(xmlDoc) {
    const list = document.getElementById("attachments-list");
    list.replaceChildren();
    const attachments = Array.from(xmlDoc.getElementsByTagName("*")).filter(node => node.localName === "Allegati");
    document.getElementById("attachments-bar").style.display = attachments.length ? "block" : "none";
    document.getElementById("attachments-title").textContent = attachments.length === 1 ? "Allegato:" : "Allegati:";
    for (const [index, node] of attachments.entries()) {
        const name = (childText(node, "NomeAttachment") || `allegato-${index + 1}`)
            .replace(/[\\/:*?"<>|\x00-\x1f\x7f\u202a-\u202e\u2066-\u2069]/g, "_").slice(0, 180);
        const format = childText(node, "FormatoAttachment").toLowerCase();
        const compression = childText(node, "AlgoritmoCompressione");
        const button = document.createElement("button");
        button.type = "button";
        button.className = "attachment-button";
        button.textContent = name;
        // Decode on click: opening an invoice need not decode every embedded PDF.
        button.addEventListener("click", async () => {
            button.disabled = true;
            try {
                const bytes = InvoiceCore.decodeAttachment(childText(node, "Attachment"));
                const pdf = !compression && (format === "pdf" || /\.pdf$/i.test(name)) && InvoiceCore.isPdf(bytes);
                const url = URL.createObjectURL(new Blob([bytes], { type: pdf ? "application/pdf" : "application/octet-stream" }));
                openedAttachmentUrls.push(url);
                if (pdf) {
                    await browser.tabs.create({ url });
                } else {
                    const link = document.createElement("a");
                    link.href = url;
                    link.download = name;
                    link.click();
                    document.getElementById("attachment-status").textContent =
                        "Allegato scaricato. Solo i PDF riconosciuti vengono aperti nell’anteprima.";
                }
            } catch {
                document.getElementById("attachment-status").textContent =
                    "Impossibile aprire l’allegato: contenuto non valido, oltre 10 MiB o apertura non riuscita.";
            } finally { button.disabled = false; }
        });
        list.appendChild(button);
    }
}
const stylesheet = fetch(browser.runtime.getURL("shared/assets/assofattura.xsl")).then(response => {
    if (!response.ok) throw new Error("Impossibile caricare il foglio di stile.");
    return response.text();
});
// Avoid an unhandled rejection if the viewer was opened without a valid token.
stylesheet.catch(() => {});
async function showInvoice(index = 0) {
    const generation = ++selectionGeneration;
    printableFrame = null;
    printButton.disabled = true;
    document.getElementById("print-status").textContent = "";
    const container = document.getElementById("invoice-container");
    container.replaceChildren();
    document.getElementById("attachments-list").replaceChildren();
    document.getElementById("attachments-bar").style.display = "none";
    document.getElementById("attachment-status").textContent = "";
    try {
        const data = await browser.runtime.sendMessage({ type: "getInvoice", token, index });
        if (generation !== selectionGeneration) return;
        const xml = InvoiceCore.checkXmlText(data.xml);
        const parser = new DOMParser();
        const xmlDoc = parser.parseFromString(xml, "application/xml");
        if (xmlDoc.querySelector("parsererror")) throw new Error("XML malformato.");
        const root = xmlDoc.documentElement;
        const namespaces = new Set([
            "http://www.fatturapa.gov.it/sdi/fatturapa/v1.0",
            "http://www.fatturapa.gov.it/sdi/fatturapa/v1.1",
            "http://ivaservizi.agenziaentrate.gov.it/docs/xsd/fatture/v1.2",
            "http://ivaservizi.agenziaentrate.gov.it/docs/xsd/fatture/v1.0"
        ]);
        if (!["FatturaElettronica", "FatturaElettronicaSemplificata"].includes(root.localName) ||
            !namespaces.has(root.namespaceURI)) throw new Error("Formato FatturaPA non supportato.");
        const stack = [[root, 0]];
        let count = 0;
        while (stack.length) {
            const [node, depth] = stack.pop();
            if (++count > 50000 || depth > 64) throw new Error("Fattura troppo complessa da visualizzare.");
            for (const child of node.children) stack.push([child, depth + 1]);
        }
        const xslDoc = parser.parseFromString(await stylesheet, "application/xml");
        if (generation !== selectionGeneration) return;
        if (xslDoc.querySelector("parsererror")) throw new Error("Foglio di stile non valido.");
        const processor = new XSLTProcessor();
        processor.importStylesheet(xslDoc);
        const result = processor.transformToDocument(xmlDoc);
        if (!result?.documentElement || !result.querySelector("#fattura-container")?.textContent.trim()) {
            throw new Error("Il foglio di stile non ha prodotto una fattura leggibile.");
        }
        const policy = result.createElement("meta");
        policy.setAttribute("http-equiv", "Content-Security-Policy");
        policy.setAttribute("content", "default-src 'none'; script-src 'none'; style-src 'unsafe-inline'; form-action 'none'; base-uri 'none'");
        result.querySelector("head").prepend(policy);
        const printStyle = result.createElement("style");
        printStyle.textContent = `
            @media print {
                @page { margin: 10mm; }
                /* Keep the XSL's screen widths and typography: fit the whole
                   invoice to paper in the print dialog instead of reflowing tables. */
                html, body { height: auto; overflow: visible; }
                *, *::before, *::after {
                    print-color-adjust: exact !important;
                    -webkit-print-color-adjust: exact !important;
                }
                tr { break-inside: avoid; }
                thead { display: table-header-group; }
            }
        `;
        result.querySelector("head").appendChild(printStyle);
        let documentTitle = result.querySelector("title");
        if (!documentTitle) {
            documentTitle = result.createElement("title");
            result.querySelector("head").appendChild(documentTitle);
        }
        documentTitle.textContent = data.filename;
        const frame = document.createElement("iframe");
        frame.title = "Fattura elettronica";
        // Same-origin access and modal permission allow the parent to call print().
        // Scripts, forms, popups and top navigation remain blocked by the sandbox;
        // the document CSP independently disallows scripts and remote resources.
        frame.setAttribute("sandbox", "allow-same-origin allow-modals");
        frame.addEventListener("load", () => {
            if (generation !== selectionGeneration || !frame.isConnected) return;
            printableFrame = frame;
            printButton.disabled = false;
        }, { once: true });
        frame.srcdoc = "<!DOCTYPE html>" + result.documentElement.outerHTML;
        container.replaceChildren(frame);
        document.getElementById("title").textContent = "Fattura: " + data.filename;
        document.getElementById("security-status").textContent =
            (data.senderMatches ? "Mittente dichiarato nel dominio pec.fatturapa.it. " : "Filtro mittenti disattivato: provenienza non verificata. ") +
            "Firma digitale e certificazione PEC non verificate. Gli allegati non sono sottoposti a scansione antivirus.";
        const selector = document.getElementById("invoice-select");
        selector.replaceChildren(...data.filenames.map((filename, i) => {
            const option = document.createElement("option");
            option.value = i;
            option.textContent = filename;
            return option;
        }));
        selector.value = index;
        document.getElementById("invoice-choice").hidden = data.filenames.length < 2;
        renderAttachments(xmlDoc);
    } catch (error) {
        if (generation !== selectionGeneration) return;
        document.getElementById("title").textContent = "Impossibile visualizzare la fattura";
        container.textContent = error.message || "Riapri la fattura dal messaggio originale.";
    }
}
document.getElementById("invoice-select").addEventListener("change", event => showInvoice(Number(event.target.value)));
showInvoice();

