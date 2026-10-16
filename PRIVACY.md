# Riservatezza

e-Invoice Viewer elabora le fatture e i loro allegati sul dispositivo dell’utente, usando le API di Thunderbird.

## Dati utilizzati

L’estensione legge il mittente dichiarato e l’elenco degli allegati del messaggio visualizzato per individuare le fatture. Quando si apre una fattura, legge il relativo allegato XML o P7M. I PDF incorporati vengono decodificati quando richiesti.

## Archiviazione e trasmissione

L’estensione non invia fatture, messaggi o allegati a server esterni, non contiene telemetria e non usa servizi di analisi. La preferenza sul filtro mittenti viene salvata localmente. Gli identificativi dei messaggi e il documento visualizzato restano in memoria durante l’uso; i download richiesti dall’utente vengono salvati dal browser o dal programma di posta.

Il pulsante di stampa usa la normale funzione di stampa. Destinazione e salvataggio in PDF dipendono dalle scelte dell’utente e dalla configurazione del sistema.

I collegamenti al sito dell’autore e a GitHub si aprono soltanto su richiesta. Visitandoli si applicano le condizioni dei rispettivi siti; il loro caricamento non è necessario per leggere o stampare le fatture.

## Permessi

- **messagesRead:** individuare e leggere gli allegati delle fatture.
- **storage:** ricordare la preferenza sui mittenti.
- **tabs:** gestire le schede del visualizzatore e degli allegati.

Il filtro mittenti non autentica la PEC. L’estensione non verifica firme digitali e non esegue scansioni antivirus.

Autore: [Edoardo Ghedina](https://github.com/ghedy). Altri programmi: [quadra.acortina.it](https://quadra.acortina.it).

