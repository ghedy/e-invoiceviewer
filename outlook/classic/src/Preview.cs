using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace EInvoiceViewer.OutlookClassic {
    internal static class Preview {
        [STAThread] private static int Main(string[] args) {
            try {
                string input=null;
                if(args.Length>0) input=args[0];
                else using(OpenFileDialog picker=new OpenFileDialog {Filter="Fatture e messaggi|*.xml;*.p7m;*.eml",Title="Apri un documento di prova"}) {
                    if(picker.ShowDialog()!=DialogResult.OK) return 0;
                    input=picker.FileName;
                }
                if(new FileInfo(input).Length>InvoiceEngine.MaxMessage) throw new InvalidDataException("File troppo grande.");
                byte[] bytes=File.ReadAllBytes(input);
                IList<Invoice> invoices=input.EndsWith(".eml",StringComparison.OrdinalIgnoreCase) ?
                    InvoiceEngine.ReadEml(bytes,true) : new List<Invoice>{InvoiceEngine.ReadInvoice(bytes,Path.GetFileName(input),false)};
                if(invoices.Count==0) throw new InvalidDataException("Nessuna fattura trovata.");
                string xsl=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"shared","assets","assofattura.xsl");
                if(args.Length==3 && args[1]=="--render") {
                    File.WriteAllText(args[2],InvoiceEngine.Render(invoices[0],xsl));
                    return 0;
                }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new ViewerForm(invoices,xsl));
                return 0;
            } catch(Exception error) {
                if(args.Length==3 && args[1]=="--render") {Console.Error.WriteLine(error.Message);return 1;}
                MessageBox.Show(error.Message,"e-Invoice Viewer — prova locale");
                return 1;
            }
        }
    }
}
