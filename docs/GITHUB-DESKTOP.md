# Pubblicazione con GitHub Desktop

Il repository è ora pubblico e la [release 1.2.0](https://github.com/ghedy/e-invoiceviewer/releases/tag/v1.2.0) è disponibile. I passaggi sotto documentano la prima pubblicazione e possono essere adattati alle prossime versioni.

Repository locale: C:\Progetti\e-invoiceviewer

Nome proposto del repository pubblico: **e-invoiceviewer**.

## Pubblicare il repository

1. In GitHub Desktop accedi con l’account **ghedy**.
2. Scegli **File → Add local repository…** e seleziona C:\Progetti\e-invoiceviewer.
3. Premi **Publish repository**.
4. Nome: **e-invoiceviewer**. Descrizione: **Fatture elettroniche italiane XML/P7M e PDF allegati direttamente in Thunderbird, con stampa ed elaborazione locale.**
5. Disattiva **Keep this code private**: questo progetto deve essere pubblico.
6. Conferma con **Publish repository**.

Gli altri repository del profilo rimangono privati. Il sito dell’autore è già collegato dal README.

## Creare la prima release

1. Dal repository aperto su GitHub scegli **Releases → Draft a new release**.
2. Crea il tag **v1.2.0** sul ramo **main**.
3. Titolo: **e-Invoice Viewer 1.2.0 per Thunderbird**.
4. Copia il testo di docs/RELEASE-v1.2.0.md nella descrizione.
5. Allega dist/e-invoiceviewer-1.2.0.xpi e dist/e-invoiceviewer-1.2.0.xpi.sha256.
6. Dopo aver ricontrollato gli allegati, pubblica la release.

Non caricare node_modules, messaggi cliente o vecchi XPI di sviluppo. Il pacchetto XPI e gli archivi automatici Source code hanno funzioni diverse: per installare si usa l’XPI.

## Rendere il progetto trovabile

- In About inserisci il sito https://quadra.acortina.it e una descrizione chiara.
- Topic suggeriti: thunderbird, thunderbird-extension, fatturapa, fattura-elettronica, pec, xml, p7m.
- Abilita Issues e Private vulnerability reporting.
- Aggiungi screenshot reali ottenuti con dati sintetici.
- Dopo il catalogo ATN, metti il link di installazione del catalogo in evidenza nel README.
- Presenta il progetto sul tuo sito con un pulsante di download verso la release o ATN.

