"use strict";
// No DOM or mail APIs: reusable by future clients.
const InvoiceCore = (() => {
    const MAX_FILE_BYTES = 20 * 1024 * 1024;
    const MAX_ATTACHMENT_BYTES = 10 * 1024 * 1024;
    function isSdiSender(author) {
        if (typeof author !== "string" || /[\r\n]/.test(author)) return false;
        const mailbox = author.trim().match(/^(?:"[^"<>]*"|[^"<>,;]*)<([^<>]+)>$/);
        const address = (mailbox ? mailbox[1] : author).trim();
        return /^[a-z0-9.!#$%&'*+/=?^_`{|}~-]+@pec\.fatturapa\.it$/i.test(address);
    }
    function isInvoiceFilename(name) {
        return typeof name === "string" && !/[\x00-\x1f/\\]/.test(name) &&
            !/_MT_/i.test(name) && /\.(?:xml|xml\.p7m|p7m)$/i.test(name);
    }
    function decodeXml(bytes) {
        let encoding = "utf-8";
        if ((bytes[0] === 0xff && bytes[1] === 0xfe) || (bytes[0] === 0x3c && bytes[1] === 0)) encoding = "utf-16le";
        else if ((bytes[0] === 0xfe && bytes[1] === 0xff) || (bytes[0] === 0 && bytes[1] === 0x3c)) encoding = "utf-16be";
        else {
            const head = new TextDecoder("windows-1252").decode(bytes.subarray(0, 256));
            encoding = head.match(/<\?xml[^>]*encoding=["']([^"']+)["']/i)?.[1] || encoding;
        }
        return new TextDecoder(encoding, { fatal: true }).decode(bytes).replace(/^\uFEFF/, "").trim();
    }
    function checkXmlText(xml) {
        if (typeof xml !== "string" || xml.length > MAX_FILE_BYTES || /<!DOCTYPE|<!ENTITY/i.test(xml)) {
            throw new Error("XML non consentito o troppo grande.");
        }
        if (!/^(?:<\?xml[^?]*\?>\s*)?(?:<!--[\s\S]*?-->\s*)*<(?:[\w.-]+:)?FatturaElettronica(?:Semplificata)?(?:\s|>)/.test(xml)) {
            throw new Error("Il contenuto non è una fattura elettronica XML.");
        }
        return xml;
    }
    // Bounded BER parsing identifies CMS eContent. It does NOT verify signatures.
    function cmsPayload(bytes) {
        let nodes = 0;
        function read(offset, limit, depth = 0) {
            if (++nodes > 20000 || depth > 32 || offset + 2 > limit) throw new Error("Busta P7M non supportata o malformata.");
            const tag = bytes[offset++];
            if ((tag & 31) === 31 || tag === 0) throw new Error("Tag BER non supportato.");
            let length = bytes[offset++];
            const indefinite = length === 0x80;
            if (indefinite && !(tag & 32)) throw new Error("Lunghezza BER non valida.");
            if (!indefinite && (length & 128)) {
                const count = length & 127;
                if (!count || count > 4 || offset + count > limit) throw new Error("Lunghezza BER non valida.");
                length = 0;
                for (let i = 0; i < count; i++) length = length * 256 + bytes[offset++];
            }
            const start = offset;
            const end = indefinite ? limit : start + length;
            if (end > limit) throw new Error("Busta P7M troncata.");
            const children = [];
            if (tag & 32) {
                while (offset < end) {
                    if (indefinite && bytes[offset] === 0 && bytes[offset + 1] === 0) {
                        return { tag, start, end: offset, next: offset + 2, children };
                    }
                    const child = read(offset, end, depth + 1);
                    children.push(child);
                    offset = child.next;
                }
                if (indefinite) throw new Error("Fine busta P7M mancante.");
            }
            return { tag, start, end, next: end, children };
        }
        const root = read(0, bytes.length);
        const oid = node => node?.tag === 6 ? Array.from(bytes.subarray(node.start, node.end)).join(",") : "";
        const wrapper = root.children[1];
        const signed = wrapper?.children[0];
        if (root.tag !== 48 || root.next !== bytes.length || root.children.length !== 2 ||
            oid(root.children[0]) !== "42,134,72,134,247,13,1,7,2" || wrapper?.tag !== 160 ||
            wrapper.children.length !== 1 || signed?.tag !== 48 ||
            signed.children[0]?.tag !== 2 || signed.children[1]?.tag !== 49 ||
            signed.children.at(-1)?.tag !== 49) throw new Error("Busta CMS SignedData non supportata.");
        const encap = signed.children[2];
        const content = encap?.children[1];
        if (encap?.tag !== 48 || oid(encap.children[0]) !== "42,134,72,134,247,13,1,7,1" ||
            encap.children.length !== 2 || content?.tag !== 160 || content.children.length !== 1) {
            throw new Error("Contenuto XML P7M assente o non supportato.");
        }
        const chunks = [];
        let total = 0;
        function collect(node) {
            if (node.tag === 4) {
                total += node.end - node.start;
                if (total > MAX_FILE_BYTES) throw new Error("Contenuto P7M troppo grande.");
                chunks.push(bytes.subarray(node.start, node.end));
            } else if (node.tag === 36) node.children.forEach(collect);
            else throw new Error("Contenuto P7M non supportato.");
        }
        collect(content.children[0]);
        const payload = new Uint8Array(total);
        let offset = 0;
        for (const chunk of chunks) { payload.set(chunk, offset); offset += chunk.length; }
        return payload;
    }
    function extractInvoiceXml(bytes, filename) {
        if (bytes.length > MAX_FILE_BYTES) throw new Error("Fattura troppo grande (massimo 20 MiB).");
        return checkXmlText(decodeXml(/\.p7m$/i.test(filename) ? cmsPayload(bytes) : bytes));
    }
    function decodeAttachment(value) {
        if (value.length > Math.ceil(MAX_ATTACHMENT_BYTES / 3) * 4 + 1024 * 1024) throw new Error("Allegato troppo grande.");
        const clean = value.replace(/\s/g, "");
        if (clean.length > Math.ceil(MAX_ATTACHMENT_BYTES / 3) * 4 ||
            (clean.length % 4 !== 0 || /[^A-Za-z0-9+/=]/.test(clean) || !/^[^=]*={0,2}$/.test(clean) || (clean.indexOf("=") !== -1 && clean.indexOf("=") < clean.length - 2))) {
            throw new Error("Allegato Base64 non valido o troppo grande.");
        }
        const binary = atob(clean);
        if (binary.length > MAX_ATTACHMENT_BYTES) throw new Error("Allegato troppo grande.");
        return Uint8Array.from(binary, c => c.charCodeAt(0));
    }
    function isPdf(bytes) {
        return bytes.length >= 8 && String.fromCharCode(...bytes.subarray(0, 5)) === "%PDF-" &&
            /%%EOF\s*$/.test(new TextDecoder("windows-1252").decode(bytes.subarray(Math.max(0, bytes.length - 1024))));
    }
    return { MAX_FILE_BYTES, MAX_ATTACHMENT_BYTES, isSdiSender, isInvoiceFilename,
        extractInvoiceXml, checkXmlText, decodeAttachment, isPdf };
})();
if (typeof module !== "undefined") module.exports = InvoiceCore;

