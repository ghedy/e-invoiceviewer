# Testi per il catalogo Thunderbird

## Nome

e-Invoice Viewer per Thunderbird

## Descrizione breve

Apri fatture elettroniche italiane XML e P7M, consulta i PDF incorporati e stampa la sola fattura, con elaborazione locale.

## Descrizione completa

e-Invoice Viewer permette di leggere le fatture elettroniche italiane direttamente dai messaggi di Thunderbird, usando il foglio di stile AssoSoftware.

Riconosce gli allegati XML e P7M e le fatture contenute in postacert.eml. Se nello stesso messaggio ci sono più fatture puoi scegliere quella da mostrare. I PDF incorporati sono accessibili con un clic e il pulsante Stampa fattura stampa soltanto il documento selezionato.

Tutta l’elaborazione delle fatture avviene sul dispositivo. Non vengono caricati documenti su server e non è presente telemetria.

Per impostazione predefinita vengono considerate le fatture con mittente dichiarato nel dominio pec.fatturapa.it. Nelle opzioni puoi consentire qualsiasi mittente, per esempio per le fatture inoltrate.

Il filtro sul mittente non autentica la PEC. L’estensione non verifica firme digitali, certificati o validità fiscale e non esegue scansioni antivirus.

Limiti: 20 MiB per XML/P7M e 10 MiB per allegato incorporato. Per mantenere i riempimenti in stampa può essere necessario attivare Stampa sfondi.

Il foglio di stile AssoSoftware è di proprietà del titolare ed è incluso invariato secondo le condizioni allegate. Il progetto non è affiliato ad AssoSoftware, Thunderbird o Agenzia delle Entrate.

## Campi di pubblicazione

- Pagina dell’autore: https://quadra.acortina.it
- Repository: https://github.com/ghedy/e-invoiceviewer
- Supporto: https://github.com/ghedy/e-invoiceviewer/issues
- Riservatezza: https://github.com/ghedy/e-invoiceviewer/blob/main/PRIVACY.md
- Licenza: ISC per il codice originale; indicare esplicitamente le condizioni separate del foglio AssoSoftware.
- Screenshot: fattura sintetica e accesso al PDF; nessun dato cliente. Devono essere acquisiti dalla versione effettiva, senza presentarli come già disponibili.

## Note per il revisore

Nessun codice remoto, nessun bundling e nessuna libreria JavaScript esterna. Ricostruzione: Node.js 22+ e Python 3, eseguire npm test e npm run build.

Il parser BER estrae soltanto eContent da strutture CMS SignedData supportate e non verifica firme. Le API mail restano in background; il documento trasformato è mostrato in una cornice con script disabilitati. Le facoltà same-origin e modals servono alla chiamata di stampa dal contenitore.

Il file tests/fixtures/synthetic-invoice.eml contiene una fattura di prova con mittente dichiarato, senza PEC autentica. Aprire il messaggio e usare il pulsante dell’estensione. Il codice sorgente è leggibile e incluso senza trasformazioni.

Riferimenti: [politiche ATN](https://thunderbird.github.io/atn-review-policy/) e [guida del revisore](https://addons-reviewer-guide.thunderbird.net/add-on-review-guide).

