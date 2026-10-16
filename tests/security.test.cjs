const test = require("node:test");
const assert = require("node:assert/strict");
const vm = require("node:vm");
const fs = require("node:fs");
const core = require("../shared/core.js");
const xml = '<p:FatturaElettronica xmlns:p="http://ivaservizi.agenziaentrate.gov.it/docs/xsd/fatture/v1.2"><FatturaElettronicaBody/></p:FatturaElettronica>';
const bytes = value => new TextEncoder().encode(value);
const tlv = (tag, body) => {
    body = Buffer.from(body);
    const length = body.length < 128 ? [body.length] : body.length < 256 ? [129, body.length] : [130, body.length >> 8, body.length & 255];
    return Buffer.concat([Buffer.from([tag, ...length]), body]);
};
const join = (...parts) => Buffer.concat(parts);
const oid = end => tlv(6, [42, 134, 72, 134, 247, 13, 1, 7, end]);
const cms = (payload, indefinite = false) => {
    const octets = indefinite ? join(Buffer.from([36, 128]), tlv(4, payload.subarray(0, 40)), tlv(4, payload.subarray(40)), Buffer.from([0, 0])) : tlv(4, payload);
    return tlv(48, join(oid(2), tlv(160, tlv(48, join(tlv(2, [1]), tlv(49, []),
        tlv(48, join(oid(1), tlv(160, octets))), tlv(49, []))))));
};
test("sender filter uses the complete mailbox, ignoring the display name", () => {
    for (const sender of ["sdi01@pec.fatturapa.it", 'SDI <SDI01@PEC.FATTURAPA.IT>']) assert.equal(core.isSdiSender(sender), true);
    for (const sender of ['"sdi01@pec.fatturapa.it" <evil@example.org>', "sdi01@pec.fatturapa.it.evil.org", "x@evil.org, sdi01@pec.fatturapa.it", "a@evil.org, SDI <sdi01@pec.fatturapa.it>", "sdi01@pec.fatturapa.it\r\nX: y", null]) assert.equal(core.isSdiSender(sender), false);
});
test("XML preserves accented characters and supports UTF-16", () => {
    const value = xml.replace("<FatturaElettronicaBody/>", "<FatturaElettronicaBody>caffè</FatturaElettronicaBody>");
    assert.equal(core.extractInvoiceXml(bytes(value), "invoice.xml"), value);
    assert.equal(core.extractInvoiceXml(Buffer.from("\ufeff" + value, "utf16le"), "invoice.xml"), value);
});
test("CMS primitive and constructed BER content are extracted without a signature verdict", () => {
    for (const indefinite of [false, true]) assert.equal(core.extractInvoiceXml(cms(bytes(xml), indefinite), "invoice.xml.p7m"), xml);
});
test("P7M rejects loose XML, truncated BER, appended XML and a wrong content type", () => {
    assert.throws(() => core.extractInvoiceXml(bytes(xml), "fake.p7m"));
    const valid = cms(bytes(xml));
    assert.throws(() => core.extractInvoiceXml(valid.subarray(0, valid.length - 1), "fake.p7m"));
    assert.throws(() => core.extractInvoiceXml(join(valid, bytes(xml)), "fake.p7m"));
    const wrong = Buffer.from(valid);
    wrong[wrong.indexOf(Buffer.from([42,134,72,134,247,13,1,7,2])) + 8] = 1;
    assert.throws(() => core.extractInvoiceXml(wrong, "fake.p7m"));
});
test("XML disallows DTD/entity declarations and oversized files", () => {
    assert.throws(() => core.extractInvoiceXml(bytes('<!DOCTYPE a [<!ENTITY x "z">]>' + xml), "invoice.xml"));
    assert.throws(() => core.extractInvoiceXml(new Uint8Array(core.MAX_FILE_BYTES + 1), "invoice.xml"));
    assert.throws(() => core.extractInvoiceXml(bytes("<html/>"), "invoice.xml"));
});
test("filename matching allows foreign issuers, excludes metadata and paths", () => {
    assert.equal(core.isInvoiceFilename("DE123_invoice.xml.p7m"), true);
    for (const name of ["IT123_MT_001.xml", "../invoice.xml", "invoice.xml.exe"]) assert.equal(core.isInvoiceFilename(name), false);
});
test("Base64 rejects corrupt input and enforces decoded limits", () => {
    assert.deepEqual(core.decodeAttachment("Y2 Fm ZsOo\n"), bytes("caffè"));
    for (const value of ["YQ==junk", "!!!!", "=YQ=", "YQ==="]) assert.throws(() => core.decodeAttachment(value));
    const maximum = Buffer.alloc(core.MAX_ATTACHMENT_BYTES).toString("base64");
    assert.equal(core.decodeAttachment(maximum).length, core.MAX_ATTACHMENT_BYTES);
    assert.throws(() => core.decodeAttachment(Buffer.alloc(core.MAX_ATTACHMENT_BYTES + 1).toString("base64")));
});
test("PDF recognition requires a header and a trailer, without claiming antivirus checks", () => {
    assert.equal(core.isPdf(bytes("%PDF-1.7\n%%EOF\n")), true);
    assert.equal(core.isPdf(bytes("<html>hello</html>")), false);
    assert.equal(core.isPdf(bytes("%PDF-1.7\ntruncated")), false);
});

function backgroundHarness() {
    const listeners = {};
    const titles = new Map();
    const enabled = new Set();
    const attachments = new Map();
    const messages = new Map();
    const files = new Map();
    const tabs = [];
    let settings = {};
    const event = name => ({ addListener: callback => { listeners[name] = callback; } });
    const browser = {
        storage: { local: { remove: async () => {}, get: async () => settings }, onChanged: event("settings") },
        messages: {
            listAttachments: async id => attachments.get(id) || [],
            getAttachmentFile: async (id, part) => { const data = files.get(id + ":" + part); return { size: data.length, arrayBuffer: async () => Uint8Array.from(data).buffer }; }
        },
        messageDisplay: { onMessageDisplayed: event("display"), getDisplayedMessage: async id => messages.get(id) },
        messageDisplayAction: { onClicked: event("click"), disable: async id => enabled.delete(id), enable: async id => enabled.add(id), setTitle: async ({ tabId, title }) => titles.set(tabId, title) },
        tabs: { onRemoved: event("removed"), create: async info => { const tab = { id: 100 + tabs.length, ...info }; tabs.push(tab); return tab; }, update: async (id, info) => Object.assign(tabs.find(tab => tab.id === id), info) },
        runtime: { id: "addon", getURL: path => "moz-extension://addon/" + path, onMessage: event("request") }
    };
    vm.runInNewContext(fs.readFileSync(require.resolve("../background.js"), "utf8"), { browser, InvoiceCore: core, crypto: require("node:crypto").webcrypto });
    function add(id, author = "sdi01@pec.fatturapa.it") {
        const message = { id, author };
        const part = { name: "IT_invoice_" + id + ".xml", partName: "1.2", size: xml.length };
        attachments.set(id, [part]); files.set(id + ":1.2", bytes(xml)); return message;
    }
    async function display(tabId, message) { messages.set(tabId, message); await listeners.display({ id: tabId }, message); }
    const request = (tab, index = 0, senderTab = tab.id) => listeners.request({ type: "getInvoice", token: new URL(tab.url).searchParams.get("invoice"), index }, { id: "addon", tab: { id: senderTab }, url: tab.url });
    return { listeners, attachments, tabs, add, display, request, enabled, setSettings: value => { settings = value; } };
}
test("two mail tabs and existing viewers retain their own invoice", async () => {
    const h = backgroundHarness();
    await h.display(1, h.add(10)); await h.listeners.click({ id: 1 });
    await h.display(2, h.add(20)); await h.listeners.click({ id: 2 });
    assert.equal((await h.request(h.tabs[0])).filename, "IT_invoice_10.xml");
    assert.equal((await h.request(h.tabs[1])).filename, "IT_invoice_20.xml");
    await h.display(1, h.add(30));
    assert.equal((await h.request(h.tabs[0])).filename, "IT_invoice_10.xml");
    await assert.rejects(h.request(h.tabs[0], 0, 999));
    await assert.rejects(h.request(h.tabs[0], -1));
    h.listeners.removed(h.tabs[0].id);
    await assert.rejects(h.request(h.tabs[0]));
});
test("PEC wrapper cannot grant trust to an unrelated inner sender", async () => {
    const h = backgroundHarness();
    const inner = h.add(20, "evil@example.org");
    h.attachments.set(10, [{ name: "postacert.eml", message: inner }]);
    await h.display(1, { id: 10, author: "sdi01@pec.fatturapa.it" });
    assert.equal(h.enabled.has(1), false);
    h.setSettings({ allowAnySender: true });
    await h.display(1, { id: 10, author: "provider@example.org" }); await h.listeners.click({ id: 1 });
    assert.equal((await h.request(h.tabs[0])).senderMatches, false);
    h.setSettings({});
    await assert.rejects(h.request(h.tabs[0]));
});
test("multiple invoices are selectable, invalid indexes are rejected", async () => {
    const h = backgroundHarness();
    const message = h.add(10);
    h.attachments.get(10).push({ name: "second.xml", partName: "1.2", size: xml.length });
    await h.display(1, message); await h.listeners.click({ id: 1 });
    assert.equal((await h.request(h.tabs[0], 1)).filename, "second.xml");
    await assert.rejects(h.request(h.tabs[0], 2));
});
test("a slow scan cannot replace a newer message", async () => {
    const h = backgroundHarness();
    let release;
    const slow = new Promise(resolve => { release = resolve; });
    h.attachments.set(10, slow);
    const first = h.display(1, { id: 10, author: "sdi01@pec.fatturapa.it" });
    await h.display(1, h.add(20));
    release([{ name: "stale.xml", partName: "1", size: 10 }]);
    await first; await h.listeners.click({ id: 1 });
    assert.equal((await h.request(h.tabs[0])).filename, "IT_invoice_20.xml");
});

