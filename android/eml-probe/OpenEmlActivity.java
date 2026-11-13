package it.acortina.einvoiceviewer;

import android.app.Activity;
import android.content.ActivityNotFoundException;
import android.content.ContentProviderClient;
import android.content.Intent;
import android.content.res.AssetFileDescriptor;
import android.database.Cursor;
import android.graphics.Insets;
import android.net.Uri;
import android.os.Build;
import android.os.Bundle;
import android.os.CancellationSignal;
import android.os.ParcelFileDescriptor;
import android.os.RemoteException;
import android.print.PageRange;
import android.print.PrintAttributes;
import android.print.PrintDocumentAdapter;
import android.print.PrintManager;
import android.view.Gravity;
import android.provider.OpenableColumns;
import android.view.View;
import android.view.ViewGroup;
import android.view.WindowInsets;
import android.webkit.WebResourceRequest;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;
import android.widget.Button;
import android.widget.ImageView;
import android.widget.LinearLayout;
import android.widget.TextView;
import android.widget.Toast;

import java.io.IOException;
import java.io.InputStream;
import javax.mail.MessagingException;

public final class OpenEmlActivity extends Activity {
    private static final int OPEN_EML = 1;
    private static final int MAX_BYTES = 32 * 1024 * 1024;
    private static final String INVOICE_ORIGIN = "https://invoice-viewer.invalid/";
    private static final String WEBSITE = "https://quadra.acortina.it/";
    private TextView status;
    private WebView preview;
    private Button printButton;
    private String invoiceHtml;
    private WebView printView;

    private static final class ViewResult {
        final String message;
        final String html;

        ViewResult(String message, String html) {
            this.message = message;
            this.html = html;
        }
    }
    private int request;

    @Override
    protected void onCreate(Bundle state) {
        super.onCreate(state);
        LinearLayout layout = new LinearLayout(this);
        layout.setOrientation(LinearLayout.VERTICAL);
        int padding = dp(16);
        layout.setPadding(0, padding, 0, 0);
        if (Build.VERSION.SDK_INT >= 30) {
            layout.setOnApplyWindowInsetsListener((view, insets) -> {
                Insets bars = insets.getInsets(WindowInsets.Type.systemBars());
                view.setPadding(bars.left, padding + bars.top, bars.right, bars.bottom);
                return insets;
            });
        }
        layout.addView(createHeader(padding));
        status = new TextView(this);
        status.setTextSize(16);
        status.setPadding(padding, 0, padding, padding / 2);
        status.setText("Apri l'allegato della PEC oppure scegli un file.");
        status.setAccessibilityLiveRegion(TextView.ACCESSIBILITY_LIVE_REGION_POLITE);
        layout.addView(status);
        Button picker = new Button(this);
        picker.setText("Apri file EML, XML o P7M");
        picker.setOnClickListener(view -> {
            Intent select = new Intent(Intent.ACTION_OPEN_DOCUMENT);
            select.addCategory(Intent.CATEGORY_OPENABLE);
            select.setType("*/*");
            startActivityForResult(select, OPEN_EML);
        });
        LinearLayout actions = new LinearLayout(this);
        actions.setGravity(Gravity.CENTER_VERTICAL);
        actions.setPadding(padding, 0, padding, 0);
        actions.addView(picker, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WRAP_CONTENT, 1));
        printButton = new Button(this);
        printButton.setText("Stampa");
        printButton.setVisibility(View.GONE);
        printButton.setOnClickListener(view -> printInvoice());
        actions.addView(printButton);
        layout.addView(actions);
        preview = createInvoiceWebView();
        WebSettings web = preview.getSettings();
        web.setUseWideViewPort(true);
        web.setLoadWithOverviewMode(true);
        web.setBuiltInZoomControls(true);
        web.setDisplayZoomControls(false);
        preview.setWebViewClient(new InvoiceWebViewClient());
        preview.setVisibility(View.GONE);
        layout.addView(preview, new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT, 0, 1));
        setContentView(layout);

        Intent intent = getIntent();
        if (Intent.ACTION_VIEW.equals(intent.getAction())) {
            inspect(intent.getData(), "Apertura da un’altra app", intent.getType());
        } else if (Intent.ACTION_SEND.equals(intent.getAction())) {
            Uri uri = Build.VERSION.SDK_INT >= 33
                    ? intent.getParcelableExtra(Intent.EXTRA_STREAM, Uri.class)
                    : intent.getParcelableExtra(Intent.EXTRA_STREAM);
            if (uri == null && intent.getClipData() != null && intent.getClipData().getItemCount() > 0) {
                uri = intent.getClipData().getItemAt(0).getUri();
            }
            inspect(uri, "Condivisione da un’altra app", intent.getType());
        }
    }

    private LinearLayout createHeader(int padding) {
        LinearLayout header = new LinearLayout(this);
        header.setGravity(Gravity.CENTER_VERTICAL);
        header.setPadding(padding, 0, padding, dp(8));
        ImageView logo = new ImageView(this);
        logo.setImageResource(getApplicationInfo().icon);
        logo.setImportantForAccessibility(View.IMPORTANT_FOR_ACCESSIBILITY_NO);
        header.addView(logo, new LinearLayout.LayoutParams(dp(56), dp(56)));
        LinearLayout labels = new LinearLayout(this);
        labels.setOrientation(LinearLayout.VERTICAL);
        labels.setGravity(Gravity.CENTER_VERTICAL);
        labels.setMinimumHeight(dp(56));
        labels.setPadding(dp(8), 0, 0, 0);
        header.addView(labels, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WRAP_CONTENT, 1));
        TextView title = new TextView(this);
        title.setText(getApplicationInfo().loadLabel(getPackageManager()));
        title.setTextAppearance(android.R.style.TextAppearance_Material_Title);
        title.setTextSize(24);
        title.setIncludeFontPadding(false);
        labels.addView(title);
        Button website = new Button(this, null, android.R.attr.borderlessButtonStyle);
        website.setText("quadra.acortina.it");
        website.setAllCaps(false);
        website.setMinHeight(dp(28));
        website.setMinimumHeight(dp(28));
        website.setGravity(Gravity.START | Gravity.CENTER_VERTICAL);
        website.setPadding(0, 0, 0, 0);
        website.setOnClickListener(view -> {
            try {
                startActivity(new Intent(Intent.ACTION_VIEW, Uri.parse(WEBSITE)));
            } catch (ActivityNotFoundException error) {
                Toast.makeText(this, "Nessun browser disponibile.", Toast.LENGTH_SHORT).show();
            }
        });
        labels.addView(website);
        return header;
    }

    private int dp(int value) {
        return Math.round(value * getResources().getDisplayMetrics().density);
    }

    private WebView createInvoiceWebView() {
        WebView webView = new WebView(this);
        WebSettings web = webView.getSettings();
        web.setJavaScriptEnabled(false);
        web.setAllowFileAccess(false);
        web.setAllowContentAccess(false);
        web.setBlockNetworkLoads(true);
        return webView;
    }

    private static class InvoiceWebViewClient extends WebViewClient {
        @Override
        public boolean shouldOverrideUrlLoading(WebView view, WebResourceRequest request) {
            return true;
        }
    }

    private void printInvoice() {
        if (invoiceHtml == null || printView != null) return;
        PrintManager manager = getSystemService(PrintManager.class);
        if (manager == null) {
            showPrintError();
            return;
        }
        printButton.setEnabled(false);
        printView = createInvoiceWebView();
        printView.setWebViewClient(new InvoiceWebViewClient() {
            @Override
            public void onPageFinished(WebView view, String url) {
                if (view != printView || isDestroyed()) return;
                view.setWebViewClient(new InvoiceWebViewClient());
                String title = "Fattura elettronica";
                PrintAttributes attributes = new PrintAttributes.Builder()
                        .setMediaSize(PrintAttributes.MediaSize.ISO_A4)
                        .build();
                try {
                    PrintDocumentAdapter document = view.createPrintDocumentAdapter(title);
                    if (manager.print(title, new PrintDocumentAdapter() {
                        @Override
                        public void onStart() {
                            document.onStart();
                        }

                        @Override
                        public void onLayout(PrintAttributes oldAttributes, PrintAttributes newAttributes,
                                CancellationSignal cancellation, LayoutResultCallback callback, Bundle extras) {
                            document.onLayout(oldAttributes, newAttributes, cancellation, callback, extras);
                        }

                        @Override
                        public void onWrite(PageRange[] pages, ParcelFileDescriptor destination,
                                CancellationSignal cancellation, WriteResultCallback callback) {
                            document.onWrite(pages, destination, cancellation, callback);
                        }

                        @Override
                        public void onFinish() {
                            try {
                                document.onFinish();
                            } finally {
                                releasePrintView();
                            }
                        }
                    }, attributes) == null) {
                        releasePrintView();
                        showPrintError();
                    }
                } catch (IllegalStateException | IllegalArgumentException error) {
                    releasePrintView();
                    showPrintError();
                }
            }
        });
        printView.loadDataWithBaseURL(INVOICE_ORIGIN, invoiceHtml, "text/html", "UTF-8", null);
    }

    private void showPrintError() {
        Toast.makeText(this, "Impossibile avviare la stampa.", Toast.LENGTH_SHORT).show();
    }

    private void releasePrintView() {
        if (printView == null) return;
        printView.destroy();
        printView = null;
        printButton.setEnabled(true);
    }

    @Override
    protected void onActivityResult(int code, int result, Intent data) {
        super.onActivityResult(code, result, data);
        if (code == OPEN_EML && result == RESULT_OK && data != null) {
            inspect(data.getData(), "Selezione da Archivio", null);
        }
    }

    private void inspect(Uri uri, String source, String mimeType) {
        int current = ++request;
        invoiceHtml = null;
        printButton.setVisibility(View.GONE);
        preview.setVisibility(View.GONE);
        preview.loadUrl("about:blank");
        if (uri == null || !"content".equals(uri.getScheme())) {
            status.setText("L’app di origine non ha passato un file leggibile.");
            return;
        }
        status.setText("Lettura del file…");
        new Thread(() -> {
            String providerType;
            try {
                providerType = getContentResolver().getType(uri);
            } catch (RuntimeException error) {
                providerType = null;
            }
            ViewResult outcome;
            try {
                outcome = readEml(uri, source, mimeType == null ? providerType : mimeType);
            } catch (SecurityException error) {
                outcome = new ViewResult(source + "\n\nAccesso al file non concesso da Android.", null);
            } catch (IOException | RuntimeException error) {
                outcome = new ViewResult(source + "\n\nImpossibile leggere il file ricevuto.", null);
            }
            String message = outcome.message
                    + "\nTipo dichiarato: " + (mimeType == null ? "non indicato" : mimeType)
                    + "\nTipo del file: " + (providerType == null ? "non disponibile" : providerType)
                    + "\nProvider: " + (uri.getAuthority() == null ? "non disponibile" : uri.getAuthority());
            String html = outcome.html;
            runOnUiThread(() -> {
                if (current != request || isDestroyed()) return;
                if (html == null) {
                    status.setText(message);
                } else {
                    status.setText("Foglio di stile AssoInvoice · AssoSoftware\nFirma digitale non verificata.");
                    invoiceHtml = html;
                    preview.loadDataWithBaseURL(INVOICE_ORIGIN, html, "text/html", "UTF-8", null);
                    preview.setVisibility(View.VISIBLE);
                    printButton.setVisibility(View.VISIBLE);
                }
            });
        }, "eml-probe").start();
    }

    private ViewResult readEml(Uri uri, String source, String mimeType) throws IOException {
        String filename = null;
        Long size = null;
        try (Cursor cursor = getContentResolver().query(uri,
                new String[] {OpenableColumns.DISPLAY_NAME, OpenableColumns.SIZE}, null, null, null)) {
            if (cursor != null && cursor.moveToFirst()) {
                int nameColumn = cursor.getColumnIndex(OpenableColumns.DISPLAY_NAME);
                int sizeColumn = cursor.getColumnIndex(OpenableColumns.SIZE);
                if (nameColumn >= 0) filename = cursor.getString(nameColumn);
                if (sizeColumn >= 0 && !cursor.isNull(sizeColumn)) size = cursor.getLong(sizeColumn);
            }
        } catch (RuntimeException unavailableName) {
            // Some providers allow opening the stream without exposing metadata.
        }
        String label = filename == null ? "Nome file non disponibile" : filename;
        byte[] data;
        try (InputStream stream = getContentResolver().openInputStream(uri)) {
            if (stream == null) throw new IOException("Stream unavailable");
            data = SignedMail.readLimited(stream, MAX_BYTES);
        }
        String details = "Flusso standard: " + data.length + " byte";
        if (data.length == 0) {
            StringBuilder attempts = new StringBuilder(details);
            data = retryEmptyRead(uri, mimeType, attempts);
            details = attempts.toString();
        }
        String result;
        if (data.length == 0) {
            result = "Riferimento ricevuto, ma il provider non ha restituito dati da leggere.";
        } else {
            try {
                SignedMail.Result invoice = new SignedMail(InvoiceSummary::read).read(data, mimeType, filename);
                result = invoice.message;
                if (invoice.xml != null) {
                    try (InputStream style = getAssets().open("assofattura.xsl")) {
                        return new ViewResult(result, InvoiceHtml.render(invoice.xml, style));
                    } catch (IOException | javax.xml.transform.TransformerException
                            | javax.xml.parsers.ParserConfigurationException | org.xml.sax.SAXException
                            | javax.xml.transform.TransformerFactoryConfigurationError error) {
                        result += "\n\nAnteprima AssoInvoice non disponibile.";
                    }
                }
            } catch (MessagingException | IOException error) {
                result = SignedMail.describeFailure("Apertura della busta esterna", mimeType, 0, error);
            }
        }
        return new ViewResult(result + "\n\n" + source + "\n" + label + "\n" + details
                + "\nDimensione dichiarata: " + (size == null || size < 0 ? "non disponibile" : size + " byte"), null);
    }

    @Override
    protected void onDestroy() {
        releasePrintView();
        ((ViewGroup) preview.getParent()).removeView(preview);
        preview.destroy();
        super.onDestroy();
    }

    private byte[] retryEmptyRead(Uri uri, String mimeType, StringBuilder details) {
        if (mimeType != null) {
            try (AssetFileDescriptor asset = getContentResolver().openTypedAssetFileDescriptor(uri, mimeType, null)) {
                if (asset == null) throw new IOException("Stream unavailable");
                byte[] data = SignedMail.readLimited(asset.createInputStream(), MAX_BYTES);
                details.append("\nLettura per tipo: ").append(data.length).append(" byte");
                if (data.length > 0) return data;
            } catch (IOException | RuntimeException error) {
                details.append("\nLettura per tipo: ").append(error.getClass().getSimpleName());
            }
        }
        try (ContentProviderClient client = getContentResolver().acquireUnstableContentProviderClient(uri)) {
            if (client == null) throw new IOException("Provider unavailable");
            ParcelFileDescriptor file = client.openFile(uri, "r");
            if (file == null) throw new IOException("Descriptor unavailable");
            try (InputStream stream = new ParcelFileDescriptor.AutoCloseInputStream(file)) {
                byte[] data = SignedMail.readLimited(stream, MAX_BYTES);
                details.append("\nLettura diretta: ").append(data.length).append(" byte");
                return data;
            }
        } catch (IOException | RemoteException | RuntimeException error) {
            details.append("\nLettura diretta: ").append(error.getClass().getSimpleName());
            return new byte[0];
        }
    }
}
