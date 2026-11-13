# Scheda Add-ons for Thunderbird — e-Invoice Viewer 1.2.1

## Italiano

**Nome:** e-Invoice Viewer

**Sommario:** Apri e stampa fatture elettroniche italiane XML/P7M e consulta i PDF incorporati direttamente da Thunderbird. Elaborazione locale.

**Descrizione:**

e-Invoice Viewer apre le fatture elettroniche italiane dai messaggi di Thunderbird e le mostra con il foglio di stile AssoSoftware.

1. Apri un messaggio con una fattura XML o P7M, anche contenuta nel messaggio PEC allegato postacert.eml.
2. Nella barra sopra il messaggio, premi **Visualizza fatture** (icona con il foglio blu). Il pulsante si attiva quando trova una fattura ammessa.
3. Se sono disponibili più fatture, scegli quella da mostrare. Premi il nome di un PDF incorporato per aprirlo oppure **Stampa fattura** per stampare il solo documento.
4. Per leggere fatture inoltrate o da altri mittenti, apri **Componenti aggiuntivi e temi → Estensioni → e-Invoice Viewer → Preferenze** e attiva **Consenti fatture da qualsiasi mittente**.

Con il filtro predefinito, il messaggio che contiene direttamente la fattura deve dichiarare un mittente nel dominio pec.fatturapa.it. Il filtro non autentica la PEC. Le firme digitali non vengono verificate e gli allegati non vengono sottoposti a scansione antivirus.

Elaborazione sul dispositivo, senza telemetria né invio di documenti. Limiti: 20 MiB per fattura XML/P7M e 10 MiB per allegato incorporato. Per riprodurre i colori in stampa, attiva **Stampa sfondi** se necessario. Il foglio di stile è di proprietà di AssoSoftware, che lo distribuisce gratuitamente alle proprie condizioni; è incluso senza modifiche.

## English

**Name:** e-Invoice Viewer

**Summary:** Open and print Italian XML/P7M electronic invoices and view embedded PDFs directly from Thunderbird. All invoice processing is local.

**Description:**

e-Invoice Viewer opens Italian electronic invoices from Thunderbird messages and displays them using the AssoSoftware stylesheet. The viewer and settings are available in English and Italian; the invoice layout and labels come from the original Italian stylesheet.

1. Open a message with an XML or P7M invoice, including invoices inside an attached postacert.eml certified email message.
2. In the toolbar above the message, click **View invoices** (the blue document icon). The button becomes available when a matching invoice is found.
3. If there are multiple invoices, select one. Click the name of an embedded PDF to open it, or click **Print invoice** to print only the document.
4. To view forwarded invoices or invoices from other senders, open **Add-ons and Themes → Extensions → e-Invoice Viewer → Preferences** and enable **Allow invoices from any sender**.

By default, the message directly containing the invoice must declare a sender in the pec.fatturapa.it domain. The sender filter does not authenticate certified email. Digital signatures are not verified and attachments are not scanned for malware.

Invoice processing takes place on your device, without telemetry or document uploads. Limits: 20 MiB per XML/P7M invoice and 10 MiB per embedded attachment. To reproduce colors in print, enable **Print backgrounds** if needed. The stylesheet is owned by AssoSoftware, which distributes it free of charge under its own terms; it is bundled unchanged.

## Campi comuni

- Supporto: https://github.com/ghedy/e-invoiceviewer/issues
- Sito autore: https://quadra.acortina.it
- Licenza: ISC per il codice originale; il foglio AssoSoftware ha condizioni separate.
- Informativa: il repository include PRIVACY.md. I dati delle fatture vengono elaborati solo in Thunderbird, senza telemetria o caricamenti.

## Screenshot e file di prova

Le schermate sono state acquisite in Thunderbird 159.0 con un profilo locale separato e una fattura fittizia. Carica le due schermate nella scheda; non caricare il file EML come immagine.

| Immagine | Didascalia italiana | English caption |
| --- | --- | --- |
| 01-message-toolbar-it.png / 01-message-toolbar.png | Pulsante Visualizza fatture nella barra del messaggio. | View invoices button in the message toolbar. |
| 02-invoice-viewer-it.png / 02-invoice-viewer.png | Fattura sintetica, allegato PDF e pulsante Stampa fattura. | Sample invoice, embedded PDF and Print invoice button. |

Per il revisore, usa demo-invoice.eml se il portale consente di condividere il file. In alternativa è disponibile un [messaggio sintetico XML nel repository](https://github.com/ghedy/e-invoiceviewer/blob/main/tests/fixtures/synthetic-invoice.eml) (senza il PDF dimostrativo).

## Version notes

The message listener now responds only to invoice requests. Removed the unnecessary tabs permission and the licenses/ directory from the XPI. Added English (default) and Italian localization.

## Notes for the reviewer

Thank you for the review. In 1.2.1 the runtime.onMessage listener is synchronous and returns undefined for unrelated messages. It only returns a Promise when handling a getInvoice request. The tabs permission has been removed; the add-on uses basic tab operations without reading privileged tab metadata.

The XPI no longer contains licenses/. The original stylesheet is unchanged. AssoSoftware attribution and a link to the official terms are available in Preferences and in THIRD_PARTY_NOTICES.md. The PDF and build provenance stay in the source repository.

English is the default locale, with Italian translations. The original third-party stylesheet remains in Italian. The JavaScript, HTML and localization files are shipped directly, without bundling, minification or remote code. No credentials are required.

To test, open the supplied synthetic EML in Thunderbird and click View invoices in the message toolbar. If the button is unavailable in a standalone message window, import the EML into a local mail folder and open it from there. The test document is fictitious and has no fiscal validity.
