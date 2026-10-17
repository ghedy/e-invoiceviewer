using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EInvoiceViewer.OutlookClassic;

public sealed class TestAttachment {
    public string FileName {get;set;}
    public int Type {get;set;}
    public int Size {get {return 4;}}
    public void SaveAsFile(string path) {File.WriteAllText(path,"test");}
}
public sealed class TestAttachments {
    public List<TestAttachment> Values=new List<TestAttachment>();
    public int Count {get {return Values.Count;}}
    public TestAttachment Item(int index) {return Values[index-1];}
}
public sealed class TestMail {
    public int Class {get {return 43;}}
    public string SenderEmailAddress {get;set;}
    public string SenderEmailType {get {return "SMTP";}}
    public TestAttachments Attachments {get;set;}
}
public sealed class TestSession {
    public TestMail Inner;
    public TestMail OpenSharedItem(string path) {return Inner;}
}
public sealed class TestApplication {public TestSession Session {get;set;}}
internal static class AttachmentCollectionTests {
    private static void Check(bool value,string label) {if(!value) throw new Exception(label);}
    private static TestMail Mail(string sender,string name,int type) {
        TestMail mail=new TestMail {SenderEmailAddress=sender,Attachments=new TestAttachments()};
        mail.Attachments.Values.Add(new TestAttachment {FileName=name,Type=type});return mail;
    }
    private static List<InvoiceJobAttachment> Collect(TestMail mail,TestMail inner) {
        Addin addin=new Addin();Array custom=null;
        addin.OnConnection(new TestApplication {Session=new TestSession {Inner=inner}},ConnectMode.Startup,null,ref custom);
        string folder=JobProtocol.CreateFolder();
        try {
            List<InvoiceJobAttachment> attachments=new List<InvoiceJobAttachment>();
            List<string> failures=new List<string>();
            object[] args={mail,folder,false,false,0,200,64L*1024*1024,attachments,failures};
            typeof(Addin).GetMethod("Collect",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(addin,args);
            Check(failures.Count==0,"attachment collection failure");
            // Validate the very same exchange files consumed by the preview.
            JobProtocol.Read(JobProtocol.Write(folder,new InvoiceJob {Attachments=attachments}));
            return attachments;
        } finally {JobProtocol.Clean(folder);}
    }
    private static int Main() {
        try {
            List<InvoiceJobAttachment> eml=Collect(Mail("sdi01@pec.fatturapa.it","messaggio-originale.eml",1),null);
            Check(eml.Count==1 && eml[0].Eml && eml[0].SenderMatches,"generic EML from PEC");
            TestMail inner=Mail("vendor@example.org","invoice.xml",1);
            List<InvoiceJobAttachment> embedded=Collect(Mail("sdi01@pec.fatturapa.it","originale.msg",5),inner);
            Check(embedded.Count==1 && !embedded[0].Eml && !embedded[0].SenderMatches && embedded[0].ContainerSenderMatches,"embedded Outlook message keeps outer provenance");
            Check(Collect(Mail("other@example.org","originale.msg",5),inner).Count==0,"unrelated outer and inner blocked");
            Check(Collect(inner,null).Count==0,"opening standalone inner does not invent PEC provenance");
            Console.WriteLine("Outlook attachment collection passed: "+(IntPtr.Size==8?"x64":"x86"));
            return 0;
        } catch(Exception error) {Console.Error.WriteLine(error);return 1;}
    }
}
