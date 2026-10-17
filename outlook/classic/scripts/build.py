"""Windows-only prototype build. Does not register or run the Outlook add-in."""
import hashlib
import io
import json
import os
from pathlib import Path
import shutil
import subprocess
import urllib.request
import zipfile

project = Path(__file__).resolve().parents[1]
root = project.parents[1]
lock = json.loads((project / "webview2-lock.json").read_text(encoding="utf-8-sig"))
cache = root / ".test-artifacts" / "webview2"
cache.mkdir(parents=True, exist_ok=True)
package = cache / "package.nupkg"
if not package.exists():
    package.write_bytes(urllib.request.urlopen(lock["url"], timeout=30).read())
assert hashlib.sha256(package.read_bytes()).hexdigest() == lock["sha256"], "WebView2 package hash mismatch"
compiler = Path(os.environ["WINDIR"]) / "Microsoft.NET/Framework64/v4.0.30319/csc.exe"
if not compiler.exists():
    compiler = Path(os.environ["WINDIR"]) / "Microsoft.NET/Framework/v4.0.30319/csc.exe"
assert compiler.exists(), ".NET Framework compiler not available"
provenance = json.loads((root / "licenses/assosoftware-provenance.json").read_text(encoding="utf-8-sig"))
assert hashlib.sha256((root / provenance["file"]).read_bytes()).hexdigest() == provenance["sha256"], "AssoSoftware XSL was modified"
assert hashlib.sha256((root / provenance["license_file"]).read_bytes()).hexdigest() == provenance["license_sha256"], "AssoSoftware license was modified"
output = root / "dist" / "outlook-classic-0.1.3"
sdk = cache / "sdk"
sdk.mkdir(exist_ok=True)
with zipfile.ZipFile(package) as archive:
    for name in ("Microsoft.Web.WebView2.Core.dll", "Microsoft.Web.WebView2.WinForms.dll"):
        (sdk / name).write_bytes(archive.read("lib/net462/" + name))
    license_text = archive.read("LICENSE.txt")
    refs = ["System.dll", "System.Core.dll", "System.Xml.dll", "System.Security.dll",
            "System.Windows.Forms.dll", "System.Drawing.dll", "Microsoft.CSharp.dll", "System.Web.Extensions.dll",
            str(sdk / "Microsoft.Web.WebView2.Core.dll"),
            str(sdk / "Microsoft.Web.WebView2.WinForms.dll")]
    def compile_file(destination, sources, target, platform, viewer=True):
        command = [str(compiler), "/nologo", "/utf8output", "/optimize+", "/target:" + target,
                   "/platform:" + platform, "/out:" + str(destination)]
        command += ["/reference:" + ref for ref in (refs if viewer else refs[:-2])]
        command += [str(project / "src" / source) for source in sources]
        subprocess.run(command, check=True)
    for arch in ("x86", "x64"):
        folder = output / arch
        folder.mkdir(parents=True, exist_ok=True)
        for name in ("Microsoft.Web.WebView2.Core.dll", "Microsoft.Web.WebView2.WinForms.dll"):
            shutil.copyfile(sdk / name, folder / name)
        (folder / "WebView2Loader.dll").write_bytes(archive.read("runtimes/win-" + arch + "/native/WebView2Loader.dll"))
        compile_file(folder / "EInvoiceViewer.Outlook.dll", ["Addin.cs", "BridgeSupport.cs"], "library", "anycpu", viewer=False)
        compile_file(folder / "OutlookPreview.exe", ["InvoiceEngine.cs", "ViewerForm.cs", "BridgeSupport.cs", "Preview.cs"], "winexe", arch)
        compile_file(folder / "OutlookSetup.exe", ["Setup.cs", "BridgeSupport.cs"], "winexe", arch, viewer=False)
        com_test = cache / ("ComStartupTests-" + arch + ".exe")
        subprocess.run([str(compiler), "/nologo", "/target:exe", "/platform:" + arch,
                        "/out:" + str(com_test), "/reference:" + str(folder / "EInvoiceViewer.Outlook.dll"),
                        str(project / "tests/ComStartupTests.cs")], check=True)
        shutil.copyfile(folder / "EInvoiceViewer.Outlook.dll", cache / "EInvoiceViewer.Outlook.dll")
        subprocess.run([str(com_test)], check=True, timeout=30)
        subprocess.run([str(folder / "OutlookPreview.exe"), "--startup-test"], check=True, timeout=30)
        print(arch + ": native WinForms startup passed")
        (folder / "shared/assets").mkdir(parents=True,exist_ok=True)
        (folder / "licenses").mkdir(exist_ok=True)
        shutil.copyfile(root / "shared/assets/assofattura.xsl", folder / "shared/assets/assofattura.xsl")
        for source, destination in (
            (root / "licenses/AssoInvoice-LICENSE.pdf", "licenses/AssoInvoice-LICENSE.pdf"),
            (root / "licenses/assosoftware-provenance.json", "licenses/assosoftware-provenance.json"),
            (project / "THIRD_PARTY_NOTICES.md", "THIRD_PARTY_NOTICES.md"),
            (root / "LICENSE", "LICENSE"),
            (project / "README.md", "LEGGIMI.md"),
            (project / "PRIVACY.md", "PRIVACY.md"),
        ):
            shutil.copyfile(source, folder / destination)
        (folder / "WEBVIEW2-LICENSE.txt").write_bytes(license_text)
        (folder / "WEBVIEW2-ORIGIN.json").write_text(json.dumps(lock, indent=2) + "\n", encoding="utf-8")
    shutil.copyfile(root / "tests/fixtures/IT00000000000_TEST01.xml", output / "FATTURA-DI-PROVA.xml")
test_exe = cache / "InvoiceEngineTests.exe"
command = [str(compiler), "/nologo", "/utf8output", "/target:exe", "/out:" + str(test_exe),
           "/reference:System.dll", "/reference:System.Core.dll", "/reference:System.Xml.dll",
           "/reference:System.Security.dll", "/reference:System.Web.Extensions.dll", str(project / "src/BridgeSupport.cs"), str(project / "src/InvoiceEngine.cs"),
           str(project / "tests/InvoiceEngineTests.cs")]
subprocess.run(command, check=True)
subprocess.run([str(test_exe), str(root)], check=True)
archive_path = root / "dist/outlook-classic-0.1.3-prototype.zip"
# Include only the files produced for this prototype, never arbitrary folder contents.
payload_names = [
    "EInvoiceViewer.Outlook.dll", "OutlookPreview.exe", "OutlookSetup.exe",
    "Microsoft.Web.WebView2.Core.dll", "Microsoft.Web.WebView2.WinForms.dll",
    "WebView2Loader.dll", "shared/assets/assofattura.xsl",
    "licenses/AssoInvoice-LICENSE.pdf", "licenses/assosoftware-provenance.json",
    "THIRD_PARTY_NOTICES.md", "LICENSE", "LEGGIMI.md", "PRIVACY.md",
    "WEBVIEW2-LICENSE.txt", "WEBVIEW2-ORIGIN.json",
]
with zipfile.ZipFile(archive_path, "w", zipfile.ZIP_DEFLATED) as archive:
    archive.write(output / "FATTURA-DI-PROVA.xml", "FATTURA-DI-PROVA.xml")
    for arch in ("x86", "x64"):
        for name in payload_names:
            archive.write(output / arch / name, arch + "/" + name)
archive_path.with_suffix(".zip.sha256").write_text(
    hashlib.sha256(archive_path.read_bytes()).hexdigest() + "  " + archive_path.name + "\n", encoding="ascii"
)
print("Prototype built:", archive_path)
print("Outlook integration has NOT been registered or tested in a real Outlook session.")
