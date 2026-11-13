package it.acortina.einvoiceviewer;

import java.io.ByteArrayInputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.StringWriter;
import java.util.Locale;
import javax.xml.parsers.ParserConfigurationException;
import javax.xml.parsers.SAXParserFactory;
import javax.xml.transform.Transformer;
import javax.xml.transform.TransformerException;
import javax.xml.transform.TransformerFactory;
import javax.xml.transform.URIResolver;
import javax.xml.transform.sax.SAXSource;
import javax.xml.transform.stream.StreamResult;
import javax.xml.transform.stream.StreamSource;
import org.xml.sax.InputSource;
import org.xml.sax.SAXException;
import org.xml.sax.XMLReader;

final class InvoiceHtml {
    private static final String HEAD = "<meta name=\"viewport\" content=\"width=840\">"
            + "<meta http-equiv=\"Content-Security-Policy\" content=\"default-src 'none'; "
            + "style-src 'unsafe-inline'; base-uri 'none'; form-action 'none'\">"
            + "<style>@page { margin: 10mm; } @media print {"
            + "body { margin: 0; }"
            // Keep the original A4 layout width while fitting it into 190 mm.
            + "#fattura-container { zoom: 90.47619%; }"
            + "#fattura-elettronica { min-width: 0 !important; }"
            + ".tbHeader, .tbTitolo, .tbFoglio, .tbNoBorder { width: 100% !important; }"
            + "td, th { word-wrap: break-word; overflow-wrap: anywhere; }"
            + "}</style>";
    private static final URIResolver NO_EXTERNAL_DOCUMENTS = (href, base) -> {
        throw new TransformerException("Riferimento esterno non consentito.");
    };

    private InvoiceHtml() {}

    /** Renders XML already checked by InvoiceSummary using the bundled stylesheet. */
    static String render(byte[] xml, InputStream stylesheet)
            throws IOException, TransformerException, ParserConfigurationException, SAXException {
        SAXParserFactory parser = SAXParserFactory.newInstance();
        parser.setNamespaceAware(true);
        XMLReader reader = parser.newSAXParser().getXMLReader();
        reader.setFeature("http://xml.org/sax/features/external-general-entities", false);
        reader.setFeature("http://xml.org/sax/features/external-parameter-entities", false);
        reader.setEntityResolver((publicId, systemId) -> {
            throw new SAXException("Entità esterna non consentita.");
        });
        TransformerFactory factory = TransformerFactory.newInstance();
        factory.setURIResolver(NO_EXTERNAL_DOCUMENTS);
        Transformer transformer = factory.newTransformer(new StreamSource(stylesheet));
        transformer.setURIResolver(NO_EXTERNAL_DOCUMENTS);
        StringWriter output = new StringWriter();
        InputSource input = new InputSource(new ByteArrayInputStream(xml));
        transformer.transform(new SAXSource(reader, input), new StreamResult(output));
        String html = output.toString();
        String lower = html.toLowerCase(Locale.ROOT);
        int head = lower.indexOf("<head>");
        if (head < 0 || !lower.contains("fattura-container")) {
            throw new IOException("Anteprima AssoInvoice non prodotta.");
        }
        int insert = head + "<head>".length();
        return html.substring(0, insert) + HEAD + html.substring(insert);
    }
}
