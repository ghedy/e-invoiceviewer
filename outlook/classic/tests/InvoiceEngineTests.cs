using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using EInvoiceViewer.OutlookClassic;

internal static class InvoiceEngineTests {
    private static int passed;
    private static string xml;
    private static byte[] Bytes(string value) {return Encoding.UTF8.GetBytes(value);}
    private static void Check(bool value,string message) {if(!value) throw new Exception(message);}
    private static void Reject(Action action) {
        bool rejected=false;
        try {action();} catch {rejected=true;}
        Check(rejected,"Expected rejection");
    }
    private static void Test(string name,Action action) {
        action(); passed++; Console.WriteLine("OK: "+name);
    }
    private static string Eml(string sender,string filename,string payload,string encoding) {
        return "From: "+sender+"\r\nReceived: first\r\nReceived: second\r\nMIME-Version: 1.0\r\nContent-Type: multipart/mixed;\r\n boundary=\"outer_test\"\r\n\r\n"+
            "--outer_test\r\nContent-Type: application/octet-stream\r\nContent-Disposition: attachment; filename=\""+filename+"\"\r\n"+
            "Content-Transfer-Encoding: "+encoding+"\r\n\r\n"+payload+"\r\n--outer_test--\r\n";
    }
    private static byte[] Ber(int tag,params byte[][] parts) {
        byte[] body=parts.SelectMany(p=>p).ToArray();
        List<byte> result=new List<byte>{(byte)tag};
        if(body.Length<128) result.Add((byte)body.Length);
        else if(body.Length<256) result.AddRange(new byte[]{129,(byte)body.Length});
        else result.AddRange(new byte[]{130,(byte)(body.Length>>8),(byte)body.Length});
        result.AddRange(body);return result.ToArray();
    }
    private static byte[] Cms(byte[] content) {
        byte[] oidSigned=Ber(6,new byte[]{42,134,72,134,247,13,1,7,2});
        byte[] oidData=Ber(6,new byte[]{42,134,72,134,247,13,1,7,1});
        return Ber(48,oidSigned,Ber(160,Ber(48,Ber(2,new byte[]{1}),Ber(49),
            Ber(48,oidData,Ber(160,Ber(4,content))),Ber(49))));
    }
    private static int Main(string[] args) {
        try {
            string root=args[0];
            xml=File.ReadAllText(Path.Combine(root,"tests/fixtures/IT00000000000_TEST01.xml"));
            Test("exact sender mailbox, no display-name or suffix bypass",delegate {
                Check(InvoiceEngine.IsSdiSender("SDI <sdi01@pec.fatturapa.it>"),"valid");
                foreach(string from in new[]{"\"sdi01@pec.fatturapa.it\" <evil@example.org>","a@pec.fatturapa.it.evil.org","a@evil.org, b@pec.fatturapa.it","a@pec.fatturapa.it\r\nBcc:x"})
                    Check(!InvoiceEngine.IsSdiSender(from),from);
            });
            Test("XML native rendering preserves invoice content and blocks scripts",delegate {
                Invoice invoice=InvoiceEngine.ReadInvoice(Bytes(xml),"test.xml",true);
                string html=InvoiceEngine.Render(invoice,Path.Combine(root,"shared/assets/assofattura.xsl"));
                Check(html.Contains("FORNITORE FITTIZIO") && html.Contains("TEST-001"),"content");
                Check(html.Contains("script-src 'none'") && html.Contains("print-color-adjust:exact"),"policy");
            });
            Test("DTD, wrong namespaces, malformed XML and oversized files",delegate {
                Reject(()=>InvoiceEngine.ReadInvoice(Bytes("<!DOCTYPE a [<!ENTITY x 'test'>]>"+xml),"bad.xml",false));
                Reject(()=>InvoiceEngine.ReadInvoice(Bytes(xml.Replace("http://ivaservizi.agenziaentrate.gov.it/docs/xsd/fatture/v1.2","urn:fake")),"bad.xml",false));
                Reject(()=>InvoiceEngine.ReadInvoice(Bytes(xml+"<extra/>"),"bad.xml",false));
                Reject(()=>InvoiceEngine.ReadInvoice(new byte[InvoiceEngine.MaxFile+1],"big.xml",false));
            });
            Test("P7M CMS content extraction and malformed envelopes",delegate {
                byte[] valid=Cms(Bytes(xml));
                Check(InvoiceEngine.ReadInvoice(valid,"invoice.p7m",false).Document.DocumentElement.LocalName=="FatturaElettronica","cms");
                Reject(()=>InvoiceEngine.ReadInvoice(Bytes(xml),"fake.p7m",false));
                Reject(()=>InvoiceEngine.ReadInvoice(valid.Take(valid.Length-8).ToArray(),"truncated.p7m",false));
            });
            Test("synthetic email, folded MIME headers and repeated Received headers",delegate {
                byte[] message=File.ReadAllBytes(Path.Combine(root,"tests/fixtures/synthetic-invoice.eml"));
                Check(InvoiceEngine.ReadEml(message,false).Count==1,"fixture");
            });
            Test("nested PEC checks inner sender independently",delegate {
                string inner=Eml("sdi01@pec.fatturapa.it","invoice.xml",Convert.ToBase64String(Bytes(xml)),"base64");
                string wrapper=Eml("provider@example.org","postacert.eml",Convert.ToBase64String(Bytes(inner)),"base64");
                Check(InvoiceEngine.ReadEml(Bytes(wrapper),false).Count==1,"inner trusted");
                string evil=inner.Replace("sdi01@pec.fatturapa.it","evil@example.org");
                wrapper=Eml("sdi01@pec.fatturapa.it","postacert.eml",Convert.ToBase64String(Bytes(evil)),"base64");
                Check(InvoiceEngine.ReadEml(Bytes(wrapper),false).Count==0,"no inherited trust");
                Check(InvoiceEngine.ReadEml(Bytes(wrapper),true).Count==1,"explicit option");
            });
            Test("quoted-printable preserves accented invoice text",delegate {
                string value=String.Concat(Bytes(xml).Select(b=>"="+b.ToString("X2")).ToArray());
                IList<Invoice> data=InvoiceEngine.ReadEml(Bytes(Eml("sdi01@pec.fatturapa.it","invoice.xml",value,"quoted-printable")),false);
                Check(data.Count==1 && data[0].Document.InnerText.Contains("caffè"),"accent");
            });
            Test("extended encoded filenames are recognized",delegate {
                string message=Eml("sdi01@pec.fatturapa.it","invoice.xml",Convert.ToBase64String(Bytes(xml)),"base64");
                message=message.Replace("filename=\"invoice.xml\"","filename*=UTF-8''fattura%20prova.xml");
                Check(InvoiceEngine.ReadEml(Bytes(message),false)[0].Name=="fattura prova.xml","filename");
            });
            Test("truncated MIME, duplicate sender, deep nesting and malformed Base64 are refused",delegate {
                string message=Eml("sdi01@pec.fatturapa.it","invoice.xml",Convert.ToBase64String(Bytes(xml)),"base64");
                Reject(()=>InvoiceEngine.ReadEml(Bytes(message.Replace("--outer_test--","")),false));
                Reject(()=>InvoiceEngine.ReadEml(Bytes("From: evil@example.org\r\n"+message),false));
                Reject(()=>InvoiceEngine.ReadEml(Bytes(Eml("sdi01@pec.fatturapa.it","invoice.xml","!!!!","base64")),false));
                for(int i=0;i<9;i++) message=Eml("sdi01@pec.fatturapa.it","postacert.eml",Convert.ToBase64String(Bytes(message)),"base64");
                Reject(()=>InvoiceEngine.ReadEml(Bytes(message),false));
            });
            Test("PDF recognition does not trust filename alone",delegate {
                string pdf=Convert.ToBase64String(Bytes("%PDF-1.7\n%%EOF\n"));
                string embedded="<Allegati><NomeAttachment>demo.pdf</NomeAttachment><FormatoAttachment>PDF</FormatoAttachment><Attachment>"+pdf+"</Attachment></Allegati>";
                string value=xml.Replace("</FatturaElettronicaBody>",embedded+"</FatturaElettronicaBody>");
                Invoice invoice=InvoiceEngine.ReadInvoice(Bytes(value),"a.xml",false);
                Check(InvoiceEngine.Embedded(invoice)[0].Pdf,"pdf");
                value=value.Replace(pdf,Convert.ToBase64String(Bytes("<html>active</html>")));
                Check(!InvoiceEngine.Embedded(InvoiceEngine.ReadInvoice(Bytes(value),"a.xml",false))[0].Pdf,"fake pdf");
            });
            Assembly addinAssembly=Assembly.LoadFrom(Path.Combine(root,"dist/outlook-classic-0.1.0/x64/EInvoiceViewer.Outlook.dll"));
            object addin=Activator.CreateInstance(addinAssembly.GetType("EInvoiceViewer.OutlookClassic.Addin",true));
            Test("COM entry supports the Office add-in and ribbon interfaces",delegate {
                IntPtr unknown=Marshal.GetIUnknownForObject(addin);
                try {
                    foreach(string text in new[]{"B65AD801-ABAF-11D0-BB8B-00A0C90F2744","000C0396-0000-0000-C000-000000000046"}) {
                        Guid id=new Guid(text); IntPtr pointer;
                        int result=Marshal.QueryInterface(unknown,ref id,out pointer);
                        Check(result==0 && pointer!=IntPtr.Zero,"QueryInterface "+text);
                        Marshal.Release(pointer);
                    }
                } finally {Marshal.Release(unknown);}
            });
            Test("ribbon is exposed only on Explorer and read-message windows",delegate {
                MethodInfo method=addin.GetType().GetMethod("GetCustomUI");
                foreach(string context in new[]{"Microsoft.Outlook.Explorer","Microsoft.Outlook.Mail.Read"}) {
                    string markup=(string)method.Invoke(addin,new object[]{context});
                    XmlDocument ribbon=new XmlDocument(); ribbon.LoadXml(markup);
                    Check(ribbon.GetElementsByTagName("button","http://schemas.microsoft.com/office/2009/07/customui").Count==1,"ribbon button");
                    Check(markup.Contains("OpenInvoice") && markup.Contains("Consenti qualsiasi mittente"),"callbacks");
                }
                Check(method.Invoke(addin,new object[]{"Microsoft.Outlook.Mail.Compose"})==null,"compose should be excluded");
            });
            Console.WriteLine(passed+" native tests passed.");
            return 0;
        } catch(Exception error) {Console.Error.WriteLine(error);return 1;}
    }
}
