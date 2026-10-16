# Collaudo

## Verifiche automatiche

`npm test` esegue test di regressione su confronto mittenti, encoding UTF-8/UTF-16, contenuto CMS con octet string primitive e constructed, input troncati o con OID errato, DTD, limiti di dimensione, Base64, riconoscimento PDF, isolamento dei messaggi e autorizzazione delle richieste.

La parte Thunderbird usa API simulate. L’assenza di dipendenze npm rende la suite eseguibile senza installazione di pacchetti. Il pacchetto di sviluppo si crea con `npm run build`.

## Riscontro dell’autore

Il 16 ottobre 2026 l’autore ha confermato il funzionamento dell’estensione nel proprio ambiente Thunderbird e, dopo gli interventi, ha approvato l’aspetto e la stampa. La versione esatta utilizzata e una matrice di sistemi operativi non sono state raccolte: questo riscontro non certifica tutti i casi sotto elencati.

La suite automatica comprende 16 test, inclusi avvio della stampa, cambio di fattura e gestione degli errori. Le prove grafiche e di stampa reali non sono simulate dalla suite.

## Esempio sintetico

Aprire in un profilo di test il file tests/fixtures/synthetic-invoice.eml, oppure caricare il suo XML. Tutti i dati sono inventati; non è una fattura fiscalmente valida e il mittente è semplicemente dichiarato, senza alcuna certificazione. Non inviare il messaggio. L’esempio serve per la prima prova di apertura e di resa grafica, senza allegati PDF.

## Prove da eseguire in un profilo di test Thunderbird

Usare esclusivamente fatture sintetiche o anonimizzate e una casella di test. Non pubblicare fatture vere, indirizzi PEC dei clienti o dati fiscali.

- [ ] Caricamento temporaneo del manifest e installazione del pacchetto XPI.
- [ ] Thunderbird 128 ESR e versione corrente; verificare disponibilità di API e XSLT.
- [ ] XML ordinario, semplificato e con firma XAdES; resa completa e stampa.
- [ ] Pulsante Stampa fattura: anteprima con solo il documento selezionato, senza avvisi, nome file esterno o comandi degli allegati; tutte le pagine delle fatture lunghe, anche su A4.
- [ ] Confrontare schermo e anteprima: riempimenti grigi/ciano, proporzioni delle colonne e caratteri; verificare Stampa sfondi e adattamento alla larghezza con una fattura multipagina.
- [ ] Stampa disabilitata durante il caricamento, dopo errori e al cambio di fattura; annullare e ripetere la stampa.
- [ ] P7M reali DER e BER, UTF-8 e ISO-8859-1, caratteri accentati.
- [ ] PEC con postacert.eml e multipart annidati, filename MIME codificati, Base64 e quoted-printable.
- [ ] Mittente del gestore PEC diverso dal mittente SdI interno: il controllo interno deve consentire la fattura.
- [ ] Mittente falso nel nome visualizzato e dominio con suffisso ostile: pulsante assente.
- [ ] Due finestre e due schede mail: ogni visualizzatore deve conservare la propria fattura.
- [ ] Cambi rapidi tra messaggi mentre la lettura è in corso.
- [ ] Più XML, inclusi metadati e XML non fattura: scegliere una fattura valida dopo una non valida.
- [ ] Opzione qualsiasi mittente attivata e disattivata, anche con schede già aperte.
- [ ] PDF incorporato valido, troncato, dichiarato PDF ma contenente HTML.
- [ ] PDF con il visualizzatore di Thunderbird; controllare che non sia affidato a un interprete attivo generico.
- [ ] Altri allegati vengono scaricati senza esecuzione; nomi con caratteri speciali.
- [ ] Allegati oltre 10 MiB e fatture oltre 20 MiB: errore leggibile o pulsante assente.
- [ ] DTD, entità, XML malformato o molto profondo: rifiuto leggibile.
- [ ] Verificare nel monitor di rete l’assenza di richieste esterne durante il rendering.
- [ ] Chiudere la fattura mentre il PDF è ancora aperto; documentare la disponibilità del PDF.
- [ ] Riavviare e ricaricare l’estensione: il messaggio invita a riaprire la fattura.
- [ ] Seguire la checklist di revisione ATN prima dell’invio.

I test automatici da soli non autorizzano a dichiarare questi casi come superati.

