# e-Invoice Viewer 0.1.7 per Outlook classico — anteprima pubblica

La finestra di visualizzazione ora si intitola **e-Invoice Viewer — Outlook**, con la stessa icona del pulsante principale e della versione Thunderbird. Il setup si intitola **e-Invoice Viewer — Configurazione Outlook** e usa la stessa icona, presente anche nella diagnostica e nei due programmi eseguibili Windows.

Restano disponibili apertura diretta delle fatture XML/P7M dalla PEC e dai messaggi EML incorporati, visualizzazione AssoSoftware, allegati PDF e stampa del documento.

## Installazione e aggiornamento

Scarica **outlook-classic-0.1.7-prototype.zip**, estrailo completamente in una cartella definitiva e usa la sottocartella **x64** o **x86** in base all’architettura di Outlook.

Per aggiornare: chiudi Outlook e le finestre delle fatture; dal vecchio OutlookSetup.exe premi **Rimuovi integrazione**. Poi avvia OutlookSetup.exe dalla nuova cartella e premi **Installa per questo utente**.

Per una prima prova senza installazione, apri OutlookPreview.exe e scegli FATTURA-DI-PROVA.xml inclusa nel pacchetto.

Richiede Outlook classico per Windows, .NET Framework 4.8 o successivo e Microsoft Edge WebView2 Runtime. Non funziona con il nuovo Outlook, Outlook sul web o Mac. Rimane un’anteprima pubblica: la modifica del titolo non cambia lo stato del rilascio.

Firma digitale, certificazione PEC, validità fiscale e assenza di malware non vengono verificate. Il filtro confronta indirizzi dichiarati del messaggio o del contenitore. Elaborazione locale, senza invio delle fatture a server esterni.

[Istruzioni](https://github.com/ghedy/e-invoiceviewer/blob/main/outlook/classic/README.md) · [Riservatezza](https://github.com/ghedy/e-invoiceviewer/blob/main/outlook/classic/PRIVACY.md) · [Altri programmi dell’autore](https://quadra.acortina.it)

Codice originale ISC; foglio AssoSoftware e componenti Microsoft soggetti alle condizioni incluse nel pacchetto.
