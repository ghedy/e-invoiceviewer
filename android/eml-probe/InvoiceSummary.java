package it.acortina.einvoiceviewer;

import android.util.Xml;
import org.xmlpull.v1.XmlPullParser;
import org.xmlpull.v1.XmlPullParserException;

import java.io.ByteArrayInputStream;
import java.io.IOException;
import java.util.HashMap;
import java.util.Map;

final class InvoiceSummary {
    private static final String[] FIELDS = {"Numero", "Data", "ImportoTotaleDocumento", "Divisa"};

    private InvoiceSummary() {}

    static String read(byte[] bytes, String filename) {
        Map<String, String> values = new HashMap<>();
        try {
            XmlPullParser parser = Xml.newPullParser();
            parser.setFeature(XmlPullParser.FEATURE_PROCESS_NAMESPACES, true);
            parser.setFeature(XmlPullParser.FEATURE_PROCESS_DOCDECL, false);
            parser.setInput(new ByteArrayInputStream(bytes), null);
            int event = parser.nextToken();
            while (event != XmlPullParser.START_TAG && event != XmlPullParser.END_DOCUMENT) {
                if (event == XmlPullParser.DOCDECL) return null;
                event = parser.nextToken();
            }
            if (event != XmlPullParser.START_TAG || !isInvoiceRoot(parser)) return null;
            int documentDepth = 0;
            int events = 0;
            for (event = parser.next(); event != XmlPullParser.END_DOCUMENT; event = parser.next()) {
                if (++events > 200000 || parser.getDepth() > 64) return null;
                if (event == XmlPullParser.START_TAG && "DatiGeneraliDocumento".equals(parser.getName())) {
                    documentDepth = parser.getDepth();
                } else if (event == XmlPullParser.START_TAG && documentDepth > 0 && parser.getDepth() == documentDepth + 1) {
                    for (String field : FIELDS) {
                        if (field.equals(parser.getName())) values.put(field, parser.nextText());
                    }
                } else if (event == XmlPullParser.END_TAG && documentDepth > 0 && parser.getDepth() == documentDepth) {
                    documentDepth = 0;
                }
            }
        } catch (XmlPullParserException | IOException error) {
            return null;
        }
        StringBuilder summary = new StringBuilder("Fattura elettronica trovata");
        if (filename != null) summary.append("\n").append(filename);
        append(summary, "Numero", values.get("Numero"));
        append(summary, "Data", values.get("Data"));
        String amount = values.get("ImportoTotaleDocumento");
        String currency = values.get("Divisa");
        if (amount != null) append(summary, "Totale", amount + (currency == null ? "" : " " + currency));
        return summary.append("\n\nFirma digitale non verificata.").toString();
    }

    private static boolean isInvoiceRoot(XmlPullParser parser) {
        String name = parser.getName();
        if (!"FatturaElettronica".equals(name) && !"FatturaElettronicaSemplificata".equals(name)) return false;
        String ns = parser.getNamespace();
        return "http://ivaservizi.agenziaentrate.gov.it/docs/xsd/fatture/v1.2".equals(ns)
                || "http://ivaservizi.agenziaentrate.gov.it/docs/xsd/fatture/v1.0".equals(ns)
                || "http://www.fatturapa.gov.it/sdi/fatturapa/v1.1".equals(ns)
                || "http://www.fatturapa.gov.it/sdi/fatturapa/v1.0".equals(ns);
    }

    private static void append(StringBuilder summary, String label, String value) {
        if (value != null && !value.isEmpty()) summary.append("\n").append(label).append(": ").append(value);
    }
}
