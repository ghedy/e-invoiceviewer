# e-Invoice Viewer 1.2.2 per Thunderbird

## Note di versione in italiano

Corretta l’apertura delle fatture XML che dichiarano un foglio di stile prima dell’elemento principale. Queste fatture si possono ora visualizzare anche quando sono allegate a una PEC.

## English release notes

Fixed opening XML invoices that declare a stylesheet before the invoice root element, including invoices attached to a certified email message.

## Notes for the reviewer

The add-on now accepts XML processing instructions in the prolog, including xml-stylesheet, before checking the invoice root. The viewer continues to render with the bundled AssoSoftware stylesheet. The release adds no permissions.

Import the supplied synthetic EML into a local mail folder in Thunderbird and open it. Click View invoices in the outer message toolbar. The invoice inside postacert.eml should render, the metadata XML should be skipped, and Print invoice should be available. The fixture uses a fictitious invoice and does not require an account or credentials.

## Invio al catalogo

Apri [Thunderbird Add-ons Developer Hub](https://addons.thunderbird.net/en-US/developers/), seleziona la scheda esistente di e-Invoice Viewer e carica e-invoiceviewer-1.2.2.xpi come nuova versione. Incolla le note di versione nei campi disponibili e le note in inglese nel campo per il revisore.
