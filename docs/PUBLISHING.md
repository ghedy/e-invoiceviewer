# Pubblicazione e visibilità

## Stato della preparazione

- Pacchetto 1.2.0 con icone, attribuzione AssoSoftware, condizioni ufficiali e informativa sulla riservatezza.
- XSL identico byte per byte al download ufficiale; provenienza e hash registrati.
- Codice originale ISC, materiale AssoSoftware sotto condizioni separate.
- Funzionamento e stampa confermati dall’autore nel proprio ambiente; la matrice completa resta in TESTING.md.
- Identificatore interno mantenuto per continuità: e-invoiceviewer@localhost. È un identificatore tecnico, non un indirizzo di contatto.
- Nome proposto per il repository pubblico: ghedy/e-invoiceviewer.
- Istruzioni operative: [GitHub Desktop](GITHUB-DESKTOP.md).
- Testi pronti: [release](RELEASE-v1.2.0.md) e [catalogo Thunderbird](ATN-LISTING.md).

Repository pubblico: https://github.com/ghedy/e-invoiceviewer

Release 1.2.0 pubblicata il 16 ottobre 2026: https://github.com/ghedy/e-invoiceviewer/releases/tag/v1.2.0

Download pubblico dell’XPI verificato identico al pacchetto locale. I controlli automatici GitHub risultano superati. Gli screenshot reali con dati sintetici e l’invio ad ATN restano da completare.

## Presentazione consigliata

Titolo: **e-Invoice Viewer – Fatture elettroniche per Thunderbird**

Descrizione breve: **Apri fatture XML e P7M e consulta i PDF allegati direttamente in Thunderbird, con elaborazione locale.**

L’argomento principale è il tempo risparmiato nell’aprire le fatture. Usare una dimostrazione breve con dati inventati: messaggio PEC, clic sul pulsante, fattura leggibile, clic sul PDF.

Non usare espressioni come “PEC sicura”, “allegati privi di virus”, “firma valida” o affiliazioni ufficiali non esistenti.

## Piano di visibilità

Queste sono proposte editoriali, senza garanzia di download:

1. **Distribuzione:** catalogo Thunderbird per chi cerca un componente aggiuntivo e GitHub Releases per chi vuole il codice o il pacchetto.
2. **Ricerca:** titolo e README in italiano con breve introduzione inglese; descrizione repository chiara. Topic suggeriti: thunderbird, thunderbird-extension, fatturapa, fattura-elettronica, pec, xml, p7m, italian-einvoice.
3. **Dimostrazione:** due screenshot leggibili e un video di 20–40 secondi. Evidenziare il numero di passaggi risparmiati; evitare fatture vere.
4. **Autore:** collegamento al [sito quadra.acortina.it](https://quadra.acortina.it) già inserito nel README e nelle opzioni; aggiungerlo anche nella scheda del catalogo. Il profilo GitHub resta un collegamento separato: i repository privati non sono una vetrina pubblica.
5. **Pubblico iniziale:** commercialisti, piccole imprese e utenti Thunderbird italiani. Presentare il progetto nelle comunità pertinenti rispettandone le regole; nessun invio automatico di messaggi.
6. **Ritorno:** note di rilascio comprensibili, canale per segnalazioni e tempi di risposta sostenibili. Favorire una prova semplice prima di chiedere stelle o condivisioni.
7. **Misura:** confrontare download di release e statistiche del catalogo, visite al repository e visite al sito dell’autore se disponibile. Non aggiungere tracciamento alle fatture.

[GitHub descrive i topic come strumento di classificazione e scoperta](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/classifying-your-repository-with-topics).
Le [indicazioni Mozilla sulla promozione](https://extensionworkshop.com/documentation/publish/promoting-your-extension/) suggeriscono comunità e canali pertinenti; per la distribuzione Thunderbird usare ATN.

## Outlook, fase successiva

Conservare la logica pura in shared/core.js e i test; separare acquisizione dei messaggi, autorizzazioni e apertura degli allegati. La cornice Thunderbird non è un porting automatico: l’accesso ai messaggi e il rendering XSLT andranno verificati nell’ambiente Outlook scelto.

