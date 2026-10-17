using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace EInvoiceViewer.OutlookClassic {
    internal static class WindowBranding {
        internal static void Apply(Form form) {
            using(Stream source=typeof(WindowBranding).Assembly.GetManifestResourceStream("EInvoiceViewer.Invoice.ico")) {
                if(source==null) throw new InvalidDataException("Icona della finestra non disponibile.");
                using(Icon original=new Icon(source)) {
                    Icon icon=(Icon)original.Clone();
                    form.Icon=icon;
                    form.Disposed+=delegate {icon.Dispose();};
                }
            }
        }
    }
}
