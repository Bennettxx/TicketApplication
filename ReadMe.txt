appsettings.json:
	AllowedHosts = Welche Dömeins dürfen anfragen
	ConnectionString = Datenbankverbindung
	API-Keys = Schlüssel für externe Dienste

EntityFrameworkCore (Zusammenarbeit aus core.SqlServer & core.Tools):
	Bibliothek, die zwischen SQL DB und C# übersetzt
	

Code	Bedeutung
{ get; set; }	Offen: Jeder darf lesen und schreiben.
{ get; private set; }	Gesperrt: Jeder liest, nur die Klasse schreibt.
{ get; init; }	Einmalig: Schreiben nur beim Erzeugen erlaubt.
{ get { ... } set { ... } }	Kontrolliert: Du prüfst den Wert mit einem if bevor er gespeichert wird.
{ get; }	Konstant: Der Wert kann nach der Erstellung nie wieder geändert werden.

GET Daten abrufen
POST Neue Ressource erstellen
PUT Ressource komplett ersetzen/überschreiben (was nicht mitgesendet wird wird null)
PATCH Ressource teilweise ändern (nicht mitgesendete Werte bleiben erhalten)
DELETE Ressource löschen
HEAD Wie GET, aber nur Header (kein Body)
OPTIONS Fragt ab, welche Methoden erlaubt sind