using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace EInvoiceViewer.OutlookClassic {
    internal static class Preview {
        [STAThread] private static int Main(string[] args) {
            try {
                bool jobMode=args.Length>=2 && args[0]=="--job";
                bool renderOnly=(args.Length==3 && args[1]=="--render") || (jobMode && args.Length==4 && args[2]=="--render");
                // WinForms must be initialized before OpenFileDialog creates any HWND.
                if(!renderOnly) {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                }
                if(args.Length==1 && args[0]=="--startup-test") {
                    // Exercise native window creation without displaying a window.
                    using(Form probe=new Form()) {
                        IntPtr handle=probe.Handle;
                        if(handle==IntPtr.Zero) throw new InvalidOperationException("Creazione finestra non riuscita.");
                    }
                    return 0;
                }
                IList<Invoice> invoices;
                if(jobMode) {
                    Diagnostics.Write("preview job enter");
                    invoices=ReadJob(args[1]);
                    Diagnostics.Write("preview job loaded");
                } else {
                    string input=null;
                    if(args.Length>0) input=args[0];
                    else using(OpenFileDialog picker=new OpenFileDialog {Filter="Fatture e messaggi|*.xml;*.p7m;*.eml",Title="Apri un documento di prova"}) {
                        if(picker.ShowDialog()!=DialogResult.OK) return 0;
                        input=picker.FileName;
                    }
                    if(new FileInfo(input).Length>InvoiceEngine.MaxMessage) throw new InvalidDataException("File troppo grande.");
                    byte[] bytes=File.ReadAllBytes(input);
                    invoices=input.EndsWith(".eml",StringComparison.OrdinalIgnoreCase) ?
                        InvoiceEngine.ReadEml(bytes,true) : new List<Invoice>{InvoiceEngine.ReadInvoice(bytes,Path.GetFileName(input),false)};
                }
                if(invoices.Count==0) throw new InvalidDataException("Nessuna fattura trovata.");
                string xsl=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"shared","assets","assofattura.xsl");
                if(renderOnly) {
                    File.WriteAllText(args[args.Length-1],InvoiceEngine.Render(invoices[0],xsl));
                    return 0;
                }
                Application.Run(new ViewerForm(invoices,xsl));
                return 0;
            } catch(Exception error) {
                Diagnostics.Write("preview failed",error);
                if((args.Length>=3 && args[args.Length-2]=="--render") || (args.Length==1 && args[0]=="--startup-test")) {
                    Console.Error.WriteLine(error.Message); return 1;
                }
                MessageBox.Show(error.Message,"e-Invoice Viewer — prova locale");
                return 1;
            }
        }
        private static IList<Invoice> ReadJob(string manifest) {
            List<Invoice> invoices=new List<Invoice>();
            // Validate the entire request before reading or cleaning its files.
            InvoiceJob job=JobProtocol.Read(manifest);
            string folder=Path.GetDirectoryName(Path.GetFullPath(manifest));
            int failures=0;
            try {
                foreach(InvoiceJobAttachment attachment in job.Attachments) {
                    try {
                        byte[] bytes=File.ReadAllBytes(Path.Combine(folder,attachment.File));
                        if(attachment.Eml) invoices.AddRange(InvoiceEngine.ReadEml(bytes,job.AllowAnySender));
                        else if(MailPolicy.IsInvoiceName(attachment.Name) && (job.AllowAnySender || attachment.SenderMatches))
                            invoices.Add(InvoiceEngine.ReadInvoice(bytes,attachment.Name,attachment.SenderMatches));
                    } catch(Exception error) {failures++; Diagnostics.Write("preview attachment failed",error);}
                }
            } finally {JobProtocol.Clean(folder);}
            if(failures>0 && invoices.Count==0) throw new InvalidDataException("Gli allegati non contengono una fattura leggibile.");
            return invoices;
        }

    }
}
