const test = require("node:test");
const assert = require("node:assert/strict");
const vm = require("node:vm");
const fs = require("node:fs");

function viewerHarness() {
    const elements = new Map();
    const frames = [];
    let rejectInvoice = false;
    let printed = 0;
    class Element {
        constructor(tag) { this.tag = tag; this.children = []; this.style = {}; this.listeners = {}; this.attributes = {}; this.textContent = ""; this.isConnected = false; }
        addEventListener(name, listener) { this.listeners[name] = listener; }
        setAttribute(name, value) { this.attributes[name] = value; }
        appendChild(child) { this.children.push(child); }
        prepend(child) { this.children.unshift(child); }
        replaceChildren(...children) {
            this.children.forEach(child => { child.isConnected = false; });
            this.children = children;
            children.forEach(child => { child.isConnected = true; });
        }
    }
    const document = {
        getElementById(id) {
            if (!elements.has(id)) elements.set(id, new Element("div"));
            return elements.get(id);
        },
        createElement(tag) {
            const element = new Element(tag);
            if (tag === "iframe") {
                element.contentWindow = { focus() {}, print() { printed++; } };
                frames.push(element);
            }
            return element;
        }
    };
    let output;
    class DOMParser {
        parseFromString() {
            return {
                querySelector: () => null,
                documentElement: { localName: "FatturaElettronica", namespaceURI: "http://ivaservizi.agenziaentrate.gov.it/docs/xsd/fatture/v1.2", children: [] },
                getElementsByTagName: () => []
            };
        }
    }
    class XSLTProcessor {
        importStylesheet() {}
        transformToDocument() {
            const head = new Element("head");
            output = {
                head,
                querySelector: selector => selector === "head" ? head : selector === "#fattura-container" ? { textContent: "Fattura di prova" } : null,
                createElement: tag => new Element(tag),
                documentElement: { outerHTML: "<html><head></head><body>Fattura di prova</body></html>" }
            };
            return output;
        }
    }
    const context = vm.createContext({
        document, DOMParser, XSLTProcessor, URLSearchParams, URL,
        location: { search: "?invoice=test" },
        window: { addEventListener() {} },
        InvoiceCore: { checkXmlText: xml => xml },
        fetch: async () => ({ ok: true, text: async () => "<xsl/>" }),
        browser: {
            runtime: {
                getURL: value => value,
                sendMessage: async request => {
                    if (rejectInvoice) throw new Error("Fattura non disponibile");
                    return { xml: "<invoice/>", filename: "fattura-" + request.index + ".xml", senderMatches: true, filenames: ["prima.xml", "seconda.xml"] };
                }
            }
        }
    });
    vm.runInContext(fs.readFileSync(require.resolve("../viewer.js"), "utf8"), context);
    return {
        elements, frames, context, get printed() { return printed; }, get output() { return output; },
        fail() { rejectInvoice = true; },
        async settle() { await new Promise(resolve => setImmediate(resolve)); }
    };
}

test("print stays disabled until the selected invoice frame finishes loading", async () => {
    const h = viewerHarness();
    const button = h.elements.get("print-invoice");
    assert.equal(button.disabled, true);
    button.listeners.click();
    assert.equal(h.printed, 0);
    await h.settle();
    assert.equal(button.disabled, true);
    h.frames[0].listeners.load();
    assert.equal(button.disabled, false);
    button.listeners.click();
    assert.equal(h.printed, 1);
    assert.equal(h.frames[0].attributes.sandbox, "allow-same-origin allow-modals");
    assert.ok(h.output.head.children.some(node => node.attributes.content?.includes("script-src 'none'")));
});

test("a previous frame cannot enable printing after selecting a different invoice", async () => {
    const h = viewerHarness();
    await h.settle();
    const oldFrame = h.frames[0];
    await vm.runInContext("showInvoice(1)", h.context);
    oldFrame.listeners.load();
    assert.equal(h.elements.get("print-invoice").disabled, true);
    h.frames[1].listeners.load();
    h.elements.get("print-invoice").listeners.click();
    assert.equal(h.printed, 1);
    assert.equal(h.elements.get("title").textContent, "Fattura: fattura-1.xml");
});

test("a failed invoice change prevents printing the previous invoice", async () => {
    const h = viewerHarness();
    await h.settle();
    h.frames[0].listeners.load();
    h.fail();
    await vm.runInContext("showInvoice(1)", h.context);
    const button = h.elements.get("print-invoice");
    assert.equal(button.disabled, true);
    button.listeners.click();
    assert.equal(h.printed, 0);
});

test("print failures display feedback and a subsequent attempt can succeed", async () => {
    const h = viewerHarness();
    await h.settle();
    const frame = h.frames[0];
    frame.listeners.load();
    const actualPrint = frame.contentWindow.print;
    frame.contentWindow.print = () => { throw new Error("Unavailable"); };
    h.elements.get("print-invoice").listeners.click();
    assert.match(h.elements.get("print-status").textContent, /Impossibile avviare la stampa/);
    frame.contentWindow.print = actualPrint;
    h.elements.get("print-invoice").listeners.click();
    assert.equal(h.printed, 1);
    assert.equal(h.elements.get("print-status").textContent, "");
});

