# e-Invoice Viewer per Outlook classico — prototipo 0.1.1

Questo è un prototipo per **Outlook classico su Windows**, sviluppato separatamente dalla versione Thunderbird. Non è una versione per il nuovo Outlook, Outlook sul web o Mac.

## Correzione 0.1.1

L’inizializzazione grafica viene eseguita prima della finestra di scelta del file. È corretto l’errore SetCompatibleTextRenderingDefault segnalato durante la prima prova. La costruzione verifica l’avvio di una finestra nativa, senza mostrarla, sia a 32 sia a 64 bit.

## Cosa fa

Aggiunge alla scheda della posta un gruppo **Fatture elettroniche**, con il pulsante **Visualizza fatture** e l’opzione **Consenti qualsiasi mittente**.

Legge XML/P7M, postacert.eml e messaggi incorporati di Outlook; apre una finestra dedicata con selezione delle fatture, allegati e stampa del solo documento. La trasformazione AssoSoftware è eseguita da .NET, senza dipendere da XSLT nel browser.

Il filtro confronta il mittente dichiarato con pec.fatturapa.it, anche dentro il messaggio PEC. Non autentica la PEC. La firma digitale non viene verificata e non sono eseguite scansioni antivirus.

## Requisiti

- Windows e Outlook classico desktop, con allegati scaricati e disponibili localmente.
- .NET Framework 4.8 o successivo.
- Microsoft Edge WebView2 Runtime aggiornato.
- Pacchetto x86 per Outlook a 32 bit, oppure x64 per Outlook a 64 bit: conta l’architettura di Outlook, non quella di Windows.

Per vedere l’architettura: Outlook → File → Account di Office → Informazioni su Outlook.

Non richiede di inserire password della casella o di spostare la PEC su Microsoft 365. La compatibilità reale con il proprio account va verificata in Outlook.

## Prima prova senza installazione

1. Estrai completamente lo ZIP in una cartella del tuo utente.
2. Apri la sottocartella x86 oppure x64 appropriata.
3. Avvia OutlookPreview.exe.
4. Seleziona la FATTURA-DI-PROVA.xml presente nella cartella principale, oppure un XML/P7M/EML di test.
5. Verifica anteprima, allegati e stampa.

Il programma di prova non si collega ad Outlook e non legge la posta. L’apertura manuale dei file non applica il filtro del mittente.

## Prova dell’integrazione Outlook

1. Chiudi Outlook classico.
2. Lascia il pacchetto in una posizione definitiva; non avviarlo dentro lo ZIP.
3. Nella cartella dell’architettura corretta avvia OutlookSetup.exe.
4. Premi **Installa per questo utente**.
5. Avvia Outlook e seleziona una sola email con fattura.
6. Usa **Visualizza fatture** dal gruppo **Fatture elettroniche**.

La configurazione registra soltanto questo add-in nell’account Windows corrente. Non modifica le impostazioni di sicurezza di Office. Su computer gestiti l’installazione può richiedere l’intervento dell’amministratore.

Il prototipo non ha una firma di distribuzione commerciale: se l’esecuzione viene bloccata dalle protezioni del computer, non disattivarle. Prima della distribuzione pubblica serviranno un installer firmato e prove su installazioni reali.

Per rimuovere l’integrazione, chiudi Outlook e usa **Rimuovi integrazione** nello stesso OutlookSetup.exe e nella stessa cartella.

## Stato del collaudo

Il nucleo di lettura, MIME, XML, P7M e trasformazione è verificato da test automatici. La compilazione genera pacchetti x86 e x64.

L’aggancio COM, i pulsanti della barra e l’anteprima WebView2 devono ancora essere collaudati in Outlook reale: sul sistema di sviluppo non è stata rilevata un’installazione utilizzabile di Outlook. Non è garantito che il prototipo si carichi in tutte le configurazioni.

Da verificare: PEC con postacert come file EML e come messaggio Outlook incorporato, firme S/MIME del messaggio, download degli allegati, caselle Exchange, finestre Explorer/Inspector, stampa multipagina e rimozione.

## Costruzione

Da Windows, con Python 3 e il compilatore .NET Framework disponibile:

    python outlook/classic/scripts/build.py

Il componente Microsoft WebView2 è scaricato da NuGet con versione e SHA256 fissati in webview2-lock.json. Le condizioni Microsoft sono incluse in ogni pacchetto.

Il codice originale è ISC. Il foglio AssoSoftware è copiato invariato ed è soggetto alle condizioni incluse. Questo prototipo non fa parte dell’XPI Thunderbird e non è stato inviato a Microsoft Marketplace.
