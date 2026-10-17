# Dati trattati dal prototipo Outlook classico

Il prototipo legge solo il messaggio selezionato, il mittente dichiarato e gli allegati necessari a trovare le fatture. Non carica fatture su server, non usa Graph e non richiede credenziali della casella.

Il filtro mittenti viene ricordato nel registro dell’utente corrente, sotto Software\Ghedy\e-InvoiceViewer\Outlook.

Gli allegati da leggere possono essere temporaneamente salvati in una sottocartella casuale di e-invoiceviewer-outlook nella cartella temporanea dell’utente. Al termine della lettura viene tentata la cancellazione dei file creati; file bloccati da Outlook o interruzioni del processo possono lasciare residui temporanei.

I PDF vengono salvati temporaneamente solo quando l’utente ne richiede l’apertura, e aperti con il programma PDF predefinito. Gli altri formati vengono soltanto salvati tramite la normale finestra di scelta destinazione. La chiusura del visualizzatore tenta di eliminare i PDF temporanei; un programma esterno può mantenerli bloccati.

L’anteprima usa WebView2 con profilo InPrivate, script e comunicazione con il sistema host disabilitati, risorse remote bloccate dal documento e navigazione limitata alla pagina locale generata. I dati tecnici del runtime risiedono in LocalAppData/e-InvoiceViewer/Outlook/WebView2. Microsoft Edge WebView2 è un componente Microsoft e segue anche le proprie condizioni e politiche del runtime.

La stampa utilizza la finestra del sistema di anteprima. Le copie stampate, i PDF salvati e i file scaricati restano sotto il controllo dell’utente.

Non vengono verificati firma digitale, certificazione PEC, validità fiscale o assenza di malware.

Dalla versione 0.1.3 il ponte Outlook passa gli allegati a un processo locale separato tramite file temporanei e un indice JSON nella stessa sottocartella casuale. L’indice contiene nomi degli allegati e risultato del filtro sul mittente, senza indirizzo del mittente. Il visualizzatore tenta di eliminare questi file appena terminata la lettura.

Il registro LocalAppData/e-InvoiceViewer/Outlook/Diagnostics/addin.log contiene soltanto orario UTC, versione, identificativo del processo, nomi fissi delle fasi tecniche e tipo/codice delle eccezioni. Non registra corpo, oggetto, indirizzi, nomi degli allegati o contenuto delle fatture. La dimensione viene limitata mediante azzeramento oltre circa 256 KiB. Può essere consultato e copiato con **Mostra diagnostica** in OutlookSetup.exe; non viene inviato automaticamente.
