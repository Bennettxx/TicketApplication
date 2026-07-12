# Ticket-Application – Entwickler-Wiki

Vollständige Dokumentation der Anwendung im Wiki-Stil: pro Klasse/Datei wird
beschrieben, welcher Teil welche Aufgabe hat und **was genau dort geschieht**.
Die Kapitel 3–9 bilden den aktuellen Stand des Codes ab; Kapitel 11 enthält die
Entwicklungshistorie (was wann hinzugekommen ist).

## Inhalt

1. Überblick
2. Start & Einrichtung (Setup, Default-Admin, Konfiguration)
3. Schicht „Data"
4. Schicht „Models"
5. Schicht „Functions" (Validierung & Krypto)
6. Schicht „Services" (Config, Logging, Mail)
7. Schicht „DTOs"
8. Schicht „Controllers"
9. Frontend
10. Sicherheits- & Validierungskonzept
11. Entwicklungshistorie (Changelog)
12. Mögliche nächste Schritte / Merkliste

---

## 1. Überblick

Lokal laufendes Ticket-System.

- **Backend:** ASP.NET Core (.NET 10) Web-API mit Controllern, Entity Framework
  Core und SQL Server. Authentifizierung über JWT.
- **Frontend:** statische HTML-Seiten mit reinem JavaScript (kein Framework),
  ausgeliefert aus `wwwroot/`.
- **Datenbank:** wird über `EnsureCreated()` automatisch aus dem Code-Modell
  erzeugt (keine Migrationen).
- **Konsole:** Im **Release-Build** ist die App als `WinExe` konfiguriert –
  beim Start erscheint **kein Konsolenfenster**. Im Debug-Build bleibt die
  Konsole für die Entwicklung erhalten. (csproj: `OutputType` mit Condition.)

### Rollen

| Rolle     | Rechte |
|-----------|--------|
| `User`    | **Als Einzige Tickets erstellen** (eigene sehen, im Ticket chatten, Anhänge hochladen, Lösungsvorschläge erhalten, Problem melden). |
| `Support` | Alle Tickets sehen/bearbeiten, Kanban, Status/Zuweisung, Zeiterfassung, interne Notizen, Wissensdatenbank/Themen pflegen, Problem melden – aber **kein** Erstellen von Tickets. |
| `Admin`   | Alles wie Support **plus** Benutzerverwaltung, Statistik, Problemmeldungs-Verwaltung und **Systemeinstellungen** (SMTP, DB-Verbindung, Log-Pfad). „Problem melden" ist für Admins ausgeblendet. |

### Projektstruktur

```
/Data          Enums, DbContext, DbInitializer
/Models        Datenbank-Entitäten
/Functions     Validierungs-Attribute + Datei-Krypto
/Services      AppConfig (geschützte Datei), Logging, Mail (SMTP + Queue)
/DTOs          Ein-/Ausgangs-Datenobjekte (Eintrittstore)
/Controllers   API-Endpunkte
/wwwroot       Frontend (HTML/JS/CSS)
Program.cs     Startup/Konfiguration/Middleware
```

---

## 2. Start & Einrichtung

### 2.1 Erststart / Setup-Modus

Findet die App beim Start **keine Datenbank-Konfiguration** (weder geschützte
Config-Datei noch `ConnectionStrings:DefaultConnection` aus
appsettings/Umgebungsvariablen), läuft sie im **Setup-Modus**: Eine Middleware
leitet alle Aufrufe auf `setup.html` um; nur `/api/setup/*` ist erreichbar.

Auf der Setup-Seite werden eingegeben und geprüft:

- **SQL-Server**, **Datenbankname**, Anmeldeart (**Windows-Anmeldung** oder
  **SQL-Benutzer** mit User/Passwort) – mit Button „Verbindung testen".
  Existiert die DB noch nicht, wird gegen `master` getestet und die DB beim
  Abschluss automatisch angelegt.
- **Log-Verzeichnis** (leer = Ordner `Logs` neben der Anwendung); wird per
  Probeschreiben geprüft.

Beim Abschluss („Speichern & Initialisieren"): Verbindung testen → DB per
`EnsureCreated` anlegen → Startdaten einspielen → Konfiguration **verschlüsselt
speichern**. Danach zeigt die Seite die Zugangsdaten des Standard-Admins an.

### 2.2 Standard-Admin & Passwort-Zwangswechsel

Existiert kein aktiver Admin, legt der `DbInitializer` an:

| E-Mail              | Passwort | Besonderheit |
|---------------------|----------|--------------|
| `admin@ticket.local` | `admin` | `MustChangePassword = true` |

Beim ersten Login wird der Admin auf `profil.html?pwzwang=1` geleitet. Solange
das Flag gesetzt ist, blockt eine **Middleware alle API-Aufrufe** außer
Login/Profil-lesen/Passwort-ändern (HTTP 403). Nach dem Passwortwechsel ist die
Anwendung sofort normal nutzbar.

### 2.3 Konfigurationsdatei `appconfig.protected`

- Liegt neben der Anwendung (ContentRoot), enthält DB-Verbindung + Log-Pfad
  als JSON.
- Wird komplett mit **Windows-DPAPI (LocalMachine-Scope)** verschlüsselt –
  nur auf **diesem Rechner** entschlüsselbar; von außen (anderer Rechner,
  Kopie der Datei) unbrauchbar. Zusätzlich sollten die NTFS-Rechte des
  App-Ordners restriktiv gesetzt sein.
- Später über **Verwaltung → Einstellungen** (Admin) änderbar: DB-Verbindung
  (mit Verbindungstest) und Log-Pfad. Die Datei hat Vorrang vor
  appsettings/Umgebungsvariablen.

### 2.4 Entwicklungsmodus (wie bisher)

- `appsettings.Local.json` (in `.gitignore`) kann weiterhin den
  Connection-String enthalten → dann startet die App direkt ohne Setup
  („Legacy-Fallback", wird auf der Einstellungen-Seite angezeigt).
- **JWT-Key** und **Anhang-Verschlüsselungsschlüssel** werden im
  Development-Modus beim ersten Start automatisch erzeugt und in
  `appsettings.Local.json` gespeichert. In Produktion kommen beide aus
  Umgebungsvariablen (`Jwt__Key`, `Attachments__Key`, siehe
  `ANLEITUNG_UMGEBUNGSVARIABLEN.txt`) – fehlt einer, startet die App nicht.
- Öffentliche Datei-Anhänge liegen unter `AttachmentStorage/`.
- **Test-Logins** (nur im Development-Modus geseedet, Passwort `Password`):
  `admin@user.com` (Admin), `support@user.com` (Support), `user@user.com` (User).

> **WICHTIG – Datenbank neu erstellen:** Das Datenmodell wird über
> `EnsureCreated()` angelegt; Schema-Änderungen an einer **bestehenden** DB
> werden NICHT automatisch eingespielt. Nach Modelländerungen die lokale
> Datenbank einmal löschen.

### 2.5 Logging

- Eigener, schlanker Datei-Logger (`LogService`), **kein** externes Framework.
- Ablage: pro **Funktionsbereich und Tag** eine Datei im konfigurierten
  Log-Verzeichnis, z. B. `auth-2026-07-11.log`.
- Bereiche: `app` (Start/Setup), `auth` (Login/Registrierung/Passwörter),
  `benutzer` (Verwaltung), `tickets` (Ticket-Aktionen), `mail` (Versand),
  `einstellungen` (Settings-Änderungen), `fehler` (unbehandelte Exceptions).
- Zeilenformat: `2026-07-11 10:00:00 [INFO] Text`.
- **Bereinigung:** Ein Hintergrunddienst löscht beim Start und danach täglich
  alle Log-Dateien, die älter als **60 Tage** sind.
- Log-Pfad: beim Setup wählbar, später unter Einstellungen änderbar.

### 2.6 E-Mail / SMTP

- Einstellungen (Host, Port, SSL, Benutzer, Passwort, Absender) pflegt der
  Admin unter **Verwaltung → Einstellungen**; Ablage in der DB-Tabelle
  `AppSettings`, das Passwort DPAPI-verschlüsselt.
- **„Testmail senden"** schickt eine Mail **an die hinterlegte
  Absenderadresse** und zeigt Erfolg bzw. die konkrete Fehlermeldung an.
- Versand läuft über eine **In-Memory-Queue + Hintergrunddienst** (MailKit),
  damit kein Request auf den Mailserver wartet. Ergebnis jedes Versands steht
  im `mail`-Log.
- **Anwendungsfälle:**
  - *Registrierung:* Ist SMTP aktiv, erhält der neue User eine
    Bestätigungsmail mit Link (`/api/auth/confirm?token=...`). Login ist erst
    nach **Mailbestätigung UND Admin-Freischaltung** möglich. Ist SMTP nicht
    konfiguriert, entfällt die Mailbestätigung automatisch.
  - *Ticket-Antwort:* Neue (nicht-interne) Chat-Nachricht → Mail an Ersteller
    und zugewiesenen Bearbeiter (außer dem Autor selbst), mit Link zum Ticket.
  - *Wiedereröffnung:* ebenfalls Mail an die Beteiligten inkl. Begründung.

---

## 3. Schicht „Data" (`/Data`)

### 3.1 `UserRole` (enum)
`Admin = 0`, `Support = 1`, `User = 2`. Wert steht als Rollen-Claim im JWT.

### 3.2 `TicketStatus` (enum)
Kanban-Spalten: `Open = 0`, `InProgress = 1`, `Closed = 2`. Zahlenwerte sind im
Frontend fest verdrahtet.

### 3.3 `TicketPriority` (enum)
`Low = 0`, `Medium = 1`, `High = 2`. Steuert Farbe/Sortierung im Frontend.

### 3.4 `ApplicationDbContext`
- **DbSets:** `Users`, `Tickets`, `TicketDialogue`, `TicketTransactions`,
  `TicketTimeEntries`, `TicketAttachments`, `KnowledgeArticles`, `Problems`,
  `TicketReads`, `Departments`, `Subjects`, **`AppSettings`**.
- **`OnModelCreating`:** zusammengesetzter Schlüssel bei `TicketTransaction`
  (`{TicketId, TransactionId}`); eigene PKs + FK/Index bei Dialogue/TimeEntry/
  Attachments; Unique-Index `(TicketId, UserId)` bei `TicketRead`;
  `AppSetting.Key` als PK.

### 3.5 `DbInitializer`
`Initialize(context, isDevelopment)`:
- `EnsureCreated()`.
- Abteilungen `IT Support`, `Einkauf`, `Verkauf` (falls leer).
- **Nur Development:** drei Test-User (siehe 2.4).
- **Default-Admin** `admin@ticket.local`/`admin` mit
  `MustChangePassword = true`, falls kein aktiver Admin existiert.
- Drei Beispiel-Wissensartikel (falls leer).

---

## 4. Schicht „Models" (`/Models`)

### 4.1 `User`
- `Id`, `FirstName`, `SecondName`, `Email`, `PasswordHash` (BCrypt).
- `Role` (`UserRole`), `DepartmentId?` (nur Rolle User).
- `IsActivated` – Freischaltung durch Admin nach Registrierung.
- `IsActive` – Soft-Delete/Sperre; User werden nie physisch gelöscht.
- `MustChangePassword` – erzwingt Passwortwechsel beim nächsten Login.
- `EmailConfirmed` / `EmailConfirmToken` – Mailbestätigung bei Registrierung
  (ohne SMTP-Konfiguration automatisch bestätigt).

### 4.2 `Ticket`
- `Id` (PK), `CreatedByUserId` (aus JWT), `AssignedToId?` (nur Staff).
- Fixer Inhalt: `Priority`, `Title`, `Description`, `ExpectedResult`,
  `ActualResult`, `AgreedBilling`, `AgreedAGB`.
- **Referenz:** `ReferenceTicketId?` + `ReferenceComment` – optionaler Verweis
  auf ein anderes Ticket (beim Erstellen angebbar, Existenz wird validiert).
- Änderbar: `Status`, `AdditionalUserId1..3` (Zusatzkontakte), `DepartmentId`,
  `SubjectId`.
- Zeitstempel: `CreatedAt`, `UpdatedAt`, `ClosedAt?`, `OpenedAt?`.

### 4.3 `TicketDialogue`
Chat-Nachricht: `Id`, `TicketId`, `AuthorUserId`, `Text`, `IsInternal`
(nur Staff sichtbar/setzbar), `CreatedAt`.

### 4.4 `TicketTimeEntry`
Zeiteintrag: `Id`, `TicketId`, `UserId` (Erfasser), `Minutes`, `Note`,
`WorkedAt` (Arbeitstag), `CreatedAt`. Basis der Statistik.

### 4.5 `TicketTransaction`
Audit-Snapshot pro Änderung, Schlüssel `{TicketId, TransactionId}` (fortlaufend
pro Ticket), `ResponsibleUserId` + Snapshot der änderbaren Felder.

### 4.6 `TicketAttachments`
Datei-Anhang; `ContainsPrivateData = true` → AES-GCM-verschlüsselt in der DB
(`DatenBase64`), sonst Datei unter `AttachmentStorage/{ticketId}/`
(`DirectoryPath`). Metadaten: `DataName`, `ContentType`, `FileSize`,
`UploadedByUserId`, `CreatedAt`.

### 4.7 `KnowledgeArticle`
Wissensartikel: `Title`, `Keywords`, `Solution`, `DepartmentId?`,
`IsPublished`, `CreatedByUserId`, Zeitstempel.

### 4.8 `Department`, `Subject`
`Department`: Id, Name. `Subject`: Id, Title, DepartmentId, `IsVerified`
(neue Themen entstehen unverifiziert beim Ticket-Erstellen).

### 4.9 `Problem`
Problemmeldung (abgespecktes Ticket): Titel, Beschreibung, Priorität, Status,
`ContactEmail` (Pflicht), `CreatedByUserId?` (null = anonym), Zeitstempel.

### 4.10 `TicketRead`
Letzter Lesezeitpunkt pro `(TicketId, UserId)` – Basis der
„Neue Antwort"-Kennzeichnung.

### 4.11 `AppSetting`
Key-Value-Tabelle für Laufzeit-Einstellungen (derzeit SMTP: `Smtp.Host`,
`Smtp.Port`, `Smtp.UseSsl`, `Smtp.User`, `Smtp.Password` (verschlüsselt),
`Smtp.From`, `Smtp.FromName`, `Smtp.Enabled`).

---

## 5. Schicht „Functions" (`/Functions`)

### 5.1 `ExistsInColumnAttribute`
Prüft generisch (Expression-Tree), ob ein Wert in einer Tabelle/Spalte
existiert (E-Mail, Abteilungsname, Ticket-Id …). `null`/leer gilt als „nicht
gesetzt" – Pflicht regelt `[Required]`.

### 5.2 `RequiresFieldAttribute`
Bedingte Pflicht: Feld nur erlaubt, wenn ein anderes Feld gesetzt ist
(Zusatzkontakt 2 nur mit Kontakt 1).

### 5.3 `NotInFutureAttribute`
Datum darf nicht in der Zukunft liegen (1 Tag Toleranz); Arbeitsdatum der
Zeiterfassung.

### 5.4 `FileCrypto`
AES-GCM für private Anhänge. Format: Base64 von `nonce(12)|tag(16)|ciphertext`.
Key (32 Byte) aus `Attachments:Key`.

---

## 6. Schicht „Services" (`/Services`)

### 6.1 `SecretProtector`
Statischer DPAPI-Wrapper (LocalMachine-Scope, feste Entropie): `Protect`/
`Unprotect` für Strings und Bytes. Verwendet für die Config-Datei und das
SMTP-Passwort.

### 6.2 `AppConfigService` (Singleton)
- Lädt/schreibt `appconfig.protected` (DB-Verbindung + Log-Pfad, DPAPI).
- `IsConfigured` (Datei ODER Fallback-Connection-String vorhanden),
  `IsLegacyFallback` (nur Fallback aktiv).
- `GetConnectionString()` – Datei hat Vorrang; Aufbau über
  `SqlConnectionStringBuilder` (kein String-Zusammenbau).
- `LogPath` – konfiguriert oder Default `<App>\Logs`.
- `TestConnection(db)` – echter Connect; wenn die Ziel-DB fehlt, Test gegen
  `master` (DB wird dann beim Setup angelegt).
- `SaveDb` / `SaveLogPath` – wirken sofort (DbContext liest den
  Connection-String pro Request neu).

### 6.3 `LogService` (Singleton) + `LogCleanupService` (BackgroundService)
Siehe 2.5. `Info/Warn/Error(bereich, text)`; `Cleanup(60)` löscht alte Dateien;
der Hintergrunddienst ruft das täglich auf. Logging wirft nie Exceptions.

### 6.4 `MailService` (Scoped), `MailQueue` (Singleton), `MailDispatcherService`
- `MailService`: liest/schreibt SMTP-Settings (`AppSettings`-Tabelle),
  `SendAsync(to, subject, body)` über MailKit (`UseSsl` = SSL direkt/465,
  sonst STARTTLS), `IsConfiguredAsync()`.
- `MailQueue`: unbounded Channel; Controller enqueuen, blockieren nie.
- `MailDispatcherService`: liest die Queue, versendet mit eigenem DI-Scope,
  loggt Erfolg/Fehler ins `mail`-Log.

---

## 7. Schicht „DTOs" (`/DTOs`) – Eintrittstore

DTOs sind die **einzigen** Eintrittspunkte für Client-Daten; `[ApiController]`
lehnt Verstöße automatisch mit `400` ab.

### 7.1 Eingangs-DTOs (Auswahl, Änderungen gegenüber Kapitel-Stand vorher)

| DTO | Zweck | Wichtige Prüfungen |
|-----|-------|--------------------|
| `CreateTicketDto` | Ticket anlegen | wie gehabt **plus** `ReferenceTicketId?` (muss existieren) + `ReferenceComment?` (≤500). |
| `UpdateTicketStatusDto` | Statuswechsel | nur noch `Status` (Ticket-Id kommt aus der URL). |
| `SetupDbDto` | Setup/DB-Einstellungen | Server + Datenbank Pflicht; `UseWindowsAuth` oder User/Passwort; optional `LogPath`. |
| `SmtpSettingsDto` | SMTP-Einstellungen | Port 1–65535, Absender-E-Mail-Format; `Password` write-only (`null` = behalten, leer = löschen). |
| `LogPathDto` | Log-Pfad ändern | Pfad Pflicht (Schreibtest im Controller). |

Alle übrigen Eingangs-DTOs (Login, Register, User, Profil, Passwort, Problem,
Dialog, Zeit, Wissen, Zuweisung, Reopen) unverändert wie zuvor dokumentiert.

### 7.2 Ausgangs-DTOs (Änderungen)

- `UserResponseDto`: zusätzlich `IsActive` (Sperr-Status) und `EmailConfirmed`.
- `TicketResponseDto`: zusätzlich `ReferenceTicketId?` + `ReferenceComment`.
- `SetupResultDto`: Meldung + Zugangsdaten des Standard-Admins.
- `SystemSettingsDto`: Log-Pfad + DB-Verbindung (ohne Passwort) + Legacy-Flag.
- `AgentStatsDto`/`CustomerStatsDto`/`DepartmentStatsDto`, `DashboardDto`,
  `KnowledgeArticleDto` etc. unverändert.

---

## 8. Schicht „Controllers" (`/Controllers`)

Identität/Rolle kommen **immer aus dem JWT**. Helfer: `CurrentUserId`, `IsStaff`.

### 8.0 `Program.cs` (Startup & Middleware)
Reihenfolge der Pipeline:
1. **Fehler-Middleware** – loggt unbehandelte Exceptions ins `fehler`-Log.
2. HTTPS-Redirect.
3. **Setup-Middleware** – ohne DB-Konfiguration Redirect auf `setup.html`
   (nur `/api/setup/*` + statische Assets erlaubt); ist konfiguriert, wird
   `setup.html` auf den Login umgeleitet.
4. Statische Dateien (im Dev ohne Cache), CORS, Authentication,
   (Dev) `Dev-Admin`-Header-Bypass.
5. **Passwort-Zwangswechsel-Middleware** – blockt bei gesetztem
   `MustChangePassword` alle `/api/*` außer Auth/Profil/Passwort (403).
6. Authorization, Controller.

Services-Registrierung: `AppConfigService`, `LogService`, `MailQueue`
(Singletons), `MailService` (scoped), `MailDispatcherService` +
`LogCleanupService` (HostedServices). Der **DbContext** bezieht den
Connection-String pro Scope aus dem `AppConfigService` (SQL-Logging nur im
Dev-Modus). DB-Initialisierung beim Start nur, wenn konfiguriert.

### 8.1 `SetupController` (`api/setup`, anonym, nur im Setup-Modus)
- `GET status` – ist die App konfiguriert?
- `POST test` – Verbindungstest (Ziel-DB, sonst `master`).
- `POST complete` – Log-Pfad-Schreibtest, DB anlegen + seeden, Config
  verschlüsselt speichern; Antwort enthält die Standard-Admin-Zugangsdaten.
  Ist die App bereits konfiguriert, liefern alle Endpunkte 403.

### 8.2 `SettingsController` (`api/settings`, nur Admin)
- `GET/PUT smtp` – SMTP-Einstellungen (Passwort write-only).
- `POST smtp/test` – **Testmail an die Absenderadresse**, Antwort mit
  Erfolg/konkreter Fehlermeldung.
- `GET system` – Log-Pfad + DB-Verbindung (ohne Passwort) + Legacy-Hinweis.
- `PUT logpath` – Log-Verzeichnis ändern (Schreibtest).
- `POST database/test` / `PUT database` – DB-Verbindung testen/ändern
  (Speichern nur nach erfolgreichem Test; gilt sofort für neue Requests).
- Alle Änderungen werden ins `einstellungen`-Log geschrieben.

### 8.3 `AuthController` (`api/auth`)
- `POST login` – Reihenfolge: Passwort → `IsActive` → `EmailConfirmed` →
  `IsActivated`; Antwort `{ token, mustChangePassword }`. Erfolg/Fehlversuche
  landen im `auth`-Log.
- `POST register` – legt User an (`IsActivated=false`); ist SMTP aktiv,
  zusätzlich `EmailConfirmed=false` + Bestätigungsmail mit Token-Link.
- `GET confirm?token=` – bestätigt die E-Mail, Redirect auf
  `/index.html?confirmed=1`.

### 8.4 `AccountController` (`api/account`)
- `GET me` / `PUT me` (Profil; E-Mail nicht änderbar; Abteilung nur Rolle User).
- `PUT me/password` – prüft altes Passwort, setzt `MustChangePassword=false`.

### 8.5 `UserController` (`api/user`)
- `GET me`; `GET /` (Admin/Support) – **alle** User inkl. gesperrter
  (`IsActive` im DTO); `GET {id}`; `POST` (Admin); `PUT {id}` (Admin,
  Teilupdate inkl. `IsActive`/`IsActivated`); `DELETE {id}` (Admin,
  Soft-Delete); `GET pending` (inkl. `EmailConfirmed`-Hinweis);
  `POST {id}/approve`. Verwaltungsaktionen loggen nach `benutzer`.
- **Sperr-Logik:** Sperren = `IsActive=false` (Soft-Delete), Entsperren =
  `IsActive=true`. `IsActivated` betrifft nur die Registrierungs-Freischaltung.

### 8.6 `TicketController` (`api/ticket`)
Wie zuvor (Create nur Rolle User; Liste mit Filtern `q`, `status`,
`activeOnly`, `priority`, `departmentId`, `assignedTo`, `createdFrom/To`;
Detail; PATCH; Status mit Reopen-Sperre; Reopen mit Pflicht-Nachricht; Assign;
Read-Tracking). Neu:
- `Create` übernimmt `ReferenceTicketId`/`ReferenceComment`.
- Ticket-Aktionen (Erstellen, Statuswechsel, Zuweisung, Reopen) loggen nach
  `tickets`; Reopen benachrichtigt Beteiligte per Mail.

### 8.7 `DialogueController` (`api/ticket/{id}/dialogue`)
Wie zuvor (Sichtbarkeit interner Notizen, Schreibschutz geschlossener Tickets
für User). Neu: nicht-interne Nachrichten lösen **Mail an Ersteller +
Bearbeiter** aus (außer Autor), sofern SMTP aktiv.

### 8.8 `TimeEntryController`, `AttachmentController`, `KnowledgeController`,
`SubjectController`, `ProblemController`, `MetadataController`,
`DashboardController` – unverändert wie zuvor dokumentiert.

### 8.9 `StatisticsController` (`api/statistics`, nur Admin)
Alle drei Endpunkte (`agents`, `customers`, `departments`) akzeptieren jetzt
optional **`?days=30|60`** (leer = Gesamt): Tickets werden nach
**Erstelldatum**, Zeiteinträge nach **Arbeitsdatum** im Zeitraum gefiltert
(Zeiten auf älteren Tickets zählen mit).

---

## 9. Frontend (`/wwwroot`)

### 9.1 `app.js`
Wie zuvor (Token, `api()`-Wrapper, `getMich()`, Sidebar, `esc()`,
Status/Prio-Tabellen). Sidebar-Änderungen:
- „Problem melden" nur für **User und Support** (nicht Admin).
- Verwaltung (Admin) zusätzlich: **Einstellungen** (`einstellungen.html`).

### 9.2 Seiten

| Datei | Zugang | Zweck |
|-------|--------|-------|
| `setup.html` | öffentlich (nur Setup-Modus) | Ersteinrichtung: DB-Verbindung (+Test), Log-Pfad; zeigt danach Standard-Admin-Zugangsdaten. |
| `index.html` | öffentlich | Login; leitet bei `mustChangePassword` auf das Profil; zeigt Bestätigungs-Hinweis nach `?confirmed=1`. |
| `register.html` | öffentlich | Selbst-Registrierung (Abteilung Pflicht). |
| `einstellungen.html` | Admin | SMTP (inkl. **Testmail an Absenderadresse**), Log-Verzeichnis, DB-Verbindung (Test + Speichern). |
| `dashboard.html` | alle | Kennzahlen + letzte Tickets. |
| `startseite.html` | alle | Offene Tickets mit Suche/Filter, „Neue Antwort"-Badge. |
| `verlauf.html` | alle | Geschlossene Tickets. |
| `TicketErstellen.html` | nur User | 5-Schritt-Assistent; **Zusatzkontakte werden mitgesendet**, **Anhänge werden nach dem Erstellen automatisch hochgeladen**, **Referenz (Ticketnummer + Kommentar)**; beim Absenden **Sammelmeldung aller fehlenden Pflichtfelder inkl. Schritt-Angabe**. |
| `problemMelden.html` | öffentlich + eingeloggt | Standalone-Problemformular. |
| `probleme.html` | Admin | Problemmeldungen; **Löschen-Button in eigener Spalte ganz rechts**. |
| `themen.html` | Staff | Themen verifizieren/löschen. |
| `ticket.html` | alle | Detail: Chat, Anhänge, Referenz-Anzeige (Link), Staff: Status/Zuweisung/Zeit; Reopen mit Pflicht-Nachricht. |
| `kanban.html` | Staff | Drag&Drop-Board; Zeitraum Gesamt/60/30 Tage (nach letztem Update). |
| `statistik.html` | Admin | Diagramme + Tabellen; **Zeitraum-Umschalter Gesamt / 60 / 30 Tage** (Server-seitig via `?days=`). |
| `benutzer.html` | Admin | Benutzerverwaltung; Status aktiv/nicht freigeschaltet/gesperrt; Pending-Liste zeigt „E-Mail noch nicht bestätigt". |
| `profil.html` | alle | Profil + Passwort; Zwangswechsel-Hinweis (`?pwzwang=1`), danach Weiterleitung. |
| `wissen.html` | Staff | Wissensartikel pflegen. |

---

## 10. Sicherheits- & Validierungskonzept

- **DTOs als einziges Eintrittstor** (DataAnnotations → automatisch 400).
- **Identität aus dem JWT**, nie aus dem Request-Body.
- **Rollenautorisierung** auf Controller-/Endpunkt-Ebene + feingranulare Checks.
- **Geschützte Konfiguration:** DB-Zugangsdaten + Log-Pfad DPAPI-verschlüsselt
  (LocalMachine) in `appconfig.protected`; SMTP-Passwort DPAPI-verschlüsselt in
  der DB. Setup-Endpunkte nur im unkonfigurierten Zustand erreichbar.
- **Passwort-Zwangswechsel** serverseitig per Middleware erzwungen.
- **Mailbestätigung** (falls SMTP aktiv) zusätzlich zur Admin-Freischaltung.
- **Verschlüsselung privater Anhänge** (AES-GCM).
- **Soft-Delete** für Benutzer; Sperren = `IsActive=false`.
- **XSS-Schutz** im Frontend über `esc()`.
- **Logging** sicherheitsrelevanter Ereignisse (Logins, Fehlversuche,
  Verwaltungs- und Einstellungs-Änderungen).

---

## 11. Entwicklungshistorie (Changelog)

### Ausbau 1 – Bearbeitung, Chat, Zeit, Statistik
- Kanban-Status `InProgress`; `TicketDialogue` + `DialogueController` (Chat);
  `TicketTimeEntry` + `TimeEntryController`; `StatisticsController`,
  `MetadataController`, Zuweisung, angereicherte Ticket-Antworten.
- Frontend: `kanban.html`, `ticket.html`, `statistik.html`, `startseite.html`,
  gemeinsame `app.js`/`style.css`.
- Diverse Fehler der Grundlage behoben (ExistsInColumn/null, [Required] auf
  optionalen Feldern, fehlendes `GET /api/user/me`, Login-Fehlermeldung u. a.).

### Ausbau 2 – Konto, Verwaltung, Navigation
- Registrierung vervollständigt, `register.html`, `profil.html`,
  `benutzer.html`; Kontaktdaten im Ticket automatisch + readonly;
  Dropdown-Sidebar.

### Ausbau 3 – Anhänge, Dashboard, Suche, Wissensdatenbank
- `AttachmentController` (privat verschlüsselt/öffentlich als Datei),
  `FileCrypto`; `DashboardController` + `dashboard.html`; Suche/Filter;
  `KnowledgeController` + `wissen.html` + Live-Vorschläge.

### Ausbau 4 – Navigations- & Rechte-Feinschliff
- „Startseite" als Top-Link; Ticket-Erstellung auf Rolle `User` beschränkt.

### Ausbau 5 – Abteilungszugehörigkeit & Abteilungs-Statistik
- Abteilung pro User (Pflicht bei Registrierung), Statistik nach Abteilung.

### Ausbau 6 – Feinschliff, Problemmeldungen, Benachrichtigungen
- Login-Optik, Ticket-Erstellen-Fixes, Themen (Subjects) + Verifizierung,
  Problemmeldungen (`Problem*`), Chart.js-Diagramme, `TicketRead`-Tracking.

### Ausbau 7 – Wiedereröffnen & Verlauf
- Reopen mit Pflicht-Nachricht, `verlauf.html`, Startseite nur offene Tickets,
  Kanban-Zeitraum-Modus, Datumsfilter.

### Ausbau 8 – Review, Bugfixes & Referenz-Feature
- **Kompletter Code-Review** mit Fehlerbereinigung:
  - JS-Syntaxfehler in `benutzer.html` (verwaistes `catch`) – Seite war tot.
  - Doppeltes `let MICH` in `themen.html` (Konflikt mit `app.js`).
  - Login-Problem bei wiederverwendeten E-Mails (aktiver User wird bevorzugt).
  - Sperr-Logik auf `IsActive` umgestellt (gesperrte User tauchten unter
    „Offene Registrierungen" auf); Benutzerliste zeigt auch gesperrte User.
  - Zusatzkontakte und Anhänge beim Ticket-Erstellen wurden nie gesendet –
    jetzt real angebunden (Anhänge werden nach dem Create hochgeladen).
  - Whitespace-Vorbelegung der Textareas, Tippfehler, `DateTime.UtcNow` fürs
    JWT, SQL-Logging nur noch im Dev-Modus, tote Felder/Spalten entfernt
    (`Ticket.TicketId`, `UpdateTicketDto.UpdatedAt`, Status-DTO-TicketId).
- **Referenz-Feature:** `ReferenceTicketId` + `ReferenceComment` am Ticket
  (Schritt 4 im Assistenten, Anzeige + Link in der Detailansicht).
- **Alle Kommentare** im ausführbaren Code entfernt und neu geschrieben
  (deutsch, stichwortartig).

### Ausbau 9 – Setup, Sicherheit, Mail, Logging (aktueller Stand)
- **Erststart-Setup** (`setup.html` + `SetupController`): DB-Verbindung mit
  Test, Log-Pfad; Config DPAPI-verschlüsselt in `appconfig.protected`;
  Setup-Middleware.
- **Standard-Admin** `admin@ticket.local`/`admin` mit erzwungenem
  Passwortwechsel (Middleware + Login-Flow + Profil-Hinweis).
- **SMTP/Mail:** Einstellungen-Seite (Admin) mit „Testmail senden" (an die
  Absenderadresse); MailKit + Queue + Hintergrundversand;
  **Mailbestätigung bei Registrierung** (zusätzlich zur Admin-Freischaltung,
  entfällt automatisch ohne SMTP); **Benachrichtigung bei Ticket-Antwort und
  Wiedereröffnung** an Ersteller/Bearbeiter.
- **Logging:** `LogService` mit Bereichs-Dateien pro Tag, Log-Pfad beim Setup
  wählbar und per Einstellungen änderbar, automatische Bereinigung >60 Tage;
  Logging in Auth-, Benutzer-, Ticket-, Mail- und Settings-Aktionen sowie
  globale Fehler-Middleware.
- **DB-Verbindung + Log-Pfad zur Laufzeit änderbar** (Einstellungen, Admin;
  Speichern nur nach erfolgreichem Verbindungstest).
- **Konsole aus im Release-Build** (`OutputType=WinExe` per Condition).
- „Problem melden" für Admins ausgeblendet.
- **Statistik-Zeitraum** Gesamt/60/30 Tage (Server-Filter `?days=`).
- Problemmeldungen: Löschen-Button in eigener Spalte ganz rechts.
- Ticket-Erstellen: **Sammelmeldung** aller fehlenden Pflichtfelder mit
  Schritt-Angabe beim Absenden.
- Neue Pakete: `MailKit`, `System.Security.Cryptography.ProtectedData`.
- **Schema-Änderungen** (lokale DB einmal löschen!): `User.MustChangePassword`,
  `User.EmailConfirmed`, `User.EmailConfirmToken`, Tabelle `AppSettings`.

---

## 12. Mögliche nächste Schritte / Merkliste

- **Windows-Dienst:** App als Dienst betreiben (`UseWindowsService` +
  `sc create`), sobald es Richtung produktivem Dauerbetrieb geht.
  *(Vom Auftraggeber ausdrücklich „im Hinterkopf behalten".)*
- EF-Core-**Migrationen** statt `EnsureCreated()` (Schema-Updates ohne
  DB-Löschen).
- Lösungsvorschläge auch auf Basis der **Beschreibung** statt nur des Titels.
- Pagination für große Ticketmengen; SLA/Fälligkeitsdaten.
- Passwort-Reset per Mail („Passwort vergessen" auf der Login-Seite).
- HTTPS-Zertifikat/Port-Konfiguration für den Produktivbetrieb dokumentieren.
