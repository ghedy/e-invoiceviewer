package it.acortina.einvoiceviewer;

import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.nio.charset.StandardCharsets;
import java.util.Locale;
import java.util.Properties;
import java.util.regex.Pattern;
import javax.mail.MessagingException;
import javax.mail.Part;
import javax.mail.Session;
import javax.mail.internet.MimeMessage;
import javax.mail.internet.MimeMultipart;
import javax.mail.util.ByteArrayDataSource;

final class SignedMail {
    private static final Pattern BOUNDARY = Pattern.compile("(?m)^--[^\\r\\n]{1,72}\\r?$");
    private static final Pattern HEADER = Pattern.compile("(?im)^(?:from|to|subject|date|received|message-id|mime-version|content-type):");
    private static final int MAX_XML_BYTES = 20 * 1024 * 1024;
    private final Session session = Session.getInstance(new Properties());
    private final XmlReader reader;
    private int remaining = 200;
    private boolean p7m;
    private String mimeFailure;
    private byte[] invoiceXml;

    interface XmlReader {
        String read(byte[] bytes, String name);
    }

    SignedMail(XmlReader reader) {
        this.reader = reader;
    }

    static final class Result {
        final String message;
        final byte[] xml;

        Result(String message, byte[] xml) {
            this.message = message;
            this.xml = xml;
        }
    }

    Result read(byte[] bytes, String mimeType, String filename) throws IOException, MessagingException {
        if (isP7m(filename, mimeType)) {
            String invoice = readP7m(bytes, filename);
            return new Result(invoice == null ? "P7M letto senza una fattura XML riconoscibile." : invoice, invoiceXml);
        }
        String prefix = new String(bytes, 0, Math.min(bytes.length, 8192), StandardCharsets.ISO_8859_1);
        java.util.regex.Matcher header = HEADER.matcher(prefix);
        java.util.regex.Matcher boundary = BOUNDARY.matcher(prefix);
        boolean hasHeader = header.find();
        boolean hasBoundary = boundary.find();
        String result = null;
        if (hasBoundary && (!hasHeader || boundary.start() < header.start())
                && mimeType != null && mimeType.toLowerCase(Locale.ROOT).startsWith("multipart/signed")) {
            // Gmail can supply the multipart body without the outer Content-Type header.
            result = visit(new MimeMultipart(new ByteArrayDataSource(bytes, mimeType)), true, 0);
        } else if (hasHeader) {
            result = visit(new MimeMessage(session, new ByteArrayInputStream(bytes)), 0);
        } else {
            result = readXml(bytes, filename);
        }
        if (result != null) return new Result(result, invoiceXml);
        if (mimeFailure != null) return new Result(mimeFailure, null);
        if (p7m) return new Result("Busta letta. Il P7M non contiene una fattura XML leggibile.", null);
        return new Result("Busta letta. Nessuna fattura XML individuata.", null);
    }

    private String visit(Part part, int depth) {
        if (depth > 8 || --remaining < 0) return null;
        String stage = "Intestazione MIME";
        String type = null;
        try {
            type = part.getContentType();
            if (part.isMimeType("multipart/*")) {
                stage = "Lettura delle parti MIME";
                try (InputStream input = part.getInputStream()) {
                    MimeMultipart multipart = new MimeMultipart(new ByteArrayDataSource(input, type));
                    return visit(multipart, part.isMimeType("multipart/signed"), depth);
                }
            }
            String name = part.getFileName();
            String lower = name == null ? "" : name.toLowerCase(Locale.ROOT);
            if (part.isMimeType("message/rfc822") || lower.endsWith(".eml")) {
                stage = "Lettura del messaggio EML interno";
                try (InputStream input = part.getInputStream()) {
                    return visit(new MimeMessage(session, input), depth + 1);
                }
            }
            if (lower.contains("_mt_") || "daticert.xml".equals(lower)) return null;
            if (isP7m(name, type)) {
                stage = "Decodifica dell'allegato P7M";
                p7m = true;
                try (InputStream input = part.getInputStream()) {
                    return readP7m(readLimited(input, CmsPayload.MAX_BYTES), name);
                }
            }
            if (!lower.endsWith(".xml") && !part.isMimeType("text/xml")
                    && !part.isMimeType("application/xml")) return null;
            stage = "Decodifica dell'allegato XML";
            try (InputStream input = part.getInputStream()) {
                return readXml(readLimited(input, MAX_XML_BYTES), name);
            }
        } catch (IOException | MessagingException error) {
            if (mimeFailure == null) mimeFailure = describeFailure(stage, type, depth, error);
            return null;
        }
    }

    static String describeFailure(String stage, String type, int depth, Exception error) {
        String mime = type == null ? "" : type.split(";", 2)[0].trim();
        if (!mime.matches("[a-zA-Z0-9.+_-]{1,40}/[a-zA-Z0-9.+_-]{1,60}")) mime = "non disponibile";
        String message = error.getMessage() == null ? "" : error.getMessage().toLowerCase(Locale.ROOT);
        String reason = error.getClass().getSimpleName();
        if (message.contains("boundary")) reason += " / delimitatore MIME";
        else if (message.contains("base64")) reason += " / Base64";
        else if (message.contains("encoding")) reason += " / codifica";
        return "Parte della busta non leggibile."
                + "\nFase: " + stage
                + "\nParte MIME: " + mime
                + "\nLivello: " + depth
                + "\nDiagnosi: " + reason;
    }

    private String readP7m(byte[] bytes, String name) {
        try {
            return readXml(CmsPayload.extract(bytes), name);
        } catch (IOException error) {
            return null;
        }
    }

    private String readXml(byte[] bytes, String name) {
        String summary = reader.read(bytes, name);
        if (summary != null) invoiceXml = bytes;
        return summary;
    }

    private static boolean isP7m(String name, String mimeType) {
        if (name != null && name.toLowerCase(Locale.ROOT).endsWith(".p7m")) return true;
        if (mimeType == null) return false;
        String type = mimeType.split(";", 2)[0].trim().toLowerCase(Locale.ROOT);
        return "application/pkcs7-mime".equals(type) || "application/x-pkcs7-mime".equals(type);
    }

    private String visit(MimeMultipart multipart, boolean signed, int depth) throws IOException, MessagingException {
        int count = multipart.getCount();
        if (signed) count = Math.min(count, 1);
        for (int i = 0; i < count && remaining > 0; i++) {
            String result = visit(multipart.getBodyPart(i), depth + 1);
            if (result != null) return result;
        }
        return null;
    }

    static byte[] readLimited(InputStream input, int limit) throws IOException {
        ByteArrayOutputStream output = new ByteArrayOutputStream();
        byte[] buffer = new byte[8192];
        for (int count; (count = input.read(buffer)) != -1;) {
            if (output.size() + count > limit) throw new IOException("File oltre il limite di lettura.");
            output.write(buffer, 0, count);
        }
        return output.toByteArray();
    }
}
