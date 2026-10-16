# Revisione del 16 ottobre 2026

Revisione dei file presenti nella cartella di lavoro; nessun repository Git o remoto configurato era disponibile. Non è un audit indipendente né una certificazione di sicurezza.

## Problemi riscontrati e interventi

| Gravità | Problema precedente | Intervento |
| --- | --- | --- |
| Alta | `author.includes("@pec.fatturapa.it")` accettava nomi visualizzati ingannevoli e suffissi di dominio | Confronto conservativo dell’indirizzo completo; avviso esplicito che From non autentica la provenienza |
| Alta | Tutte le schede condividevano `currentInvoice` su disco | Identificatori in memoria per messaggio e per visualizzatore; autorizzazione del richiedente; cancellazione del vecchio valore persistente |
| Alta | Il P7M veniva scandito alla ricerca di XML, anche fuori dal contenuto CMS | Lettura BER limitata e selezione esclusiva di eContent nella struttura CMS SignedData; nessun verdetto sulla firma |
| Media | Parsing MIME tramite separazione testuale di un singolo boundary | API native Thunderbird, inclusi messaggi allegati; controllo indipendente del mittente interno |
| Media | Nessun limite a dimensione, ricorsione BER o numero di parti | Limiti su dimensioni, nodi BER, profondità e parti MIME; limiti di complessità prima della trasformazione XSLT |
| Media | Tutti i formati allegati venivano aperti direttamente | Apertura diretta solo di PDF riconosciuti; gli altri formati sono scaricati |
| Media | Rendering inserito nel documento privilegiato dell’estensione | Cornice sandbox senza script o navigazione superiore e politica che blocca risorse remote |
| Media | Solo la prima fattura candidata era gestita | Selettore di tutte le fatture candidate entro il limite |
| Media | Moduli PKI inutilizzati e incoerenti con il runtime | Rimozione dei moduli non collegati e delle dipendenze inutilizzate |
| Pubblicazione | Licenza assente nel filesystem, provenienza XSL non documentata | Testo ISC coerente con il package originale e avviso separato sul materiale di terzi |

## Decisioni mantenute

Nessuna verifica crittografica, di revoca o di certificazione PEC. L’estrazione CMS riconosce un contenitore supportato, senza attestare che il file sia firmato correttamente. Anche XML con firme XAdES viene solo visualizzato.

Il filtro resta attivo per impostazione predefinita. L’opzione di ampliamento dei mittenti non disattiva i controlli sul contenuto. Non viene promessa assenza di virus, nemmeno per documenti con mittente SdI.

Sono riconosciuti anche nomi file con prefisso diverso da IT: il paese del trasmittente non è una proprietà di sicurezza.

## Limiti residui

- Mancano ancora prove reali su Thunderbird per MIME PEC, cornice XSLT, apertura blob PDF e salvataggio degli altri allegati.
- API simulate nei test: nessuna prova di compatibilità completa con tutte le versioni dichiarate.
- Nessuna validazione XSD, fiscale o semantica. Un XML malformato viene rifiutato nel visualizzatore; i test senza DOM non sostituiscono questa prova.
- I limiti riducono il lavoro su input ostili, senza garantire un tempo massimo di rendering.
- Il parser BER è intenzionalmente limitato: non supporta ogni possibile costruzione ASN.1/CMS, firme detached o buste annidate.
- I PDF possono restare pericolosi; intestazione e terminazione sono controlli di riconoscimento.
- I file scaricati possono essere pericolosi quando aperti esternamente.
- Le schede già aperte devono essere riaperte dal messaggio dopo riavvio o ricaricamento dell’estensione.
- Gli URL temporanei degli allegati restano validi fino alla chiusura del visualizzatore. Verificare i casi di PDF ancora aperto quando si chiude la fattura.
- Non è stato configurato un `update_url` esterno. Pianificare gli aggiornamenti tramite la distribuzione scelta.

## Riferimenti

- [API messages di Thunderbird](https://webextension-api.thunderbird.net/en/mv2/messages.html): listAttachments e MessageAttachment.message.
- [Politiche di revisione Thunderbird](https://thunderbird.github.io/atn-review-policy/).
- [AssoSoftware](https://www.assosoftware.it/servizi-offerti/assoinvoice/): risorsa separata e condizioni d’uso da chiarire per questa copia.


## Aggiornamento per la distribuzione 1.2.0

L’autore ha confermato il funzionamento e approvato grafica e stampa nel proprio ambiente. La copia del foglio XSL è risultata identica byte per byte al download ufficiale; condizioni, attribuzione e impronte sono ora incluse in licenses/ e THIRD_PARTY_NOTICES.md. Le incertezze sulla provenienza descritte nella revisione iniziale sono quindi state documentate. Il rapporto iniziale resta come storico; i casi non verificati della matrice di collaudo restano elencati in TESTING.md.
