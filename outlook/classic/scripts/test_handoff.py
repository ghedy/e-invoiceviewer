"""Exercise real preview subprocesses with synthetic handoff files only."""
import json
from pathlib import Path
import os
import subprocess
import tempfile
import uuid

project = Path(__file__).resolve().parents[1]
root = project.parents[1]
base = Path(tempfile.gettempdir()) / "e-invoiceviewer-outlook"
base.mkdir(exist_ok=True)
xml = (root / "tests/fixtures/IT00000000000_TEST01.xml").read_bytes()
eml = (root / "tests/fixtures/synthetic-invoice.eml").read_bytes()
vendor_eml = eml.replace(b"sdi01@pec.fatturapa.it", b"vendor@example.org")
assert vendor_eml != eml
for arch in ("x86", "x64"):
    exe = root / "dist/outlook-classic-0.1.6" / arch / "OutlookPreview.exe"
    for label, payload, allow, sender, nested, expected in (
        ("trusted", xml, False, True, False, True),
        ("blocked", xml, False, False, False, False),
        ("explicit-option", xml, True, False, False, True),
        ("eml", eml, False, False, True, True),
        ("outer-pec", vendor_eml, False, True, True, True),
        ("unrelated-eml", vendor_eml, False, False, True, False),
    ):
        folder = base / uuid.uuid4().hex
        folder.mkdir()
        attachment = uuid.uuid4().hex + ".bin"
        (folder / attachment).write_bytes(payload)
        manifest = folder / "job.json"
        manifest.write_text(json.dumps({"ProtocolVersion": 1, "AllowAnySender": allow,
            "Attachments": [{"File": attachment, "Name": "invoice.xml" if not nested else "postacert.eml",
            "SenderMatches": sender, "Eml": nested}]}), encoding="utf-8")
        output = root / ".test-artifacts" / ("handoff-" + arch + "-" + label + ".html")
        result = subprocess.run([str(exe), "--job", str(manifest), "--render", str(output)],
            timeout=30, capture_output=True)
        assert (result.returncode == 0) == expected, (arch, label, result.returncode)
        assert not folder.exists(), ("handoff files not cleaned", folder)
        if expected:
            assert "FORNITORE FITTIZIO" in output.read_text(encoding="utf-8-sig")
        print(arch + ": subprocess handoff " + label + " passed")
