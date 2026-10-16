"""Create a reproducible XPI from an explicit file list, including license notices."""
import hashlib
import json
from pathlib import Path
import zipfile

root = Path(__file__).resolve().parents[1]
manifest = json.loads((root / "manifest.json").read_text(encoding="utf-8-sig"))
files = [
    "manifest.json", "background.js", "viewer.html", "viewer.js",
    "options.html", "options.js", "shared/core.js",
    "shared/assets/assofattura.xsl", "LICENSE", "THIRD_PARTY_NOTICES.md",
    "PRIVACY.md", "licenses/AssoInvoice-LICENSE.pdf",
    "licenses/assosoftware-provenance.json",
    *[f"icons/invoice-{size}.png" for size in (16, 32, 48, 64, 128)],
]
# Refuse accidental changes to the third-party files covered by separate terms.
provenance = json.loads((root / "licenses/assosoftware-provenance.json").read_text(encoding="utf-8-sig"))
assert hashlib.sha256((root / provenance["file"]).read_bytes()).hexdigest() == provenance["sha256"], "AssoSoftware XSL differs from the verified original"
assert hashlib.sha256((root / provenance["license_file"]).read_bytes()).hexdigest() == provenance["license_sha256"], "AssoSoftware license differs from the verified original"
assert manifest["version"] == json.loads((root / "package.json").read_text(encoding="utf-8-sig"))["version"]
assert all(path in files for path in manifest["background"]["scripts"])
assert all(path in files for path in manifest["icons"].values())
assert all(path in files for path in manifest["message_display_action"]["default_icon"].values())
assert manifest["options_ui"]["page"] in files
out = root / "dist"
out.mkdir(exist_ok=True)
target = out / ("e-invoiceviewer-" + manifest["version"] + ".xpi")
with zipfile.ZipFile(target, "w", compression=zipfile.ZIP_DEFLATED) as archive:
    for name in sorted(files):
        info = zipfile.ZipInfo(name, date_time=(2026, 1, 1, 0, 0, 0))
        info.compress_type = zipfile.ZIP_DEFLATED
        info.external_attr = 0o644 << 16
        archive.writestr(info, (root / name).read_bytes())
with zipfile.ZipFile(target) as archive:
    assert archive.testzip() is None
    assert set(archive.namelist()) == set(files)
digest = hashlib.sha256(target.read_bytes()).hexdigest()
(target.with_suffix(".xpi.sha256")).write_text(digest + "  " + target.name + "\n", encoding="ascii")
print("Pacchetto:", target)
print("SHA256:", digest)
print("Include attribuzione, condizioni AssoSoftware e informativa sulla riservatezza.")

