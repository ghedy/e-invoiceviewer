# e-Invoice Viewer 1.2.0 per Thunderbird

Apri le fatture elettroniche italiane direttamente dai messaggi, consulta i PDF incorporati e stampa solo la fattura che stai visualizzando.

## Installazione

1. Scarica **e-invoiceviewer-1.2.0.xpi** dagli allegati della release.
2. In Thunderbird apri **Componenti aggiuntivi e temi**.
3. Dal menu con l’ingranaggio scegli **Installa componente aggiuntivo da file…** e seleziona il file XPI.
4. Apri un messaggio con una fattura e premi **Visualizza fatture**.

Non usare gli archivi automatici “Source code” per installare l’estensione.

## Funzioni

- XML e contenuto XML di P7M, senza verifica della firma.
- Messaggi PEC con postacert.eml e più fatture selezionabili.
- PDF incorporati aperti su richiesta.
- Stampa del solo documento; se mancano i colori, attiva **Stampa sfondi** nelle opzioni di stampa.
- Elaborazione locale, senza inviare le fatture a servizi esterni.

Il filtro sul dominio dichiarato pec.fatturapa.it è attivo per impostazione predefinita. Puoi consentire altri mittenti nelle opzioni. Il filtro non autentica la PEC e non garantisce la sicurezza degli allegati.

## Compatibilità e limiti

Il manifest richiede Thunderbird 128 o successivo. Il funzionamento e la stampa sono stati confermati dall’autore nel proprio ambiente; non è stata eseguita una matrice completa di versioni e sistemi operativi.

Non vengono verificate firme, certificati, revoche o validità fiscale; non sono eseguite scansioni antivirus.

Il codice originale è ISC; il foglio AssoSoftware è incluso invariato con attribuzione e condizioni separate. Il pacchetto non dichiara affiliazioni ufficiali.

[Scopri gli altri programmi di Edoardo Ghedina](https://quadra.acortina.it).

