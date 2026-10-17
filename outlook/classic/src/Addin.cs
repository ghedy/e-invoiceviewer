using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("e-Invoice Viewer for classic Outlook")]
[assembly: AssemblyVersion("0.1.6.0")]
[assembly: ComVisible(false)]

namespace EInvoiceViewer.OutlookClassic {
    [ComVisible(true)] public enum ConnectMode {AfterStartup=0,Startup=1,External=2,CommandLine=3,Solution=4,UISetup=5}
    [ComVisible(true)] public enum DisconnectMode {HostShutdown=0,UserClosed=1,UISetupComplete=2,SolutionClosed=3}
    [ComVisible(true), Guid("B65AD801-ABAF-11D0-BB8B-00A0C90F2744"), InterfaceType(ComInterfaceType.InterfaceIsDual), TypeLibType(4160)]
    public interface IDTExtensibility2 {
        [DispId(1)] void OnConnection([In,MarshalAs(UnmanagedType.IDispatch)] object application,[In] ConnectMode connectMode,
            [In,MarshalAs(UnmanagedType.IDispatch)] object addin,[In,MarshalAs(UnmanagedType.SafeArray,SafeArraySubType=VarEnum.VT_VARIANT)] ref Array custom);
        [DispId(2)] void OnDisconnection([In] DisconnectMode removeMode,[In,MarshalAs(UnmanagedType.SafeArray,SafeArraySubType=VarEnum.VT_VARIANT)] ref Array custom);
        [DispId(3)] void OnAddInsUpdate([In,MarshalAs(UnmanagedType.SafeArray,SafeArraySubType=VarEnum.VT_VARIANT)] ref Array custom);
        [DispId(4)] void OnStartupComplete([In,MarshalAs(UnmanagedType.SafeArray,SafeArraySubType=VarEnum.VT_VARIANT)] ref Array custom);
        [DispId(5)] void OnBeginShutdown([In,MarshalAs(UnmanagedType.SafeArray,SafeArraySubType=VarEnum.VT_VARIANT)] ref Array custom);
    }
    [ComVisible(true), Guid("A2C61CEC-C980-4D4B-BF02-430FE42C373E"), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IRibbonCallbacks {
        [DispId(104)] [return:MarshalAs(UnmanagedType.IDispatch)] object LoadRibbonImage([In,MarshalAs(UnmanagedType.BStr)] string imageId);
        [DispId(101)] void OpenInvoice([In,MarshalAs(UnmanagedType.IDispatch)] object control);
        [DispId(102)] [return:MarshalAs(UnmanagedType.VariantBool)] bool GetAnySender([In,MarshalAs(UnmanagedType.IDispatch)] object control);
        [DispId(103)] void SetAnySender([In,MarshalAs(UnmanagedType.IDispatch)] object control,[In,MarshalAs(UnmanagedType.VariantBool)] bool pressed);
    }
    [ComVisible(true), Guid("000C0396-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IRibbonExtensibility {
        [DispId(1)] [return: MarshalAs(UnmanagedType.BStr)] string GetCustomUI([MarshalAs(UnmanagedType.BStr)] string ribbonId);
    }
    [ComVisible(true), Guid("8440B89B-1D14-4C0F-9EB8-6483F99A5932"),
     ProgId("Ghedy.EInvoiceViewer.Outlook"), ClassInterface(ClassInterfaceType.None), ComDefaultInterface(typeof(IRibbonCallbacks))]
    public sealed class Addin : IDTExtensibility2, IRibbonExtensibility, IRibbonCallbacks {
        private sealed class RibbonPicture : AxHost {
            private RibbonPicture() : base(Guid.Empty.ToString()) {}
            public static object FromImage(Image image) {return GetIPictureDispFromPicture(image);}
        }
        private object ribbonPicture;
        private object application;
        private bool busy;
        private const string SettingsKey=@"Software\Ghedy\e-InvoiceViewer\Outlook";
        public Addin() {Diagnostics.Write("construct");}
        public void OnConnection(object app,ConnectMode mode,object addin,ref Array custom) {
            Diagnostics.Write("OnConnection enter");
            try {application=app;} catch(Exception error) {Diagnostics.Write("OnConnection failed",error);}
            Diagnostics.Write("OnConnection exit");
        }
        public void OnDisconnection(DisconnectMode mode,ref Array custom) {Diagnostics.Write("OnDisconnection");application=null;}
        public void OnAddInsUpdate(ref Array custom) {Diagnostics.Write("OnAddInsUpdate");}
        public void OnStartupComplete(ref Array custom) {Diagnostics.Write("OnStartupComplete");}
        public void OnBeginShutdown(ref Array custom) {Diagnostics.Write("OnBeginShutdown");application=null;}
        public string GetCustomUI(string ribbonId) {
            Diagnostics.Write("GetCustomUI");
            string tab;
            if(ribbonId=="Microsoft.Outlook.Explorer") tab="TabMail";
            else if(ribbonId=="Microsoft.Outlook.Mail.Read") tab="TabReadMessage";
            else return null;
            return "<customUI xmlns=\"http://schemas.microsoft.com/office/2009/07/customui\" loadImage=\"LoadRibbonImage\"><ribbon><tabs><tab idMso=\""+tab+
                "\"><group id=\"ghedyInvoiceGroup\" label=\"Fatture elettroniche\"><button id=\"ghedyOpenInvoice\" label=\"Visualizza fatture\" size=\"large\" image=\"InvoiceSearch\" onAction=\"OpenInvoice\"/>"+
                "<checkBox id=\"ghedyAnySender\" label=\"Consenti qualsiasi mittente\" getPressed=\"GetAnySender\" onAction=\"SetAnySender\"/></group></tab></tabs></ribbon></customUI>";
        }
        public object LoadRibbonImage(string imageId) {
            if(imageId!="InvoiceSearch") return null;
            try {
                if(ribbonPicture==null) {
                    using(Stream source=typeof(Addin).Assembly.GetManifestResourceStream("EInvoiceViewer.InvoiceSearch.png")) {
                        if(source==null) throw new InvalidDataException("Icona non disponibile.");
                        using(Bitmap image=new Bitmap(source)) ribbonPicture=RibbonPicture.FromImage(image);
                    }
                }
                return ribbonPicture;
            } catch(Exception error) {Diagnostics.Write("ribbon image failed",error);return null;}
        }
        public bool GetAnySender(object control) {
            try {
                using(RegistryKey key=Registry.CurrentUser.OpenSubKey(SettingsKey))
                    return key!=null && Convert.ToInt32(key.GetValue("AllowAnySender",0))==1;
            } catch(Exception error) {Diagnostics.Write("GetAnySender failed",error);return false;}
        }
        public void SetAnySender(object control,bool pressed) {
            try {using(RegistryKey key=Registry.CurrentUser.CreateSubKey(SettingsKey)) key.SetValue("AllowAnySender",pressed?1:0,RegistryValueKind.DWord);}
            catch {MessageBox.Show("Impossibile salvare la preferenza.","e-Invoice Viewer");}
        }
        public void OpenInvoice(object control) {
            if(busy || application==null) return;
            busy=true;
            object context=null, item=null, selection=null;
            string temp=null;
            bool launched=false;
            Diagnostics.Write("OpenInvoice enter");
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
                temp=JobProtocol.CreateFolder();
                InvoiceJob job=new InvoiceJob {AllowAnySender=GetAnySender(null)};
                List<string> failures=new List<string>();
                int budget=200;
                long bytesRemaining=64L*1024*1024;
                Collect(item,temp,job.AllowAnySender,false,0,ref budget,ref bytesRemaining,job.Attachments,failures);
                if(job.Attachments.Count==0) {
                    string message="Nessuna fattura leggibile trovata. Il filtro consente solo mittenti dichiarati nel dominio pec.fatturapa.it; puoi ampliarlo dalla barra Fatture elettroniche.";
                    if(failures.Count>0) message+="\n\nUno o più allegati non sono stati letti: "+String.Join("; ",failures.ToArray());
                    MessageBox.Show(message,"e-Invoice Viewer"); return;
                }
                string directory=Path.GetDirectoryName(typeof(Addin).Assembly.Location);
                string exe=Path.Combine(directory,"OutlookPreview.exe");
                if(!File.Exists(exe)) throw new FileNotFoundException("Visualizzatore non disponibile.");
                string manifest=JobProtocol.Write(temp,job);
                Process process=Process.Start(new ProcessStartInfo(exe,"--job \""+manifest+"\"") {UseShellExecute=false,WorkingDirectory=directory});
                if(process==null) throw new InvalidOperationException("Avvio non riuscito.");
                process.Dispose();launched=true;
                Diagnostics.Write("viewer launched");
                if(failures.Count>0) MessageBox.Show("Alcuni allegati non sono stati letti: "+String.Join("; ",failures.ToArray()),"e-Invoice Viewer");
            } catch(Exception error) {
                Diagnostics.Write("OpenInvoice failed",error);
                MessageBox.Show("Impossibile leggere il messaggio. Verifica che sia selezionata una sola email e che gli allegati siano disponibili localmente.","e-Invoice Viewer");
            } finally {
                Release(selection); Release(item); Release(context);
                if(!launched && temp!=null) JobProtocol.Clean(temp);
                busy=false;Diagnostics.Write("OpenInvoice exit");
            }
        }
        private void Collect(object item,string temp,bool any,bool containerMatches,int depth,ref int budget,ref long bytesRemaining,List<InvoiceJobAttachment> attachmentsToOpen,List<string> failures) {
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
            bool trusted=MailPolicy.IsSdiSender(sender);
            object attachments=((dynamic)item).Attachments;
            try {
                int count=(int)((dynamic)attachments).Count;
                for(int i=1;i<=count && budget>0;i++) {
                    budget--;
                    object attachment=null;
                    try {
                        attachment=((dynamic)attachments).Item(i);
                        string name=(string)((dynamic)attachment).FileName;
                        bool embedded=(int)((dynamic)attachment).Type==5;
                        bool nested=embedded || (name!=null && name.EndsWith(".eml",StringComparison.OrdinalIgnoreCase));
                        if(!nested && !(MailPolicy.IsInvoiceName(name) && (any||trusted||containerMatches))) continue;
                        int max=nested ? MailPolicy.MaxMessage : MailPolicy.MaxFile;
                        if((int)((dynamic)attachment).Size>max) {failures.Add("file oltre il limite");continue;}
                        string path=Path.Combine(temp,Guid.NewGuid().ToString("N")+(embedded?".msg":".bin"));
                        ((dynamic)attachment).SaveAsFile(path);
                        long length=new FileInfo(path).Length;
                        if(length>max || length>bytesRemaining) throw new InvalidDataException();
                        bytesRemaining-=length;
                        if(embedded) {
                            object session=((dynamic)application).Session, inner=null;
                            try {
                                inner=((dynamic)session).OpenSharedItem(path);
                                if((int)((dynamic)inner).Class==43) Collect(inner,temp,any,containerMatches||trusted,depth+1,ref budget,ref bytesRemaining,attachmentsToOpen,failures);
                            } finally {Release(inner);Release(session);}
                        } else {
                            attachmentsToOpen.Add(new InvoiceJobAttachment {
                                File=Path.GetFileName(path),Name=name,Eml=nested,SenderMatches=trusted,ContainerSenderMatches=containerMatches
                            });
                        }
                    } catch {if(failures.Count<8) failures.Add("contenuto non supportato o malformato");}
                    finally {Release(attachment);}
                }
            } finally {Release(attachments);}
        }
        private static void Release(object value) {
            try {if(value!=null && Marshal.IsComObject(value)) Marshal.ReleaseComObject(value);}
            catch(Exception error) {Diagnostics.Write("COM release failed",error);}
        }
    }
}
