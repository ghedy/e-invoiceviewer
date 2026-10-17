using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography.Pkcs;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Xsl;

namespace EInvoiceViewer.OutlookClassic {
    public sealed class Invoice {
        public string Name;
        public XmlDocument Document;
        public bool SenderMatches;
        public bool ContainerSenderMatches;
        public override string ToString() { return Name; }
    }
    public sealed class EmbeddedFile {
        public string Name;
        public byte[] Bytes;
        public bool Pdf;
        public override string ToString() { return Name; }
    }
    public static class InvoiceEngine {
        public const int MaxFile = 20 * 1024 * 1024;
        public const int MaxMessage = 32 * 1024 * 1024;
        public const int MaxEmbedded = 10 * 1024 * 1024;
        private static readonly Encoding Latin1 = Encoding.GetEncoding(28591);
        private static readonly HashSet<string> Namespaces = new HashSet<string> {
            "http://www.fatturapa.gov.it/sdi/fatturapa/v1.0",
            "http://www.fatturapa.gov.it/sdi/fatturapa/v1.1",
            "http://ivaservizi.agenziaentrate.gov.it/docs/xsd/fatture/v1.2",
            "http://ivaservizi.agenziaentrate.gov.it/docs/xsd/fatture/v1.0"
        };
        public static bool IsSdiSender(string from) {return MailPolicy.IsSdiSender(from);}
        public static bool IsInvoiceName(string name) {return MailPolicy.IsInvoiceName(name);}
        public static Invoice ReadInvoice(byte[] bytes, string name, bool senderMatches) {
            if (bytes.Length > MaxFile) throw new InvalidDataException("Fattura oltre 20 MiB.");
            if (name.EndsWith(".p7m", StringComparison.OrdinalIgnoreCase)) {
                // Decode the CMS envelope only. Deliberately do not call CheckSignature.
                SignedCms cms = new SignedCms();
                cms.Decode(bytes);
                bytes = cms.ContentInfo.Content;
                if (bytes.Length > MaxFile) throw new InvalidDataException("Contenuto P7M oltre 20 MiB.");
            }
            XmlReaderSettings settings = new XmlReaderSettings {
                DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null,
                MaxCharactersInDocument = MaxFile, MaxCharactersFromEntities = 1024
            };
            XmlDocument doc = new XmlDocument { XmlResolver = null, PreserveWhitespace = true };
            using (MemoryStream stream = new MemoryStream(bytes))
            using (XmlReader reader = XmlReader.Create(stream, settings)) { doc.Load(reader); }
            XmlElement root = doc.DocumentElement;
            if (root == null || !Namespaces.Contains(root.NamespaceURI) ||
                (root.LocalName != "FatturaElettronica" && root.LocalName != "FatturaElettronicaSemplificata"))
                throw new InvalidDataException("Il documento non è una FatturaPA supportata.");
            Stack<Tuple<XmlNode,int>> stack = new Stack<Tuple<XmlNode,int>>();
            stack.Push(Tuple.Create((XmlNode)root,0));
            int count = 0;
            while (stack.Count > 0) {
                Tuple<XmlNode,int> item = stack.Pop();
                if (++count > 100000 || item.Item2 > 64) throw new InvalidDataException("Fattura troppo complessa.");
                foreach (XmlNode child in item.Item1.ChildNodes) stack.Push(Tuple.Create(child,item.Item2+1));
            }
            return new Invoice { Name = name, Document = doc, SenderMatches = senderMatches };
        }
        public static string Render(Invoice invoice, string stylesheet) {
            XslCompiledTransform transform = new XslCompiledTransform();
            using (XmlReader reader = XmlReader.Create(stylesheet, new XmlReaderSettings {
                DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
                transform.Load(reader, new XsltSettings(false,false), null);
            string html;
            XmlWriterSettings outputSettings=transform.OutputSettings.Clone();
            outputSettings.Encoding=new UTF8Encoding(false);
            using(MemoryStream output=new MemoryStream()) {
                using(XmlWriter writer=XmlWriter.Create(output,outputSettings))
                    transform.Transform(invoice.Document,null,writer);
                html=Encoding.UTF8.GetString(output.ToArray());
            }
            int head = html.IndexOf("<head>",StringComparison.OrdinalIgnoreCase);
            if (head < 0) throw new InvalidDataException("Anteprima non prodotta.");
            string extra = "<meta http-equiv=\"Content-Security-Policy\" content=\"default-src 'none'; script-src 'none'; style-src 'unsafe-inline'; base-uri 'none'; form-action 'none'\">" +
                "<style>html,body{background:white} @media print{*{print-color-adjust:exact!important;-webkit-print-color-adjust:exact!important}tr{break-inside:avoid}}</style>";
            html = html.Insert(head+6,extra);
            if (Encoding.UTF8.GetByteCount(html) > 1900000) throw new InvalidDataException("Anteprima troppo grande.");
            return html;
        }
        public static IList<EmbeddedFile> Embedded(Invoice invoice) {
            List<EmbeddedFile> result = new List<EmbeddedFile>();
            foreach (XmlNode node in invoice.Document.SelectNodes("//*[local-name()='Allegati']")) {
                if (result.Count >= 200) throw new InvalidDataException("Troppi allegati.");
                Func<string,string> text = key => {
                    foreach (XmlNode child in node.ChildNodes) if (child.LocalName == key) return child.InnerText.Trim();
                    return "";
                };
                string content = text("Attachment");
                if (content.Length > ((MaxEmbedded+2)/3)*4+1024*1024) throw new InvalidDataException("Allegato oltre 10 MiB.");
                byte[] data = Convert.FromBase64String(content);
                if (data.Length > MaxEmbedded) throw new InvalidDataException("Allegato oltre 10 MiB.");
                string name = SafeName(text("NomeAttachment"));
                bool pdf = String.IsNullOrEmpty(text("AlgoritmoCompressione")) &&
                    (name.EndsWith(".pdf",StringComparison.OrdinalIgnoreCase) || text("FormatoAttachment").Equals("PDF",StringComparison.OrdinalIgnoreCase)) &&
                    data.Length >= 8 && Encoding.ASCII.GetString(data,0,5) == "%PDF-" &&
                    Regex.IsMatch(Latin1.GetString(data,Math.Max(0,data.Length-1024),Math.Min(1024,data.Length)),@"%%EOF\s*$");
                result.Add(new EmbeddedFile {Name=name,Bytes=data,Pdf=pdf});
            }
            return result;
        }
        public static string SafeName(string name) {
            if (String.IsNullOrWhiteSpace(name)) return "allegato";
            foreach (char c in Path.GetInvalidFileNameChars()) name=name.Replace(c,'_');
            name=Regex.Replace(name,@"[\p{Cc}\p{Cf}]","_").Trim().TrimEnd('.');
            if (name.Length>160) name=name.Substring(0,160);
            return String.IsNullOrWhiteSpace(name) ? "allegato" : name;
        }
        private sealed class Budget { public int Parts=200; public long Decoded=64L*1024*1024; }
        public static IList<Invoice> ReadEml(byte[] bytes, bool allowAny) {return ReadEml(bytes,allowAny,false);}
        public static IList<Invoice> ReadEml(byte[] bytes, bool allowAny,bool containerMatches) {
            if (bytes.Length>MaxMessage) throw new InvalidDataException("Messaggio oltre 32 MiB.");
            List<Invoice> result=new List<Invoice>();
            Message(Latin1.GetString(bytes),allowAny,containerMatches,new Budget(),0,result);
            return result;
        }
        private static Dictionary<string,string> Headers(string source,out string body) {
            int at=source.IndexOf("\r\n\r\n",StringComparison.Ordinal);
            int sep=4;
            if (at<0) {at=source.IndexOf("\n\n",StringComparison.Ordinal);sep=2;}
            if (at<0 || at>65536) throw new InvalidDataException("Intestazioni MIME mancanti o troppo grandi.");
            string head=Regex.Replace(source.Substring(0,at),@"\r?\n[ \t]+"," ");
            body=source.Substring(at+sep);
            Dictionary<string,string> fields=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            foreach (string line in head.Split('\n')) {
                int colon=line.IndexOf(':');
                if (colon<=0) continue;
                string key=line.Substring(0,colon).Trim();
                if (fields.ContainsKey(key)) {
                    if (key.Equals("From",StringComparison.OrdinalIgnoreCase) || key.Equals("Content-Type",StringComparison.OrdinalIgnoreCase) ||
                        key.Equals("Content-Transfer-Encoding",StringComparison.OrdinalIgnoreCase) || key.Equals("Content-Disposition",StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Intestazione MIME critica duplicata.");
                    continue;
                }
                fields[key]=line.Substring(colon+1).Trim();
            }
            return fields;
        }
        private static string Get(Dictionary<string,string> h,string key,string fallback) {
            string value; return h.TryGetValue(key,out value) ? value : fallback;
        }
        private static string Parameter(string header,string key) {
            Match match=Regex.Match(header,@"(?:^|;)\s*"+Regex.Escape(key)+@"(?<extended>\*)?\s*=\s*(?:""(?<quoted>[^""]*)""|(?<plain>[^;\r\n]*))",RegexOptions.IgnoreCase);
            if (!match.Success) return "";
            string value=(match.Groups["quoted"].Success ? match.Groups["quoted"].Value : match.Groups["plain"].Value).Trim();
            if (match.Groups["extended"].Success) {
                int marker=value.IndexOf("''",StringComparison.Ordinal);
                value=Uri.UnescapeDataString(marker>=0 ? value.Substring(marker+2) : value);
            }
            return Regex.Replace(value,@"=\?([^?]+)\?([bq])\?([^?]*)\?=",m => {
                byte[] data=m.Groups[2].Value.Equals("b",StringComparison.OrdinalIgnoreCase) ?
                    Convert.FromBase64String(m.Groups[3].Value) : QuotedPrintable(m.Groups[3].Value.Replace('_',' '));
                return Encoding.GetEncoding(m.Groups[1].Value).GetString(data);
            },RegexOptions.IgnoreCase);
        }
        private static byte[] QuotedPrintable(string value) {
            value=Regex.Replace(value,@"=\r?\n","");
            using (MemoryStream output=new MemoryStream()) {
                for (int i=0;i<value.Length;i++) {
                    if (value[i]=='=' && i+2<value.Length && Regex.IsMatch(value.Substring(i+1,2),@"^[0-9A-Fa-f]{2}$")) {
                        output.WriteByte(Convert.ToByte(value.Substring(i+1,2),16)); i+=2;
                    } else output.WriteByte((byte)value[i]);
                }
                return output.ToArray();
            }
        }
        private static byte[] Decode(string body,Dictionary<string,string> fields,Budget budget) {
            string encoding=Get(fields,"Content-Transfer-Encoding","7bit").ToLowerInvariant();
            byte[] bytes;
            switch (encoding) {
                case "base64": bytes=Convert.FromBase64String(body); break;
                case "quoted-printable": bytes=QuotedPrintable(body); break;
                case "7bit": case "8bit": case "binary": bytes=Latin1.GetBytes(body); break;
                default: throw new InvalidDataException("Codifica MIME non supportata.");
            }
            budget.Decoded-=bytes.Length;
            if (budget.Decoded<0) throw new InvalidDataException("Messaggio MIME troppo complesso.");
            return bytes;
        }
        private static void Message(string source,bool any,bool containerMatches,Budget budget,int depth,List<Invoice> result) {
            string body; Dictionary<string,string> headers=Headers(source,out body);
            string from=Get(headers,"From","");
            Part(headers,body,from,any,containerMatches,budget,depth,result);
        }
        private static void Part(Dictionary<string,string> h,string body,string from,bool any,bool containerMatches,Budget budget,int depth,List<Invoice> result) {
            if (--budget.Parts<0 || depth>6) throw new InvalidDataException("Troppe parti MIME o annidamento eccessivo.");
            string type=Get(h,"Content-Type","text/plain");
            if (type.StartsWith("multipart/",StringComparison.OrdinalIgnoreCase)) {
                string boundary=Parameter(type,"boundary");
                if (boundary.Length==0 || boundary.Length>200) throw new InvalidDataException("Boundary MIME non valido.");
                MatchCollection markers=Regex.Matches(body,@"(?m)^--"+Regex.Escape(boundary)+@"(?<end>--)?[ \t]*(?:\r?\n|$)");
                if (markers.Count<2 || !markers[markers.Count-1].Groups["end"].Success) throw new InvalidDataException("Messaggio MIME troncato.");
                for (int i=0;i<markers.Count-1;i++) {
                    if (markers[i].Groups["end"].Success) break;
                    int start=markers[i].Index+markers[i].Length, end=markers[i+1].Index;
                    if (end>=2 && body.Substring(end-2,2)=="\r\n") end-=2;
                    else if (end>=1 && body[end-1]=='\n') end--;
                    if (end<start) throw new InvalidDataException("Parte MIME vuota.");
                    string childBody; Dictionary<string,string> child=Headers(body.Substring(start,end-start),out childBody);
                    Part(child,childBody,from,any,containerMatches,budget,depth+1,result);
                }
                return;
            }
            string name=Parameter(Get(h,"Content-Disposition",""),"filename");
            if (name.Length==0) name=Parameter(type,"name");
            bool nested=type.Split(';')[0].Trim().Equals("message/rfc822",StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".eml",StringComparison.OrdinalIgnoreCase);
            if (nested) {
                byte[] raw=Decode(body,h,budget);
                if (raw.Length>MaxMessage) throw new InvalidDataException("Messaggio allegato troppo grande.");
                Message(Latin1.GetString(raw),any,containerMatches||IsSdiSender(from),budget,depth+1,result);
            } else if (IsInvoiceName(name) && (any || containerMatches || IsSdiSender(from))) {
                byte[] raw=Decode(body,h,budget);
                Invoice invoice=ReadInvoice(raw,name,IsSdiSender(from));
                invoice.ContainerSenderMatches=containerMatches && !invoice.SenderMatches;
                result.Add(invoice);
            }
        }
    }
}
