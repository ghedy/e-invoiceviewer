"use strict";
// No DOM or mail APIs. Use localized errors when running in the extension.
const InvoiceCore = (() => {
    function localizedError(key, fallback) {
        return new Error(typeof browser === "undefined" ? fallback : browser.i18n.getMessage(key) || fallback);
    }
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
            throw localizedError("xmlRejected", "XML is not allowed or is too large.");
        }
        // A processing instruction such as xml-stylesheet may appear before the root.
        // Read each prolog item up to its own terminator. The viewer parses the full XML.
        const declaration = /^<\?xml\s[^?]*\?>/.exec(xml);
        let offset = declaration ? declaration[0].length : 0;
        const prologItem = /\s*(<!--|<\?(?!xml(?=\s|\?>))[^?\s<>]+(?=\s|\?>))/iy;
        prologItem.lastIndex = offset;
        for (let item = prologItem.exec(xml); item; item = prologItem.exec(xml)) {
            const terminator = item[1] === "<!--" ? "-->" : "?>";
            const end = xml.indexOf(terminator, prologItem.lastIndex);
            if (end === -1) break;
            offset = end + terminator.length;
            prologItem.lastIndex = offset;
        }
        const invoiceRoot = /\s*<(?:[\w.-]+:)?FatturaElettronica(?:Semplificata)?(?:\s|>)/y;
        invoiceRoot.lastIndex = offset;
        if (!invoiceRoot.test(xml)) {
            throw localizedError("notInvoice", "The content is not an XML electronic invoice.");
        }
        return xml;
    }
    // Bounded BER parsing identifies CMS eContent. It does NOT verify signatures.
    function cmsPayload(bytes) {
        let nodes = 0;
        function read(offset, limit, depth = 0) {
            if (++nodes > 20000 || depth > 32 || offset + 2 > limit) throw localizedError("invalidP7m", "Unsupported or malformed P7M envelope.");
            const tag = bytes[offset++];
            if ((tag & 31) === 31 || tag === 0) throw localizedError("invalidBerTag", "Unsupported BER tag.");
            let length = bytes[offset++];
            const indefinite = length === 0x80;
            if (indefinite && !(tag & 32)) throw localizedError("invalidBerLength", "Invalid BER length.");
            if (!indefinite && (length & 128)) {
                const count = length & 127;
                if (!count || count > 4 || offset + count > limit) throw localizedError("invalidBerLength", "Invalid BER length.");
                length = 0;
                for (let i = 0; i < count; i++) length = length * 256 + bytes[offset++];
            }
            const start = offset;
            const end = indefinite ? limit : start + length;
            if (end > limit) throw localizedError("truncatedP7m", "Truncated P7M envelope.");
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
                if (indefinite) throw localizedError("missingP7mEnd", "Missing P7M end marker.");
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
            signed.children.at(-1)?.tag !== 49) throw localizedError("unsupportedCms", "Unsupported CMS SignedData envelope.");
        const encap = signed.children[2];
        const content = encap?.children[1];
        if (encap?.tag !== 48 || oid(encap.children[0]) !== "42,134,72,134,247,13,1,7,1" ||
            encap.children.length !== 2 || content?.tag !== 160 || content.children.length !== 1) {
            throw localizedError("missingP7mXml", "Missing or unsupported XML content in the P7M envelope.");
        }
        const chunks = [];
        let total = 0;
        function collect(node) {
            if (node.tag === 4) {
                total += node.end - node.start;
                if (total > MAX_FILE_BYTES) throw localizedError("p7mTooLarge", "P7M content is too large.");
                chunks.push(bytes.subarray(node.start, node.end));
            } else if (node.tag === 36) node.children.forEach(collect);
            else throw localizedError("unsupportedP7m", "Unsupported P7M content.");
        }
        collect(content.children[0]);
        const payload = new Uint8Array(total);
        let offset = 0;
        for (const chunk of chunks) { payload.set(chunk, offset); offset += chunk.length; }
        return payload;
    }
    function extractInvoiceXml(bytes, filename) {
        if (bytes.length > MAX_FILE_BYTES) throw localizedError("invoiceTooLarge", "Invoice is too large (maximum 20 MiB).");
        return checkXmlText(decodeXml(/\.p7m$/i.test(filename) ? cmsPayload(bytes) : bytes));
    }
    function decodeAttachment(value) {
        if (value.length > Math.ceil(MAX_ATTACHMENT_BYTES / 3) * 4 + 1024 * 1024) throw localizedError("attachmentTooLarge", "Attachment is too large.");
        const clean = value.replace(/\s/g, "");
        if (clean.length > Math.ceil(MAX_ATTACHMENT_BYTES / 3) * 4 ||
            (clean.length % 4 !== 0 || /[^A-Za-z0-9+/=]/.test(clean) || !/^[^=]*={0,2}$/.test(clean) || (clean.indexOf("=") !== -1 && clean.indexOf("=") < clean.length - 2))) {
            throw localizedError("invalidBase64", "Invalid or oversized Base64 attachment.");
        }
        const binary = atob(clean);
        if (binary.length > MAX_ATTACHMENT_BYTES) throw localizedError("attachmentTooLarge", "Attachment is too large.");
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

