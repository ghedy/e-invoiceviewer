using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace EInvoiceViewer.OutlookClassic {
    internal static class Setup {
        private const string Id="8440B89B-1D14-4C0F-9EB8-6483F99A5932";
        private const string ProgId="Ghedy.EInvoiceViewer.Outlook";
        private const string ClassName="EInvoiceViewer.OutlookClassic.Addin";
        [STAThread] private static void Main() {
            Application.EnableVisualStyles();
            Form form=new Form {Text="e-Invoice Viewer — configurazione prototipo",Width=570,Height=220};
            FlowLayoutPanel panel=new FlowLayoutPanel {Dock=DockStyle.Fill,Padding=new Padding(12)};
            panel.Controls.Add(new Label {Text="Solo Outlook classico per Windows. Chiudi Outlook prima di installare o rimuovere.\nQuesta cartella deve restare nella posizione attuale dopo l’installazione.\nPacchetto: "+(Environment.Is64BitProcess?"64":"32")+" bit.",AutoSize=true,MaximumSize=new System.Drawing.Size(520,0)});
            Button install=new Button {Text="Installa per questo utente",AutoSize=true};
            Button remove=new Button {Text="Rimuovi integrazione",AutoSize=true};
            panel.Controls.Add(install); panel.Controls.Add(remove); form.Controls.Add(panel);
            install.Click+=delegate {Change(true);};
            remove.Click+=delegate {Change(false);};
            Application.Run(form);
        }
        private static void Change(bool install) {
            try {
                if(Process.GetProcessesByName("OUTLOOK").Length>0) throw new InvalidOperationException("Chiudi Outlook e riprova.");
                string dll=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"EInvoiceViewer.Outlook.dll");
                if(!File.Exists(dll)) throw new FileNotFoundException("DLL del prototipo non trovata.");
                string codebase=new Uri(Path.GetFullPath(dll)).AbsoluteUri;
                RegistryView view=Environment.Is64BitProcess?RegistryView.Registry64:RegistryView.Registry32;
                using(RegistryKey root=RegistryKey.OpenBaseKey(RegistryHive.CurrentUser,view)) {
                    string cls=@"Software\Classes\CLSID\{"+Id+"}";
                    string prog=@"Software\Classes\"+ProgId;
                    string addin=@"Software\Microsoft\Office\Outlook\Addins\"+ProgId;
                    using(RegistryKey existing=root.OpenSubKey(cls+@"\InprocServer32")) {
                        if(existing!=null && !String.Equals(Convert.ToString(existing.GetValue("CodeBase")),codebase,StringComparison.OrdinalIgnoreCase))
                            throw new InvalidOperationException("Esiste una registrazione in un’altra cartella. Rimuovila da quella copia prima di procedere.");
                    }
                    if(install) {
                        using(RegistryKey key=root.CreateSubKey(cls)) key.SetValue("","e-Invoice Viewer");
                        using(RegistryKey key=root.CreateSubKey(cls+@"\ProgId")) key.SetValue("",ProgId);
                        using(RegistryKey key=root.CreateSubKey(prog+@"\CLSID")) key.SetValue("","{"+Id+"}");
                        using(RegistryKey key=root.CreateSubKey(cls+@"\InprocServer32")) {
                            key.SetValue("","mscoree.dll"); key.SetValue("ThreadingModel","Both");
                            key.SetValue("Class",ClassName);
                            key.SetValue("Assembly",AssemblyName.GetAssemblyName(dll).FullName);
                            key.SetValue("RuntimeVersion","v4.0.30319"); key.SetValue("CodeBase",codebase);
                        }
                        using(RegistryKey key=root.CreateSubKey(addin)) {
                            key.SetValue("FriendlyName","e-Invoice Viewer");
                            key.SetValue("Description","Visualizza e stampa fatture elettroniche italiane — prototipo");
                            key.SetValue("LoadBehavior",3,RegistryValueKind.DWord);
                        }
                    } else {
                        // Only the exact per-user keys owned by this prototype are removed.
                        root.DeleteSubKeyTree(addin,false);
                        root.DeleteSubKeyTree(prog,false);
                        root.DeleteSubKeyTree(cls,false);
                    }
                }
                MessageBox.Show(install?"Registrazione completata. Avvia Outlook classico.":"Integrazione rimossa. I file del pacchetto e la preferenza sui mittenti sono conservati.","e-Invoice Viewer");
            } catch(Exception error) {MessageBox.Show(error.Message,"Configurazione non completata");}
        }
    }
}
