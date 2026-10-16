# e-Invoice Viewer per Thunderbird

![Icona](icons/invoice-64.png)

**Apri e stampa le fatture elettroniche direttamente dai messaggi di Thunderbird.**

Leggi XML e P7M con il foglio di stile AssoSoftware, consulta i PDF incorporati e stampa soltanto la fattura selezionata. L’elaborazione avviene sul tuo dispositivo, senza inviare le fatture a servizi esterni.

**[Scopri gli altri programmi dell’autore](https://quadra.acortina.it)** · [Edoardo Ghedina su GitHub](https://github.com/ghedy)

**[Scarica e-Invoice Viewer 1.2.0](https://github.com/ghedy/e-invoiceviewer/releases/tag/v1.2.0)**

## Installazione

Il file installabile è disponibile nella sezione **[Releases](https://github.com/ghedy/e-invoiceviewer/releases)**.

1. Scarica **e-invoiceviewer-1.2.0.xpi**, non l’archivio “Source code”.
2. In Thunderbird apri **Componenti aggiuntivi e temi**.
3. Dal menu con l’ingranaggio scegli **Installa componente aggiuntivo da file…**.
4. Seleziona l’XPI e conferma l’installazione.
5. Apri un messaggio contenente una fattura e premi **Visualizza fatture**.

Per preparare nuove release: [istruzioni per GitHub Desktop](docs/GITHUB-DESKTOP.md).

## Funzioni

- Fatture XML e contenuto XML di buste P7M/CMS, senza verifica della firma.
- Allegati diretti e fatture nel messaggio PEC postacert.eml.
- Scelta tra più fatture presenti nello stesso messaggio.
- PDF incorporati aperti su richiesta; gli altri formati vengono scaricati.
- Pulsante **Stampa fattura**: stampa solo il documento, senza avvisi e comandi del visualizzatore.
- Filtro sul dominio dichiarato pec.fatturapa.it, attivo per impostazione predefinita.
- Opzione **Consenti fatture da qualsiasi mittente**, utile per fatture inoltrate.

Per mantenere i riempimenti grigi e colorati in stampa può essere necessario attivare **Stampa sfondi**. Per adattare la fattura alla carta usa l’adattamento alla larghezza. Le preferenze dell’utente possono prevalere sullo stile di stampa.

## Compatibilità

Richiede Thunderbird **128 o successivo**. Funzionamento, grafica e stampa sono stati confermati dall’autore nel proprio ambiente; non è stata eseguita una matrice completa di versioni e sistemi operativi.

Sono disponibili [16 test automatici e una checklist di collaudo](docs/TESTING.md). I limiti e gli interventi di revisione sono descritti in [REVIEW.md](docs/REVIEW.md).

## Sicurezza e riservatezza

Il filtro confronta l’indirizzo completo dichiarato nel campo mittente; **non autentica la PEC** e non garantisce la sicurezza dei file.

L’estensione **non verifica firme digitali, certificati, revoche o validità fiscale** e non esegue scansioni antivirus.

Limiti: 20 MiB per XML/P7M, 10 MiB per allegato incorporato, 200 parti MIME esaminate e massimo cinque livelli di messaggi contando quello esterno. DTD ed entità XML sono rifiutate. Il rendering avviene in una cornice senza script e senza risorse remote.

Nessuna telemetria o trasmissione delle fatture. La preferenza sui mittenti è salvata localmente; i documenti visualizzati restano in memoria e gli eventuali download sono richiesti dall’utente. [Informativa completa](PRIVACY.md).

Per segnalare problemi non allegare documenti reali o dati cliente. Per vulnerabilità leggi [SECURITY.md](SECURITY.md).

## English

View Italian electronic invoices (XML and P7M) directly in Thunderbird, open embedded PDFs and print the selected invoice. Documents are processed locally. Digital signatures and certified email authenticity are not verified. See the Italian instructions above to install the XPI package.

## Sviluppo

Richiede Node.js 22+ e Python 3; non sono necessarie dipendenze npm.

~~~text
npm test
npm run build
~~~

Il pacchetto viene creato in dist/ con un elenco esplicito di file. La costruzione verifica gli hash del foglio AssoSoftware e delle condizioni originali. I file del codice sono inclusi senza trasformazioni.

Per caricare temporaneamente l’estensione usa **Debug dei componenti aggiuntivi** e seleziona manifest.json. Per la prima prova è disponibile un [messaggio sintetico](tests/fixtures/synthetic-invoice.eml), privo di validità fiscale e di certificazione PEC.

## Licenze e crediti

Il codice e le icone originali sono distribuiti con licenza permissiva [ISC](LICENSE).

Il foglio di stile è di proprietà di **AssoSoftware**, che lo rilascia gratuitamente secondo le proprie condizioni. È incluso invariato, con [condizioni ufficiali](licenses/AssoInvoice-LICENSE.pdf), [provenienza verificata](licenses/assosoftware-provenance.json) e [attribuzione separata](THIRD_PARTY_NOTICES.md). La licenza ISC non si estende a questo file.

Il progetto non dichiara affiliazioni ufficiali con AssoSoftware, Thunderbird o Agenzia delle Entrate.

Autore: **Edoardo Ghedina** · [Altri programmi](https://quadra.acortina.it) · [GitHub](https://github.com/ghedy).

Il porting Outlook è previsto come fase successiva.
