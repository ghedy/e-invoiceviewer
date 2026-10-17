using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace EInvoiceViewer.OutlookClassic {
    public static class MailPolicy {
        public const int MaxFile=20*1024*1024, MaxMessage=32*1024*1024;
        public static bool IsSdiSender(string from) {
            if(String.IsNullOrWhiteSpace(from) || from.Contains("\r") || from.Contains("\n")) return false;
            Match mailbox=Regex.Match(from.Trim(),@"^(?:""[^""<>]*""|[^""<>,;]*)<(?<mail>[^<>]+)>$");
            string address=(mailbox.Success?mailbox.Groups["mail"].Value:from).Trim();
            return Regex.IsMatch(address,@"^[a-z0-9.!#$%&'*+/=?^_{|}~-]+@pec\.fatturapa\.it$",RegexOptions.IgnoreCase);
        }
        public static bool IsInvoiceName(string name) {
            return name!=null && name.IndexOf("_MT_",StringComparison.OrdinalIgnoreCase)<0 &&
                name.IndexOfAny(new char[]{'/','\\','\0','\r','\n'})<0 &&
                Regex.IsMatch(name,@"\.(xml|p7m)$",RegexOptions.IgnoreCase);
        }
    }
    public static class Diagnostics {
        public static string LogPath { get {return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"e-InvoiceViewer","Outlook","Diagnostics","addin.log");} }
        public static void Write(string stage) { Write(stage,null); }
        public static void Write(string stage,Exception error) {
            try {
                string path=LogPath;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                if(File.Exists(path) && new FileInfo(path).Length>262144) File.WriteAllText(path,"");
                string line=DateTime.UtcNow.ToString("o")+" 0.1.7 "+Process.GetCurrentProcess().Id+" "+stage;
                if(error!=null) line+=" "+error.GetType().Name+" 0x"+error.HResult.ToString("X8");
                File.AppendAllText(path,line+Environment.NewLine,Encoding.UTF8);
            } catch { /* Diagnostics must never interrupt Outlook. */ }
        }
    }
    public sealed class InvoiceJobAttachment {
        public string File {get;set;}
        public string Name {get;set;}
        public bool Eml {get;set;}
        public bool SenderMatches {get;set;}
        public bool ContainerSenderMatches {get;set;}
    }
    public sealed class InvoiceJob {
        public int ProtocolVersion {get;set;}
        public bool AllowAnySender {get;set;}
        public List<InvoiceJobAttachment> Attachments {get;set;}
        public InvoiceJob() {ProtocolVersion=1; Attachments=new List<InvoiceJobAttachment>();}
    }
    public static class JobProtocol {
        public static string Root {get {return Path.GetFullPath(Path.Combine(Path.GetTempPath(),"e-invoiceviewer-outlook"));}}
        public static string CreateFolder() {
            string path=Path.Combine(Root,Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);return path;
        }
        private static void CheckFolder(string path) {
            string full=Path.GetFullPath(path);
            string parent=Path.GetDirectoryName(full.TrimEnd(Path.DirectorySeparatorChar));
            Guid id;
            if(!String.Equals(parent,Root,StringComparison.OrdinalIgnoreCase) ||
                !Guid.TryParseExact(Path.GetFileName(full),"N",out id))
                throw new InvalidDataException("Cartella di scambio non valida.");
            if((File.GetAttributes(full)&FileAttributes.ReparsePoint)!=0)
                throw new InvalidDataException("Cartella di scambio non consentita.");
        }
        public static string Write(string folder,InvoiceJob job) {
            CheckFolder(folder);
            string path=Path.Combine(folder,"job.json");
            JavaScriptSerializer json=new JavaScriptSerializer {MaxJsonLength=131072};
            File.WriteAllText(path,json.Serialize(job),Encoding.UTF8);
            return path;
        }
        public static InvoiceJob Read(string path) {
            path=Path.GetFullPath(path);
            CheckFolder(Path.GetDirectoryName(path));
            if(Path.GetFileName(path)!="job.json" || new FileInfo(path).Length>131072)
                throw new InvalidDataException("Richiesta di apertura non valida.");
            JavaScriptSerializer json=new JavaScriptSerializer {MaxJsonLength=131072};
            InvoiceJob job=json.Deserialize<InvoiceJob>(File.ReadAllText(path,Encoding.UTF8));
            if(job==null || job.ProtocolVersion!=1 || job.Attachments==null || job.Attachments.Count>200)
                throw new InvalidDataException("Protocollo di apertura non supportato.");
            long total=0;
            foreach(InvoiceJobAttachment attachment in job.Attachments) {
                if(attachment==null || attachment.File==null ||
                    !Regex.IsMatch(attachment.File,@"^[0-9a-f]{32}\.bin$") ||
                    String.IsNullOrEmpty(attachment.Name) || attachment.Name.Length>1024)
                    throw new InvalidDataException("Allegato di scambio non valido.");
                string file=Path.Combine(Path.GetDirectoryName(path),attachment.File);
                if((File.GetAttributes(file)&FileAttributes.ReparsePoint)!=0)
                    throw new InvalidDataException("Allegato di scambio non consentito.");
                long length=new FileInfo(file).Length;
                total+=length;
                if(length>(attachment.Eml?MailPolicy.MaxMessage:MailPolicy.MaxFile) || total>64L*1024*1024)
                    throw new InvalidDataException("Dimensioni degli allegati eccessive.");
            }
            return job;
        }
        public static void Clean(string folder) {
            try {
                CheckFolder(folder);
                foreach(string file in Directory.GetFiles(folder)) {
                    string name=Path.GetFileName(file);
                    if(name=="job.json" || Regex.IsMatch(name,@"^[0-9a-f]{32}\.(bin|msg)$")) File.Delete(file);
                }
                Directory.Delete(folder,false);
            } catch { /* Files still locked are kept; no recursive deletion. */ }
        }
    }
}

