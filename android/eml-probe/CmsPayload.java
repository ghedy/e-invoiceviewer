package it.acortina.einvoiceviewer;

import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.util.ArrayList;
import java.util.List;

/** Extracts encapsulated CMS SignedData bytes. It does not verify signatures. */
final class CmsPayload {
    static final int MAX_BYTES = 20 * 1024 * 1024;
    private static final int SEQUENCE = 0x30;
    private static final int SET = 0x31;
    private static final int EXPLICIT_CONTENT = 0xa0;
    private static final int[] PKCS7_OID_PREFIX = {0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d, 1, 7};

    private final byte[] bytes;
    private int nodeCount;

    private CmsPayload(byte[] bytes) { this.bytes = bytes; }

    static byte[] extract(byte[] bytes) throws IOException {
        if (bytes.length > MAX_BYTES) throw invalid();
        return new CmsPayload(bytes).extract();
    }

    private byte[] extract() throws IOException {
        Node root = read(0, bytes.length, 0);
        if (root.tag != SEQUENCE || root.next != bytes.length || root.children.size() != 2
                || !hasOid(root.children.get(0), 2)) throw invalid();
        Node wrapper = root.children.get(1);
        if (wrapper.tag != EXPLICIT_CONTENT || wrapper.children.size() != 1) throw invalid();
        Node signed = wrapper.children.get(0);
        int fields = signed.children.size();
        if (signed.tag != SEQUENCE || fields < 4 || signed.children.get(0).tag != 2
                || signed.children.get(1).tag != SET || signed.children.get(fields - 1).tag != SET) throw invalid();
        Node encap = signed.children.get(2);
        if (encap.tag != SEQUENCE || encap.children.size() != 2 || !hasOid(encap.children.get(0), 1)) throw invalid();
        Node content = encap.children.get(1);
        if (content.tag != EXPLICIT_CONTENT || content.children.size() != 1) throw invalid();
        ByteArrayOutputStream payload = new ByteArrayOutputStream();
        appendOctets(content.children.get(0), payload);
        return payload.toByteArray();
    }

    private boolean hasOid(Node node, int type) {
        if (node.tag != 6 || node.end - node.start != PKCS7_OID_PREFIX.length + 1) return false;
        for (int i = 0; i < PKCS7_OID_PREFIX.length; i++) {
            if ((bytes[node.start + i] & 0xff) != PKCS7_OID_PREFIX[i]) return false;
        }
        return bytes[node.end - 1] == type;
    }

    private Node read(int offset, int limit, int depth) throws IOException {
        if (++nodeCount > 20000 || depth > 32 || limit - offset < 2) throw invalid();
        int tag = bytes[offset++] & 0xff;
        if (tag == 0 || (tag & 31) == 31) throw invalid();
        long length = bytes[offset++] & 0xff;
        boolean indefinite = length == 128;
        boolean constructed = (tag & 32) != 0;
        if (indefinite && !constructed) throw invalid();
        if (length > 128) {
            int count = (int) length & 127;
            if (count > 4 || count > limit - offset) throw invalid();
            length = 0;
            for (int i = 0; i < count; i++) length = length * 256 + (bytes[offset++] & 0xff);
        }
        int start = offset;
        if (!indefinite && length > limit - start) throw invalid();
        int end = indefinite ? limit : start + (int) length;
        List<Node> children = new ArrayList<>();
        if (constructed) {
            while (offset < end) {
                if (indefinite && end - offset >= 2 && bytes[offset] == 0 && bytes[offset + 1] == 0) {
                    return new Node(tag, start, offset, offset + 2, children);
                }
                Node child = read(offset, end, depth + 1);
                children.add(child);
                offset = child.next;
            }
            if (indefinite) throw invalid();
        }
        return new Node(tag, start, end, end, children);
    }

    private void appendOctets(Node node, ByteArrayOutputStream output) throws IOException {
        if (node.tag == 4) {
            int count = node.end - node.start;
            if (count > MAX_BYTES - output.size()) throw invalid();
            output.write(bytes, node.start, count);
        } else if (node.tag == 36) {
            for (Node child : node.children) appendOctets(child, output);
        } else {
            throw invalid();
        }
    }

    private static IOException invalid() { return new IOException("Busta P7M non valida o priva di contenuto."); }

    private static final class Node {
        final int tag, start, end, next;
        final List<Node> children;

        Node(int tag, int start, int end, int next, List<Node> children) {
            this.tag = tag;
            this.start = start;
            this.end = end;
            this.next = next;
            this.children = children;
        }
    }
}
