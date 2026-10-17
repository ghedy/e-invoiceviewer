using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace EInvoiceViewer.OutlookClassic {
    public sealed class ViewerForm : Form {
        private readonly WebView2 web = new WebView2();
        private readonly ComboBox invoices = new ComboBox();
        private readonly ComboBox files = new ComboBox();
        private readonly Button print = new Button { Text="Stampa fattura", AutoSize=true, Enabled=false };
        private readonly Button open = new Button { Text="Apri PDF / Salva allegato", AutoSize=true, Enabled=false };
        private readonly Label status = new Label {AutoSize=true, MaximumSize=new Size(1000,0)};
        private readonly string stylesheet;
        private readonly string session = Path.Combine(Path.GetTempPath(),"e-invoiceviewer-outlook",Guid.NewGuid().ToString("N"));
        private ulong navigationId;
        private string expectedDocumentUri;
        private string invoiceStatus;
        private bool validInvoice;
        private bool ready;
        public ViewerForm(IList<Invoice> data,string xsl) {
            stylesheet=xsl;
            Text="e-Invoice Viewer — Outlook classico (prototipo)";
            Width=1060; Height=850;
            Font=new Font("Segoe UI",9);
            FlowLayoutPanel toolbar=new FlowLayoutPanel {Dock=DockStyle.Top,AutoSize=true,Padding=new Padding(8),WrapContents=true};
            invoices.DropDownStyle=ComboBoxStyle.DropDownList; invoices.Width=360;
            files.DropDownStyle=ComboBoxStyle.DropDownList; files.Width=250;
            toolbar.Controls.AddRange(new Control[]{invoices,print,files,open,status});
            web.Dock=DockStyle.Fill;
            web.CreationProperties=new CoreWebView2CreationProperties {
                UserDataFolder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"e-InvoiceViewer","Outlook","WebView2"),
                IsInPrivateModeEnabled=true
            };
            Controls.Add(web); Controls.Add(toolbar);
            invoices.DataSource=data;
            invoices.SelectedIndexChanged+=delegate { if (ready) ShowInvoice(); };
            files.SelectedIndexChanged+=delegate {open.Enabled=files.SelectedItem!=null;};
            print.Click+=delegate {
                try { if (ready && print.Enabled) web.CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.Browser); }
                catch {MessageBox.Show(this,"Impossibile avviare la stampa.","Stampa");}
            };
            open.Click+=delegate {SaveOrOpen();};
            Shown+=async delegate {
                try {
                    await web.EnsureCoreWebView2Async();
                    if (IsDisposed) return;
                    web.CoreWebView2.Settings.IsScriptEnabled=false;
                    web.CoreWebView2.Settings.AreHostObjectsAllowed=false;
                    web.CoreWebView2.Settings.IsWebMessageEnabled=false;
                    web.CoreWebView2.Settings.IsGeneralAutofillEnabled=false;
                    web.CoreWebView2.Settings.IsPasswordAutosaveEnabled=false;
                    web.CoreWebView2.NavigationStarting+=(s,e)=>{
                        if(e.Uri!="about:blank" && !String.Equals(e.Uri,expectedDocumentUri,StringComparison.Ordinal)) {
                            e.Cancel=true;
                            status.Text="Anteprima bloccata: richiesta di navigazione non consentita.";
                        } else {navigationId=e.NavigationId;print.Enabled=false;}
                    };
                    web.CoreWebView2.NavigationCompleted+=(s,e)=>{
                        if(IsDisposed || e.NavigationId!=navigationId) return;
                        print.Enabled=validInvoice && e.IsSuccess;
                        status.Text=e.IsSuccess ? invoiceStatus :
                            "Impossibile caricare l’anteprima ("+e.WebErrorStatus+"). Gli allegati restano disponibili.";
                    };
                    web.CoreWebView2.NewWindowRequested+=(s,e)=>{e.Handled=true;};
                    web.CoreWebView2.DownloadStarting+=(s,e)=>{e.Cancel=true;};
                    web.CoreWebView2.PermissionRequested+=(s,e)=>{e.State=CoreWebView2PermissionState.Deny;};
                    ready=true; ShowInvoice();
                } catch(Exception error) {
                    status.Text="Anteprima non disponibile ("+error.GetType().Name+"). Verifica Microsoft Edge WebView2 Runtime.";
                }
            };
            FormClosed+=delegate {
                validInvoice=false;
                web.Dispose();
                // Delete only files created in this exact session; do not recurse.
                try { if(Directory.Exists(session)) {foreach(string file in Directory.GetFiles(session)) File.Delete(file); Directory.Delete(session);} }
                catch(IOException) {} catch(UnauthorizedAccessException) {}
            };
        }
        private void ShowInvoice() {
            validInvoice=false;
            expectedDocumentUri=null;
            print.Enabled=false; open.Enabled=false; files.DataSource=null;
            Invoice invoice=invoices.SelectedItem as Invoice;
            if(invoice==null) return;
            status.Text=(invoice.SenderMatches ? "Mittente dichiarato SdI. " : invoice.ContainerSenderMatches ? "Mittente dichiarato SdI nella PEC contenitore. " : "Provenienza non verificata. ")+
                "Firma digitale, PEC e sicurezza degli allegati non verificate.";
            try {
                string html=InvoiceEngine.Render(invoice,stylesheet);
                try {files.DataSource=InvoiceEngine.Embedded(invoice);}
                catch {status.Text+=" Alcuni allegati non sono leggibili o superano il limite.";}
                validInvoice=true;
                invoiceStatus=status.Text;
                NavigateHtml(html);
            } catch(Exception error) {
                validInvoice=false;
                invoiceStatus=error.Message;
                NavigateHtml("<html><body>Impossibile visualizzare la fattura.</body></html>");
                status.Text=error.Message; files.DataSource=null; open.Enabled=false;
            }
        }
        private void NavigateHtml(string html) {
            // Some WebView2 versions expose in-memory HTML as a data URL.
            // Authorize exactly our current generated document, never arbitrary data URLs.
            expectedDocumentUri="data:text/html;charset=utf-8;base64,"+Convert.ToBase64String(Encoding.UTF8.GetBytes(html));
            web.CoreWebView2.Navigate(expectedDocumentUri);
        }
        private void SaveOrOpen() {
            EmbeddedFile attachment=files.SelectedItem as EmbeddedFile;
            if(attachment==null) return;
            try {
                if(attachment.Pdf) {
                    Directory.CreateDirectory(session);
                    string path=Path.Combine(session,Guid.NewGuid().ToString("N")+".pdf");
                    File.WriteAllBytes(path,attachment.Bytes);
                    Process.Start(new ProcessStartInfo(path){UseShellExecute=true});
                } else using(SaveFileDialog dialog=new SaveFileDialog {FileName=attachment.Name,Filter="Tutti i file|*.*",OverwritePrompt=true}) {
                    if(dialog.ShowDialog(this)==DialogResult.OK) File.WriteAllBytes(dialog.FileName,attachment.Bytes);
                }
            } catch {MessageBox.Show(this,"Impossibile aprire o salvare l’allegato.","Allegato");}
        }
    }
}
