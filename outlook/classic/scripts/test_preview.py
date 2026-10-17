"""Render the bundled synthetic invoice in WebView2 without opening Outlook."""
import os
from pathlib import Path
import shutil
import subprocess
import sys

project = Path(__file__).resolve().parents[1]
root = project.parents[1]
arch = sys.argv[1] if len(sys.argv) > 1 else "x64"
assert arch in ("x86", "x64")
package = root / "dist/outlook-classic-0.1.5" / arch
work = root / ".test-artifacts" / ("preview-render-" + arch)
work.mkdir(parents=True, exist_ok=True)
names = ("OutlookPreview.exe", "Microsoft.Web.WebView2.Core.dll",
         "Microsoft.Web.WebView2.WinForms.dll", "WebView2Loader.dll")
for name in names:
    shutil.copyfile(package / name, work / name)
compiler = Path(os.environ["WINDIR"]) / "Microsoft.NET/Framework64/v4.0.30319/csc.exe"
if not compiler.exists():
    compiler = Path(os.environ["WINDIR"]) / "Microsoft.NET/Framework/v4.0.30319/csc.exe"
executable = work / "PreviewRenderTests.exe"
command = [str(compiler), "/nologo", "/target:exe", "/platform:" + arch, "/out:" + str(executable)]
command += ["/reference:" + name for name in ("System.dll", "System.Core.dll",
    "System.Drawing.dll", "System.Windows.Forms.dll", "System.Xml.dll")]
command += ["/reference:" + str(work / name) for name in names if name != "WebView2Loader.dll"]
command.append(str(project / "tests/PreviewRenderTests.cs"))
subprocess.run(command, check=True)
subprocess.run([str(executable), str(root)], check=True, timeout=65)
print("WebView2 synthetic preview passed:", arch)

