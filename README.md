# e-Invoice Viewer per Thunderbird e Outlook classico

![Icona](icons/invoice-64.png)

**Apri e stampa le fatture elettroniche italiane dai messaggi di Thunderbird o Outlook classico per Windows.**

Leggi XML e P7M con il foglio di stile AssoSoftware, consulta i PDF incorporati e stampa soltanto la fattura selezionata. L’elaborazione avviene sul tuo dispositivo, senza inviare le fatture a servizi esterni.

**[Scopri gli altri programmi dell’autore](https://quadra.acortina.it)** · [Edoardo Ghedina su GitHub](https://github.com/ghedy)

## Download

| Programma di posta | Versione | Pacchetto e istruzioni |
| --- | --- | --- |
| Thunderbird 128 o successivo | 1.2.0 | [Scarica il componente XPI](https://github.com/ghedy/e-invoiceviewer/releases/tag/v1.2.0) |
| Outlook classico per Windows | 0.1.6 — anteprima pubblica | [Scarica il pacchetto ZIP](https://github.com/ghedy/e-invoiceviewer/releases/tag/outlook-v0.1.6) · [Istruzioni Outlook](outlook/classic/README.md) |

La versione Outlook richiede .NET Framework 4.8 o successivo e Microsoft Edge WebView2 Runtime. Scegli x64 oppure x86 in base all’architettura di **Outlook**. Non funziona con il nuovo Outlook, Outlook sul web o Outlook per Mac.

## Outlook classico: prima installazione

1. Estrai completamente lo ZIP in una cartella definitiva.
2. Chiudi Outlook e apri la sottocartella x64 oppure x86 appropriata.
3. Avvia **OutlookSetup.exe** e premi **Installa per questo utente**.
4. Riapri Outlook, seleziona la PEC esterna e premi **Visualizza fatture** nel gruppo **Fatture elettroniche**. Non occorre aprire prima l’EML allegato.

Per provare il visualizzatore senza installare l’integrazione, avvia **OutlookPreview.exe** e scegli la fattura sintetica inclusa. Se aggiorni un prototipo precedente, chiudi Outlook e rimuovi prima l’integrazione usando OutlookSetup.exe dalla vecchia cartella.

Il filtro Outlook considera il mittente dichiarato del messaggio con la fattura oppure di un contenitore: quando coincide soltanto la PEC esterna, l’anteprima lo indica. Non verifica firma digitale, certificazione PEC o assenza di malware. [Riservatezza Outlook](outlook/classic/PRIVACY.md) · [Requisiti, rimozione e diagnostica](outlook/classic/README.md).

La versione Outlook è un prototipo: funzionamento confermato dall’autore sul proprio Outlook classico, senza una matrice completa di compatibilità. Non è pubblicata sul Microsoft Marketplace.

## Installazione Thunderbird

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

Le indicazioni qui sotto descrivono Thunderbird. Per file temporanei e diagnostica della versione Outlook consulta la [sua informativa](outlook/classic/PRIVACY.md).

Il filtro confronta l’indirizzo completo dichiarato nel campo mittente; **non autentica la PEC** e non garantisce la sicurezza dei file.

L’estensione **non verifica firme digitali, certificati, revoche o validità fiscale** e non esegue scansioni antivirus.

Limiti: 20 MiB per XML/P7M, 10 MiB per allegato incorporato, 200 parti MIME esaminate e massimo cinque livelli di messaggi contando quello esterno. DTD ed entità XML sono rifiutate. Il rendering avviene in una cornice senza script e senza risorse remote.

Nessuna telemetria o trasmissione delle fatture. La preferenza sui mittenti è salvata localmente; i documenti visualizzati restano in memoria e gli eventuali download sono richiesti dall’utente. [Informativa completa](PRIVACY.md).

Per segnalare problemi non allegare documenti reali o dati cliente. Per vulnerabilità leggi [SECURITY.md](SECURITY.md).

## English

View Italian electronic invoices (XML and P7M) directly in Thunderbird or classic Outlook for Windows, open embedded PDFs and print the selected invoice. The Outlook version is a public preview; new Outlook, Outlook on the web and Mac are not supported. Documents are processed locally. Digital signatures and certified email authenticity are not verified. See the Italian instructions above to install the XPI package.

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

Il codice della versione Outlook classico è in [outlook/classic](outlook/classic). Per costruire il pacchetto Windows: `python outlook/classic/scripts/build.py`. Requisiti e prove sono descritti nel [README Outlook](outlook/classic/README.md).
