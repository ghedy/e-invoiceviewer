using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using EInvoiceViewer.OutlookClassic;
internal static class PreviewRenderTests {
    [STAThread] private static int Main(string[] args) {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        int result=1;
        string root=args[0];
        Invoice invoice=InvoiceEngine.ReadInvoice(File.ReadAllBytes(Path.Combine(root,"tests/fixtures/IT00000000000_TEST01.xml")),"TEST.xml",false);
        using(ViewerForm form=new ViewerForm(new[]{invoice},Path.Combine(root,"shared/assets/assofattura.xsl"))) {
            form.StartPosition=FormStartPosition.Manual; form.Location=new Point(-2500,-2500);
            form.ShowInTaskbar=false;
            WebView2 observed=(WebView2)typeof(ViewerForm).GetField("web",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(form);
            observed.CoreWebView2InitializationCompleted+=(s,e)=>{
                if(e.IsSuccess) {
                    observed.CoreWebView2.NavigationStarting+=(sender,nav)=>Console.WriteLine("Navigation: "+(nav.Uri.StartsWith("data:")?"generated document":nav.Uri));
                    observed.CoreWebView2.NavigationCompleted+=(sender,nav)=>Console.WriteLine("Completed: "+nav.IsSuccess+" "+nav.WebErrorStatus);
                }
            };
            form.Shown+=async delegate {
                try {
                    WebView2 web=(WebView2)typeof(ViewerForm).GetField("web",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(form);
                    for(int i=0;i<45;i++) {
                        await Task.Delay(1000);
                        bool ready=(bool)typeof(ViewerForm).GetField("ready",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(form);
                        if(ready) break;
                    }
                    Console.WriteLine("WebView bounds: "+web.Bounds);
                    string metrics=await web.CoreWebView2.ExecuteScriptAsync("({supplier: document.body.innerText.indexOf('FORNITORE FITTIZIO') >= 0, textLength: document.body.innerText.length, width: innerWidth, height: innerHeight, contentHeight: document.documentElement.scrollHeight, encoding: document.characterSet})");
                    Console.WriteLine(metrics);

                    using(FileStream image=File.Create(Path.Combine(root,".test-artifacts/outlook-webview-"+(IntPtr.Size==8?"x64":"x86")+".png")))
                        await web.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png,image);
                    result=metrics.Contains("\"supplier\":true") && metrics.Contains("\"encoding\":\"UTF-8\"") && web.ClientSize.Width>=600 && web.ClientSize.Height>=300 ? 0 : 1;
                } catch(Exception error) {Console.Error.WriteLine(error);result=1;}
                finally {form.Close();}
            };
            Application.Run(form);
        }
        return result;
    }
}
