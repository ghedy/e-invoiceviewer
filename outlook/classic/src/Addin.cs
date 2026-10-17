using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("e-Invoice Viewer for classic Outlook")]
[assembly: AssemblyVersion("0.1.2.0")]
[assembly: ComVisible(false)]

namespace EInvoiceViewer.OutlookClassic {
    [ComVisible(true), Guid("B65AD801-ABAF-11D0-BB8B-00A0C90F2744"), InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IDTExtensibility2 {
        [DispId(1)] void OnConnection([MarshalAs(UnmanagedType.IDispatch)] object application,int connectMode,[MarshalAs(UnmanagedType.IDispatch)] object addin,ref Array custom);
        [DispId(2)] void OnDisconnection(int removeMode,ref Array custom);
        [DispId(3)] void OnAddInsUpdate(ref Array custom);
        [DispId(4)] void OnStartupComplete(ref Array custom);
        [DispId(5)] void OnBeginShutdown(ref Array custom);
    }
    [ComVisible(true), Guid("000C0396-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IRibbonExtensibility {
        [DispId(1)] [return: MarshalAs(UnmanagedType.BStr)] string GetCustomUI([MarshalAs(UnmanagedType.BStr)] string ribbonId);
    }
    [ComVisible(true), Guid("8440B89B-1D14-4C0F-9EB8-6483F99A5932"),
     ProgId("Ghedy.EInvoiceViewer.Outlook"), ClassInterface(ClassInterfaceType.AutoDispatch)]
    public sealed class Addin : IDTExtensibility2, IRibbonExtensibility {
        private object application;
        private bool busy;
        private const string SettingsKey=@"Software\Ghedy\e-InvoiceViewer\Outlook";
        public void OnConnection(object app,int mode,object addin,ref Array custom) {application=app;}
        public void OnDisconnection(int mode,ref Array custom) {application=null;}
        public void OnAddInsUpdate(ref Array custom) {}
        public void OnStartupComplete(ref Array custom) {}
        public void OnBeginShutdown(ref Array custom) {application=null;}
        public string GetCustomUI(string ribbonId) {
            string tab;
            if(ribbonId=="Microsoft.Outlook.Explorer") tab="TabMail";
            else if(ribbonId=="Microsoft.Outlook.Mail.Read") tab="TabReadMessage";
            else return null;
            return "<customUI xmlns=\"http://schemas.microsoft.com/office/2009/07/customui\"><ribbon><tabs><tab idMso=\""+tab+
                "\"><group id=\"ghedyInvoiceGroup\" label=\"Fatture elettroniche\"><button id=\"ghedyOpenInvoice\" label=\"Visualizza fatture\" size=\"large\" imageMso=\"FileOpen\" onAction=\"OpenInvoice\"/>"+
                "<checkBox id=\"ghedyAnySender\" label=\"Consenti qualsiasi mittente\" getPressed=\"GetAnySender\" onAction=\"SetAnySender\"/></group></tab></tabs></ribbon></customUI>";
        }
        public bool GetAnySender(object control) {
            using(RegistryKey key=Registry.CurrentUser.OpenSubKey(SettingsKey))
                return key!=null && Convert.ToInt32(key.GetValue("AllowAnySender",0))==1;
        }
        public void SetAnySender(object control,bool pressed) {
            try {using(RegistryKey key=Registry.CurrentUser.CreateSubKey(SettingsKey)) key.SetValue("AllowAnySender",pressed?1:0,RegistryValueKind.DWord);}
            catch {MessageBox.Show("Impossibile salvare la preferenza.","e-Invoice Viewer");}
        }
        public void OpenInvoice(object control) {
            if(busy || application==null) return;
            busy=true;
            object context=null, item=null, selection=null;
            string temp=Path.Combine(Path.GetTempPath(),"e-invoiceviewer-outlook",Guid.NewGuid().ToString("N"));
            try {
                // Use the clicked ribbon's context, not a global last-message cache.
                context=((dynamic)control).Context;
                try {item=((dynamic)context).CurrentItem;}
                catch {
                    selection=((dynamic)context).Selection;
                    if((int)((dynamic)selection).Count==1) item=((dynamic)selection).Item(1);
                }
                if(item==null || (int)((dynamic)item).Class!=43) {
                    MessageBox.Show("Seleziona un solo messaggio di posta.","e-Invoice Viewer"); return;
                }
                Directory.CreateDirectory(temp);
                List<Invoice> invoices=new List<Invoice>();
                List<string> failures=new List<string>();
                int budget=200;
                Collect(item,temp,GetAnySender(null),0,ref budget,invoices,failures);
                if(invoices.Count==0) {
                    string message="Nessuna fattura leggibile trovata. Il filtro consente solo mittenti dichiarati nel dominio pec.fatturapa.it; puoi ampliarlo dalla barra Fatture elettroniche.";
                    if(failures.Count>0) message+="\n\nUno o più allegati non sono stati letti: "+String.Join("; ",failures.ToArray());
                    MessageBox.Show(message,"e-Invoice Viewer"); return;
                }
                string xsl=Path.Combine(Path.GetDirectoryName(typeof(Addin).Assembly.Location),"shared","assets","assofattura.xsl");
                ViewerForm viewer=new ViewerForm(invoices,xsl);
                viewer.Show();
                if(failures.Count>0) MessageBox.Show("Alcuni allegati non sono stati letti: "+String.Join("; ",failures.ToArray()),"e-Invoice Viewer");
            } catch {
                MessageBox.Show("Impossibile leggere il messaggio. Verifica che sia selezionata una sola email e che gli allegati siano disponibili localmente.","e-Invoice Viewer");
            } finally {
                Release(selection); Release(item); Release(context);
                try {if(Directory.Exists(temp)) {foreach(string path in Directory.GetFiles(temp)) File.Delete(path); Directory.Delete(temp);}}
                catch(IOException) {} catch(UnauthorizedAccessException) {}
                busy=false;
            }
        }
        private void Collect(object item,string temp,bool any,int depth,ref int budget,List<Invoice> invoices,List<string> failures) {
            if(depth>4 || budget<=0) return;
            string sender="";
            try {sender=(string)((dynamic)item).SenderEmailAddress;}
            catch {}
            try {
                if((string)((dynamic)item).SenderEmailType=="EX") {
                    object accessor=((dynamic)item).PropertyAccessor;
                    try {sender=(string)((dynamic)accessor).GetProperty("http://schemas.microsoft.com/mapi/proptag/0x5D01001F");}
                    finally {Release(accessor);}
                }
            } catch {}
            bool trusted=InvoiceEngine.IsSdiSender(sender);
            object attachments=((dynamic)item).Attachments;
            try {
                int count=(int)((dynamic)attachments).Count;
                for(int i=1;i<=count && budget>0;i++) {
                    budget--;
                    object attachment=null;
                    try {
                        attachment=((dynamic)attachments).Item(i);
                        string name=(string)((dynamic)attachment).FileName;
                        bool nested=String.Equals(name,"postacert.eml",StringComparison.OrdinalIgnoreCase);
                        if(!nested && !(InvoiceEngine.IsInvoiceName(name) && (any||trusted))) continue;
                        int max=nested ? InvoiceEngine.MaxMessage : InvoiceEngine.MaxFile;
                        if((int)((dynamic)attachment).Size>max) {failures.Add("file oltre il limite");continue;}
                        bool embedded=nested && (int)((dynamic)attachment).Type==5;
                        string path=Path.Combine(temp,Guid.NewGuid().ToString("N")+(embedded?".msg":".bin"));
                        ((dynamic)attachment).SaveAsFile(path);
                        if(new FileInfo(path).Length>max) throw new InvalidDataException();
                        if(embedded) {
                            object session=((dynamic)application).Session, inner=null;
                            try {
                                inner=((dynamic)session).OpenSharedItem(path);
                                if((int)((dynamic)inner).Class==43) Collect(inner,temp,any,depth+1,ref budget,invoices,failures);
                            } finally {Release(inner);Release(session);}
                        } else {
                            byte[] bytes=File.ReadAllBytes(path);
                            if(nested) invoices.AddRange(InvoiceEngine.ReadEml(bytes,any));
                            else invoices.Add(InvoiceEngine.ReadInvoice(bytes,name,trusted));
                        }
                    } catch {if(failures.Count<8) failures.Add("contenuto non supportato o malformato");}
                    finally {Release(attachment);}
                }
            } finally {Release(attachments);}
        }
        private static void Release(object value) {if(value!=null && Marshal.IsComObject(value)) Marshal.ReleaseComObject(value);}
    }
}
