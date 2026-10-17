# e-Invoice Viewer 0.1.6 per Outlook classico — anteprima pubblica

Apri le fatture elettroniche italiane XML/P7M dal messaggio selezionato in Outlook classico per Windows, visualizzale con il foglio di stile AssoSoftware, consulta gli allegati PDF e stampa soltanto la fattura.

- Ricerca diretta delle fatture negli EML allegati alla PEC e nei messaggi Outlook incorporati: non serve aprire prima il messaggio interno.
- Filtro sul mittente dichiarato del messaggio o di un suo contenitore nel dominio pec.fatturapa.it, con indicazione della provenienza nell’anteprima.
- Opzione esplicita per consentire altri mittenti.
- Visualizzatore in un processo separato da Outlook, elaborazione locale e registro tecnico senza contenuto delle email.
- Icona condivisa con la versione Thunderbird.

## Download e installazione

Scarica **outlook-classic-0.1.6-prototype.zip**, non gli archivi automatici “Source code”. Il file .sha256 permette di verificare l’integrità dello ZIP.

1. Estrai completamente lo ZIP in una cartella definitiva e chiudi Outlook.
2. Apri x64 per Outlook a 64 bit oppure x86 per Outlook a 32 bit: conta l’architettura di Outlook.
3. Avvia OutlookSetup.exe e premi **Installa per questo utente**.
4. Riapri Outlook, seleziona la PEC esterna e premi **Visualizza fatture**.

Per aggiornare un prototipo precedente, con Outlook chiuso usa prima **Rimuovi integrazione** nel programma OutlookSetup.exe della vecchia cartella. Per una prova senza integrazione, usa OutlookPreview.exe con FATTURA-DI-PROVA.xml inclusa nello ZIP.

Richiede **Outlook classico per Windows**, **.NET Framework 4.8 o successivo** e **Microsoft Edge WebView2 Runtime**. Non supporta il nuovo Outlook, Outlook sul web o Mac. Pacchetto prototipo senza firma di distribuzione; non è un componente del Microsoft Marketplace.

## Stato e limiti

Funzionamento confermato dall’autore sul proprio Outlook classico a 64 bit. Sono passati 14 controlli automatici sul motore e sul protocollo, oltre alle verifiche COM e di raccolta degli allegati a 32 e 64 bit. Queste prove non costituiscono una matrice completa di compatibilità Office.

Il filtro riguarda **indirizzi dichiarati**: non autentica la PEC e non garantisce che gli allegati siano sicuri. Firma digitale, certificazione PEC, validità fiscale e assenza di malware non vengono verificate.

[Istruzioni e diagnostica](https://github.com/ghedy/e-invoiceviewer/blob/main/outlook/classic/README.md) · [Riservatezza](https://github.com/ghedy/e-invoiceviewer/blob/main/outlook/classic/PRIVACY.md) · [Segnala un problema senza allegare fatture reali](https://github.com/ghedy/e-invoiceviewer/issues)

Codice originale ISC; foglio AssoSoftware invariato e componenti WebView2 soggetti alle rispettive condizioni incluse nel pacchetto.

**Autore: Edoardo Ghedina** · [Scopri gli altri programmi](https://quadra.acortina.it) · [GitHub ghedy](https://github.com/ghedy)
