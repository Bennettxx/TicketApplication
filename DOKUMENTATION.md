# Ticket-Application – Entwickler-Wiki

Vollständige Dokumentation der Anwendung im Wiki-Stil: pro Klasse/Datei wird
beschrieben, welcher Teil welche Aufgabe hat und **was genau dort geschieht**.
Die Kapitel 3–8 bilden den aktuellen Stand des Codes ab; Kapitel 10 enthält die
Entwicklungshistorie (was wann hinzugekommen ist).

## Inhalt

1. Überblick
2. Start & Einrichtung
3. Schicht „Data"
4. Schicht „Models"
5. Schicht „Functions" (Validierung & Krypto)
6. Schicht „DTOs"
7. Schicht „Controllers"
8. Frontend
9. Sicherheits- & Validierungskonzept
10. Entwicklungshistorie (Changelog)
11. Mögliche nächste Schritte

---

## 1. Überblick

Lokal laufendes Ticket-System.

- **Backend:** ASP.NET Core (.NET 10) Web-API mit Controllern, Entity Framework
  Core und SQL Server. Authentifizierung über JWT.
- **Frontend:** statische HTML-Seiten mit reinem JavaScript (kein Framework),
  ausgeliefert aus `wwwroot/`.
- **Datenbank:** wird über `EnsureCreated()` automatisch aus dem Code-Modell
  erzeugt (keine Migrationen).

### Rollen

| Rolle     | Rechte |
|-----------|--------|
| `User`    | **Als Einzige Tickets erstellen** (eigene sehen, im Ticket chatten, Anhänge hochladen, Lösungsvorschläge erhalten). |
| `Support` | Alle Tickets sehen/bearbeiten, Kanban, Status/Zuweisung, Zeiterfassung, interne Notizen, Wissensdatenbank pflegen – aber **kein** Erstellen von Tickets. |
| `Admin`   | Alles wie Support **plus** Benutzerverwaltung und Statistik – ebenfalls **kein** Erstellen von Tickets. |

### Projektstruktur

```
/Data          Enums, DbContext, DbInitializer
/Models        Datenbank-Entitäten
/Functions     Validierungs-Attribute + Datei-Krypto
/DTOs          Ein-/Ausgangs-Datenobjekte (Eintrittstore)
/Controllers   API-Endpunkte
/wwwroot       Frontend (HTML/JS/CSS)
Program.cs     Startup/Konfiguration
```

---

## 2. Start & Einrichtung

> **WICHTIG – Datenbank neu erstellen:** Das Datenmodell wird über
> `EnsureCreated()` angelegt; Schema-Änderungen an einer **bestehenden** DB
> werden NICHT automatisch eingespielt. Nach Modelländerungen die lokale
> Datenbank einmal löschen, damit sie mit dem aktuellen Schema neu entsteht.

- `appsettings.Local.json` (steht in `.gitignore`) enthält den Connection-String
  und wird zusätzlich zu `appsettings.json` geladen.
- **JWT-Key** und **Anhang-Verschlüsselungsschlüssel** werden beim ersten Start
  automatisch erzeugt und in `appsettings.Local.json` gespeichert (siehe
  Program.cs, Kapitel 7.0).
- Öffentliche Datei-Anhänge liegen unter `AttachmentStorage/` (ebenfalls in
  `.gitignore`).

### Test-Logins (vom `DbInitializer` angelegt, Passwort jeweils `Password`)

| E-Mail              | Rolle   |
|---------------------|---------|
| `admin@user.com`    | Admin   |
| `support@user.com`  | Support |
| `kunde@user.com`    | User    |

---

## 3. Schicht „Data" (`/Data`)

### 3.1 `UserRole` (enum)
Definiert die Rollen `Admin = 0`, `Support = 1`, `User = 2`. Der Wert wird im
JWT als Rollen-Claim mitgegeben und steuert die Autorisierung.

### 3.2 `TicketStatus` (enum)
Bildet zugleich die Spalten des Kanban-Boards ab:
- `Open = 0` – neu/unbearbeitet.
- `InProgress = 1` – in Bearbeitung.
- `Closed = 2` – erledigt/geschlossen.

Die Zahlenwerte sind fest verdrahtet (Frontend `app.js` und Backend nutzen
dieselbe Codierung).

### 3.3 `TicketPriority` (enum)
`Low = 0`, `Medium = 1`, `High = 2`. Steuert Farbe/Sortierung im Frontend.

### 3.4 `ApplicationDbContext`
Bildet die Datenbank ab und stellt den Zugriff bereit.
- **DbSets:** `Users`, `Tickets`, `TicketDialogue`, `TicketTransactions`,
  `TicketTimeEntries`, `TicketAttachments`, `KnowledgeArticles`, `Departments`,
  `Subjects`.
- **`OnModelCreating`** legt Schlüssel/Beziehungen fest:
  - `TicketTransaction`: zusammengesetzter Schlüssel `{TicketId, TransactionId}`,
    FK auf `Ticket`.
  - `TicketDialogue`: eigener PK `Id`, FK über `TicketId`, Index auf `TicketId`.
  - `TicketTimeEntry`: eigener PK `Id`, FK über `TicketId`, Index auf `TicketId`.
  - `TicketAttachments`: eigener PK `Id`, FK über `TicketId`, Index auf `TicketId`.
  - `KnowledgeArticle`: eigener PK `Id`.

### 3.5 `DbInitializer`
`Initialize()` ruft `EnsureCreated()` auf und legt Default-Daten an, wenn die
jeweiligen Tabellen leer sind (Reihenfolge: **erst Abteilungen, dann Benutzer**,
damit die Benutzer einer Abteilung zugeordnet werden können):
- Drei Abteilungen: `IT Support`, `Einkauf`, `Verkauf`.
- Drei Benutzer (Admin/Support → IT Support, User „Karl Kunde" → Einkauf).
- Drei Beispiel-Wissensartikel (Passwort, Drucker, VPN) für die Lösungsvorschläge.

---

## 4. Schicht „Models" (`/Models`)

### 4.1 `User`
Repräsentiert einen Benutzer.
- `Id` – PK.
- `FirstName`, `SecondName`, `Email` – Stammdaten.
- `PasswordHash` – BCrypt-Hash; verlässt nie den Server (DTOs filtern ihn raus).
- `Role` – `UserRole`.
- `IsActivated` – Konto erst nach Admin-Freigabe nutzbar (Self-Registrierung).
- `DepartmentId?` – Abteilungszugehörigkeit. Wird bei der Registrierung gewählt
  (Pflicht für neue User) und dient als Kategorie in der Statistik
  (Bearbeitungszeit pro Abteilung).
- `IsActive` – Soft-Delete-Flag; Benutzer werden nie physisch gelöscht.

### 4.2 `Ticket`
Das zentrale Ticket-Objekt.
- `Id` – PK; `CreatedByUserId` – Ersteller (aus JWT gesetzt).
- `AssignedToId?` – zugewiesener Bearbeiter (nur Staff setzt das).
- Unveränderlicher Inhalt: `Priority`, `Title`, `Description`, `ExpectedResult`,
  `ActualResult`, `AgreedBilling`, `AgreedAGB`.
- Veränderlicher Inhalt: `Status`, `AdditionalUserId1..3` (Zusatzkontakte),
  `DepartmentId`, `SubjectId`.
- Zeitstempel: `CreatedAt`, `UpdatedAt`, `ClosedAt?`, `OpenedAt?` (automatisch
  durch die Status-Logik gepflegt).
- `TicketId` – historisches, derzeit ungenutztes Zusatzfeld (PK ist `Id`).

### 4.3 `TicketDialogue`
Eine einzelne Chat-Nachricht eines Tickets (Dialog-/Chatfunktion).
- `Id` – eigener PK; `TicketId` – Ticketbezug; `AuthorUserId` – Verfasser.
- `Text` – Nachrichtentext.
- `IsInternal` – interne Notiz: nur für Admin/Support sichtbar; für normale User
  serverseitig immer `false`.
- `CreatedAt` – Zeitpunkt.

### 4.4 `TicketTimeEntry`
Ein manuell erfasster Zeiteintrag (Zeiterfassung).
- `Id` – PK; `TicketId`; `UserId` – wer die Zeit erfasst hat (Bearbeiter).
- `Minutes` – Dauer in Minuten (> 0).
- `Note` – optionale Tätigkeitsbeschreibung.
- `WorkedAt` – Arbeitsdatum; `CreatedAt` – Erfassungszeitpunkt.
- Grundlage der Statistik-Auswertungen.

### 4.5 `TicketTransaction`
Audit-/Verlaufseintrag. Pro Ticketänderung wird ein Datensatz mit fortlaufender
`TransactionId` (pro Ticket) und dem damaligen Stand geschrieben.
- Schlüssel `{TicketId, TransactionId}`; `ResponsibleUserId` – wer die Änderung
  vornahm.
- Snapshot-Felder: `AssignedToId?`, `Status`, `AdditionalUserId1..3`,
  `DepartmentId`, `SubjectId`, `UpdatedAt`, `ClosedAt?`, `OpenedAt?`.
- Wird vom `TicketController` bei Erstellen/Update/Status/Assign gefüllt.

### 4.6 `TicketAttachments`
Ein Datei-Anhang zu einem Ticket. Zwei Speicherstrategien:
- `ContainsPrivateData = true` → Inhalt **AES-GCM-verschlüsselt** als Base64 in
  `DatenBase64` (DB); `DirectoryPath` bleibt leer.
- `ContainsPrivateData = false` → Datei im Projekt-Unterordner gespeichert,
  relativer Pfad in `DirectoryPath`; `DatenBase64` bleibt leer.

Felder: `Id`, `TicketId`, `UploadedByUserId`, `ContainsPrivateData`, `DataName`
(Originalname), `ContentType` (MIME), `FileSize` (Bytes), `DatenBase64`,
`DirectoryPath`, `CreatedAt`.

### 4.7 `KnowledgeArticle`
Ein Wissensartikel / Lösungsvorschlag.
- `Id` – PK; `Title` – Kurztitel/Problem; `Keywords` – Stichwörter für die Suche;
  `Solution` – Lösungstext.
- `DepartmentId?` – optionale Zuordnung; `IsPublished` – nur veröffentlichte
  werden vorgeschlagen.
- `CreatedByUserId`, `CreatedAt`, `UpdatedAt`.

### 4.8 `Department`, `Subject`
- `Department`: `Id`, `Name`.
- `Subject`: `Id`, `Title`, `DepartmentId`, `IsVerified`. Themen können beim
  Ticket-Erstellen neu (unverifiziert) entstehen.

---

## 5. Schicht „Functions" (`/Functions`)

Validierungs-Attribute (Eingangsprüfung an den DTOs) und Datei-Verschlüsselung.

### 5.1 `ExistsInColumnAttribute` (ValidationAttribute)
Prüft generisch per Reflection/Expression-Tree, ob ein Wert in einer bestimmten
Spalte einer Entität existiert (z. B. „existiert diese E-Mail als User?",
„existiert diese Abteilung?"). `null`/leere Strings werden durchgelassen und
gelten als „nicht gesetzt"; ob ein Feld Pflicht ist, entscheidet ausschließlich
`[Required]`.

### 5.2 `RequiresFieldAttribute` (ValidationAttribute)
Bedingte Pflicht: Ein Feld darf nur gesetzt sein, wenn ein anderes Feld bereits
gesetzt ist (z. B. Zusatzkontakt 2 nur, wenn Zusatzkontakt 1 vorhanden ist).

### 5.3 `NotInFutureAttribute` (ValidationAttribute)
Datumsprüfung: Der Wert darf nicht in der Zukunft liegen (1 Tag Toleranz für
Zeitzonen-/Uhrdifferenzen). Wird beim Arbeitsdatum der Zeiterfassung genutzt.

### 5.4 `FileCrypto` (statische Klasse)
Symmetrische Verschlüsselung (AES-GCM) für private Datei-Anhänge – liefert
Vertraulichkeit und Integrität.
- `Encrypt(byte[] plaintext, byte[] key)` → Base64 von `nonce(12) | tag(16) |
  ciphertext`.
- `Decrypt(string base64, byte[] key)` → Klartext-Bytes.
- Der 32-Byte-Schlüssel kommt aus der Konfiguration `Attachments:Key` (siehe 7.0).

---

## 6. Schicht „DTOs" (`/DTOs`) – Eintrittstore

DTOs sind die **einzigen** Eintrittspunkte für Client-Daten. Dank
`[ApiController]` führt ein Verstoß gegen die DataAnnotations automatisch zu
`400 Bad Request`, **bevor** Controller-Code läuft – es kommt also nichts
Ungeprüftes durch.

### 6.1 Eingangs-DTOs (Client → Server)

| DTO | Zweck | Wichtige Prüfungen |
|-----|-------|--------------------|
| `LoginDto` | Login | E-Mail Pflicht + Format, Passwort Pflicht. |
| `RegisterDto` | Self-Registrierung | Vorname/Nachname Pflicht+MaxLen, E-Mail Format, Passwort-Regex (≥8, Groß/Klein/Ziffer), **Abteilung Pflicht (muss existieren)**. |
| `CreateUserDto` | Admin legt User an | Namen Pflicht+MaxLen, E-Mail Format, Passwort-Regex, Rolle, Abteilung optional (muss existieren). |
| `UpdateUserDto` | Admin ändert User | Alle Felder optional (inkl. Abteilung); MaxLen/E-Mail/Enum-Prüfung; nur gesetzte werden übernommen. |
| `UpdateProfileDto` | eigenes Profil | Alle Felder optional; MaxLen/E-Mail-Prüfung. |
| `ChangePasswordDto` | Passwortwechsel | Altes PW Pflicht, neues PW Regex. |
| `CreateTicketDto` | Ticket anlegen | Priorität (Enum), Titel/Beschreibung/Erwartet/Aktuell Pflicht+MaxLen, AGB+Billing müssen `true` sein, Abteilung muss existieren, Subject Pflicht (2–30 Zeichen), Zusatzkontakte müssen existieren. |
| `UpdateTicketDto` | Ticket ändern | Optionale Felder; existierende Zusatzkontakte. |
| `UpdateTicketStatusDto` | Statuswechsel | `Status` muss gültiger Enum-Wert sein. |
| `AssignTicketDto` | Zuweisung | `AssignToEmail` optional; wenn gesetzt, muss User existieren; `null` = Zuweisung entfernen. |
| `CreateDialogueDto` | Chat-Nachricht | `Text` Pflicht, 1–4000 Zeichen; `IsInternal` nur als Wunsch (serverseitig rollenabhängig erzwungen). |
| `CreateTimeEntryDto` | Zeiteintrag | `Minutes` 1–1440, `Note` ≤500, `WorkedAt` Pflicht + nicht in der Zukunft. |
| `CreateKnowledgeArticleDto` | Wissensartikel anlegen | Titel Pflicht (3–200), Stichwörter ≤300, Lösung Pflicht ≤5000, Abteilung optional (muss existieren). |
| `UpdateKnowledgeArticleDto` | Wissensartikel ändern | Alle Felder optional; gleiche Längen-/Existenzprüfungen. |

Datei-Uploads (`AttachmentController`) kommen als `multipart/form-data`
(`IFormFile`); deren Prüfung (Größe ≤ 5 MB, Dateiname) erfolgt direkt im
Controller, da `IFormFile` nicht über DataAnnotations validierbar ist.

### 6.2 Ausgangs-DTOs (Server → Client)

- `UserResponseDto` – Benutzer **ohne** Passwort-Hash; inkl. `DepartmentId` und
  `DepartmentName`.
- `TicketResponseDto` – angereichertes Ticket: zusätzlich `StatusCode`,
  `PriorityCode`, `CreatedByEmail`, `AssignedToEmail`, `DepartmentName`,
  `SubjectName`, `ExpectedResult`, `ActualResult`, `TotalMinutes` (Summe der
  erfassten Zeit). Versorgt Liste, Kanban und Detailseite.
- `DialogueResponseDto` – Chat-Nachricht inkl. Autor-E-Mail.
- `TimeEntryResponseDto` – Zeiteintrag inkl. Bearbeiter-E-Mail.
- `AttachmentResponseDto` – Anhang-Metadaten (ohne Inhalt) inkl. Hochlader-E-Mail.
- `AgentStatsDto`, `CustomerStatsDto` (inkl. `DepartmentName`),
  `DepartmentStatsDto` – Statistik-Zeilen.
- `DashboardDto` (+ `DashboardTicketDto`) – Dashboard-Kennzahlen + letzte Tickets.
- `KnowledgeArticleDto` – voller Artikel (Verwaltung); `KnowledgeSuggestionDto` –
  kompakter Vorschlag (Id, Titel, Lösung).
- `DepartmentDto`, `SubjectDto` – Auswahllisten fürs Frontend.

> `TicketTransactionsDto` existiert im Projekt, wird aktuell aber von keinem
> Controller verwendet (der Verlauf wird intern über die Entität
> `TicketTransaction` geschrieben).

---

## 7. Schicht „Controllers" (`/Controllers`)

Allgemein: Identität und Rolle werden **immer aus dem JWT** gelesen, nie aus dem
Request-Body. Dadurch kann niemand im Namen anderer handeln oder sich Rechte
geben. Die meisten Controller haben private Helfer `CurrentUserId` und `IsStaff`.

### 7.0 `Program.cs` (Startup)
Kein Controller, aber die zentrale Konfiguration:
- CORS-Policy `AllowAll`; lädt `appsettings.Local.json`.
- `EnsureJwtKeyExists` / `EnsureAttachmentKeyExists`: erzeugen beim ersten Start
  kryptographisch sichere Schlüssel und speichern sie in `appsettings.Local.json`.
- Registriert DbContext (SQL Server), Controller, OpenAPI, JWT-Bearer-Auth und
  Autorisierung.
- Middleware-Reihenfolge: HTTPS-Redirect, statische Dateien, CORS,
  Authentifizierung, (Dev) `Dev-Admin`-Header-Bypass, Autorisierung, Controller.
- Im Development: OpenAPI + Scalar; Dev-Middleware setzt bei Header
  `Authorization: Dev-Admin` eine Admin-Identität (nur zum Testen).
- Beim Start: `DbInitializer.Initialize(...)`.

### 7.1 `AuthController` (`api/auth`)
- `POST login` – prüft aktiven + freigeschalteten User + BCrypt-Passwort, gibt
  JWT zurück.
- `POST register` – legt einen `User` (Rolle `User`, `IsActivated = false`) mit
  Namen **und Abteilung** an; Freischaltung erfolgt durch einen Admin.
- `CreateToken` – baut das JWT mit Claims `NameIdentifier`, `Email`, `Role`.

### 7.2 `AccountController` (`api/account`, eingeloggt)
- `GET me` – eigenes Profil.
- `PUT me` – eigenes Profil teil-aktualisieren (leere Felder = unverändert,
  E-Mail-Eindeutigkeit geprüft).
- `PUT me/password` – Passwort ändern (altes PW muss stimmen).

### 7.3 `UserController` (`api/user`)
- `GET me` – eigene Id/E-Mail/Rolle (für Anzeige + rollenabhängige Navigation).
- `GET /` (Admin/Support) – alle aktiven User als DTO.
- `GET {id}` (Admin/Support).
- `POST` (Admin) – User anlegen.
- `PUT {id}` (Admin) – ausgewählte Felder ändern (Name/E-Mail/Rolle/IsActivated/
  IsActive).
- `DELETE {id}` (Admin) – Soft-Delete (`IsActive = false`).
- `GET pending` (Admin/Support) – offene Registrierungen.
- `POST {id}/approve` (Admin) – Registrierung freischalten.

### 7.4 `TicketController` (`api/ticket`, eingeloggt)
- `POST /` (**nur Rolle `User`**) – Ticket anlegen; löst Abteilung/Subject/
  Zusatzkontakte auf, legt Ticket + erste `TicketTransaction` an. Admin/Support
  erhalten hier `403`.
- `GET /` – Liste **mit Suche & Filter** (Query-Parameter `q`, `status`,
  `priority`, `departmentId`, `assignedTo`=`me`/`none`). User sehen nur eigene,
  Staff alle.
- `GET {id}` – Einzel-Ticket (Zugriffsschutz für fremde Tickets).
- `PATCH {id}` – Felder ändern (Zuweisung nur Staff); schreibt eine Transaktion.
- `PATCH {id}/status` – Kanban-Statuswechsel; pflegt `OpenedAt`/`ClosedAt` und
  schreibt eine Transaktion.
- `PATCH {id}/assign` (Admin/Support) – Bearbeiter zuweisen/entfernen.
- **Private Helfer:** `ResolveUserIdOrNull`, `WriteTransaction`,
  `ProjectTickets` (LINQ-Projektion mit Left-Joins auf Department/Subject/
  Ersteller/Bearbeiter + Summe der Zeiteinträge).

### 7.5 `DialogueController` (`api/ticket/{ticketId}/dialogue`, eingeloggt)
Chat-Funktion, gespeichert in `TicketDialogue`.
- `GET` – Nachrichten chronologisch. Staff sieht alles; ein User nur sein
  eigenes Ticket und keine internen Notizen (doppelt abgesichert).
- `POST` – neue Nachricht; `IsInternal` für Nicht-Staff zwangsweise `false`;
  geschlossene Tickets sind für normale User schreibgeschützt.

### 7.6 `TimeEntryController` (`api/ticket/{ticketId}/time`, Admin/Support)
Manuelle Zeiterfassung.
- `GET` – alle Einträge eines Tickets (neueste zuerst).
- `POST` – neuer Eintrag (`UserId` aus JWT, Minuten/Datum im DTO geprüft).
- `DELETE {entryId}` – Support nur eigene, Admin alle.

### 7.7 `AttachmentController` (`api/ticket/{ticketId}/attachments`, eingeloggt)
Datei-Anhänge.
- `GET` – Metadaten-Liste (Zugriff: Ersteller oder Staff).
- `POST` – Upload (`multipart`: `file` + `isPrivate`); max. 5 MB; privat →
  verschlüsselt in DB, öffentlich → Datei im Ordner `AttachmentStorage/{ticketId}/`.
- `GET {attachmentId}/download` – entschlüsselt bzw. liest die Datei und liefert
  sie aus.
- `DELETE {attachmentId}` – Staff oder Hochlader; entfernt öffentliche Dateien
  auch von der Platte.

### 7.8 `StatisticsController` (`api/statistics`, nur Admin)
- `GET agents` – pro Bearbeiter: zugewiesene Tickets, geschlossene Tickets,
  erfasste Minuten (Basis: alle aktiven Staff-User).
- `GET customers` – pro Kunde: Abteilung, erstellte Tickets, davon offen,
  gesamte Bearbeitungszeit (Summe der Zeiteinträge aller seiner Tickets).
- `GET departments` – **Bearbeitungszeit kategorisiert nach Abteilung des
  Erstellers**: pro Abteilung Benutzeranzahl, Tickets, davon offen und gesamte
  Bearbeitungszeit (inkl. Sammelposten „(ohne Abteilung)").

### 7.9 `DashboardController` (`api/dashboard`, eingeloggt)
- `GET` – rollenabhängige Kennzahlen: offen/in Bearbeitung/geschlossen/gesamt
  (Staff systemweit, User nur eigene); für Staff zusätzlich „mir zugewiesen
  (offen)" und „nicht zugewiesen (offen)"; plus die 5 zuletzt erstellten Tickets.

### 7.10 `MetadataController` (`api/metadata`, eingeloggt)
Auswahllisten fürs Frontend.
- `GET departments` (**[AllowAnonymous]**, da auch auf der Registrierungsseite
  ohne Login gebraucht) / `GET subjects` (optional pro Abteilung).
- `GET agents` (Admin/Support) – Bearbeiterliste für die Zuweisung.

### 7.11 `KnowledgeController` (`api/knowledge`, eingeloggt)
Wissensdatenbank / Lösungsvorschläge.
- `GET suggest?q=...` (alle eingeloggten) – einfache Stichwortsuche: Text wird in
  Wörter zerlegt, Treffer nach Anzahl passender Wörter sortiert (Top 5).
- `GET /` (Admin/Support) – alle Artikel.
- `GET {id}` – einzelner Artikel (unveröffentlichte nur für Staff).
- `POST` / `PUT {id}` / `DELETE {id}` (Admin/Support) – Pflege mit DTO-Validierung.

---

## 8. Frontend (`/wwwroot`)

### 8.1 `app.js` (gemeinsame Logik)
- `TOKEN` aus `localStorage`; ohne Token → Redirect zum Login.
- `api(path, options)` – fetch-Wrapper mit Auth-Header, JSON und einheitlichem
  Fehlerhandling (inkl. Auswertung der ASP.NET-Validierungsfehler), Auto-Logout
  bei `401`.
- `getMich()` (gecacht) und `istStaff()`.
- `seitenleisteAufbauen(aktiv)` – baut die linke Leiste: ganz oben der
  eigenständige Link **Startseite** (→ `dashboard.html`, **kein** Dropdown),
  darunter **Dropdown-Gruppen**: *Tickets* (Meine Tickets; *Ticket erstellen* und
  *Problem melden* nur für Rolle `User`), *Bearbeitung* (Staff: Kanban,
  Wissensdatenbank), *Verwaltung* (Admin: Statistik, Benutzer), *Konto* (Profil).
  Die Gruppe mit dem aktiven Punkt ist geöffnet; `navToggle()` klappt um.
- Helfer: `esc()` (HTML-Escaping), `minutenFormat()` sowie Status-/Prioritäts-
  Tabellen (Text/Farbe) passend zu den Enums.

### 8.2 `style.css`
Gemeinsames Layout: Sidebar inkl. Dropdown-Gruppen
(`.nav-gruppe/.nav-kopf/.nav-links`), Karten, Tabellen, Badges, Buttons,
Formulare.

### 8.3 Seiten

| Datei | Zugang | Zweck |
|-------|--------|-------|
| `index.html` | öffentlich | Login (setzt Token, bindet `app.js` nicht ein). Link zur Registrierung. |
| `register.html` | öffentlich | Selbst-Registrierung (Name, E-Mail, **Abteilung**, Passwort). |
| `dashboard.html` | alle | „Startseite": Kennzahlen-Kacheln + zuletzt erstellte Tickets. |
| `startseite.html` | alle | „Meine Tickets": Ticketliste mit Such-/Filterleiste; Kunde sieht eigene, Staff alle. |
| `TicketErstellen.html` | nur User | 5-Schritt-Assistent; Kontaktdaten autom. & gesperrt, Abteilung als Dropdown, **Live-Lösungsvorschläge** beim Titel. |
| `problemMelden.html` | nur User | Schnelles Kontakt-/Ticketformular. |
| `ticket.html` | alle | Ticket-Detail: Beschreibung, **Chat** (interne Notizen für Staff), **Anhänge**; für Staff zusätzlich Status, Zuweisung, **Zeiterfassung**. |
| `kanban.html` | Staff | 3-Spalten-Board mit **Drag & Drop** und „Öffnen"-Link. |
| `statistik.html` | Admin | Bearbeiter- und Kundenauswertung (Tabellen + Balken). |
| `benutzer.html` | Admin | Benutzer verwalten (Rolle, **Abteilung**, sperren) + Registrierungen freischalten/ablehnen. |
| `wissen.html` | Staff | Wissensartikel anlegen/bearbeiten/löschen. |
| `profil.html` | alle | Eigenes Profil + Passwort ändern. |

---

## 9. Sicherheits- & Validierungskonzept

- **DTOs als einziges Eintrittstor:** Jede Schreiboperation läuft über ein DTO
  mit DataAnnotations; ungültige Requests werden automatisch mit `400`
  abgewiesen, bevor Logik greift. Datei-Uploads werden im Controller geprüft.
- **Identität aus dem JWT:** Ersteller/Bearbeiter/Zeit-Erfasser/Hochlader werden
  nie vom Client übernommen.
- **Rollenautorisierung:** `[Authorize(Roles = ...)]` auf Controllerebene plus
  feingranulare Prüfungen (Sichtbarkeit interner Notizen, eigenes vs. fremdes
  Ticket, Lösch-/Download-Rechte bei Anhängen).
- **Erzwungene Felder:** `IsInternal` für Nicht-Staff immer `false`; Zuweisung
  nur durch Staff.
- **Verschlüsselung:** Private Anhänge werden mit AES-GCM verschlüsselt (Key aus
  Konfiguration, automatisch erzeugt).
- **Soft-Delete:** Benutzer werden nur deaktiviert, nie gelöscht.
- **XSS-Schutz im Frontend:** Alle dynamischen Texte laufen durch `esc()` bzw.
  lokales Escaping.

---

## 10. Entwicklungshistorie (Changelog)

Die Anwendung wurde ausgehend von einer Grundlage (Auth, User, Ticket-Basis,
einfache HTML-Seiten) in drei Ausbaustufen erweitert.

### Ausbau 1 – Bearbeitung, Chat, Zeit, Statistik
- `TicketStatus` um `InProgress` erweitert (Kanban-Spalten).
- `TicketDialogue` sauber neu modelliert (eigener PK, `IsInternal`); neuer
  `DialogueController` (Chat).
- Neues Model `TicketTimeEntry` + `TimeEntryController` (Zeiterfassung).
- `StatisticsController` (Admin), `MetadataController`, Ticket-Zuweisung,
  angereicherte Ticket-Antworten.
- Frontend: `kanban.html`, `ticket.html`, `statistik.html`, neue
  `startseite.html`, gemeinsame `app.js`/`style.css`.
- **Behobene Fehler der Grundlage:**
  - `ExistsInColumnAttribute` lehnte `null` ab → optionale Felder schlugen fehl.
  - `UpdateUserDto`/`UpdateProfileDto` hatten `[Required]` auf optionalen Feldern
    → partielle Updates unmöglich.
  - `GET /api/user/me` fehlte, obwohl das Frontend es aufrief.
  - Ticket-Erstellung schickte veraltete Felder; `CreateTicketDto.SubjectName`
    war ungeprüft.
  - „Problem melden" schickte ein ungültiges Payload; Login-Seite zeigte im
    Fehlerfall „Erfolgreicher Login!".

### Ausbau 2 – Konto, Verwaltung, Navigation
- Registrierung vervollständigt (Vor-/Nachname im `RegisterDto`), `register.html`
  + Link auf der Login-Seite.
- `profil.html` (eigenes Profil + Passwort).
- `benutzer.html` (Admin): Rolle ändern, sperren/entsperren, Registrierungen
  freischalten/ablehnen.
- Ticket-Kontaktdaten automatisch aus dem Konto + readonly.
- Linke Leiste auf aufklappbare Dropdown-Gruppen umgestellt.

### Ausbau 3 – Anhänge, Dashboard, Suche, Wissensdatenbank
- `TicketAttachments` erweitert + `AttachmentController` (privat verschlüsselt /
  öffentlich als Datei); `FileCrypto`; Upload-Key in `Program.cs`. Anhang-UI in
  `ticket.html`.
- `DashboardController` + `dashboard.html`.
- Suche/Filter in `TicketController.GetAll` + Filterleiste in `startseite.html`.
- `KnowledgeArticle` + `KnowledgeController` (Stichwortsuche), `wissen.html`,
  Live-Vorschläge beim Ticket-Titel, Seed-Artikel im `DbInitializer`.

### Ausbau 5 – Abteilungszugehörigkeit & Abteilungs-Statistik
- Jeder User gehört einer **Abteilung** an; diese wird bei der Registrierung
  gewählt (`RegisterDto.DepartmentName` Pflicht, `AuthController` setzt sie).
- `GET /api/metadata/departments` ist nun `[AllowAnonymous]` (für die
  Registrierungsseite).
- `UserResponseDto` um `DepartmentId`/`DepartmentName` erweitert; `CreateUserDto`/
  `UpdateUserDto` um optionale Abteilung. `benutzer.html` zeigt/ändert die
  Abteilung, `register.html` hat ein Abteilungs-Dropdown.
- **Statistik nach Abteilung:** neuer Endpunkt `GET /api/statistics/departments`
  (Bearbeitungszeit kategorisiert nach Abteilung des Erstellers);
  `CustomerStatsDto` um Abteilung erweitert; `statistik.html` zeigt eine
  Abteilungs-Tabelle und eine Abteilungsspalte bei den Kunden.
- `DbInitializer`: Abteilungen werden vor den Benutzern angelegt; Seed-User
  erhalten Abteilungen.

### Ausbau 4 – Navigations- & Rechte-Feinschliff
- „Dashboard" in der Sidebar umbenannt in **„Startseite"** und als eigenständiger
  Top-Link aus dem Dropdown herausgelöst.
- **Ticket-Erstellung auf die Rolle `User` beschränkt** (`POST /api/ticket` mit
  `[Authorize(Roles = "User")]`); im Frontend sind „Ticket erstellen" und
  „Problem melden" nur noch für User sichtbar.
- Bestätigt: Admin **und** Support dürfen Tickets bearbeiten; Statistik und
  Benutzerverwaltung bleiben Admin-exklusiv.

---

## 11. Mögliche nächste Schritte

- EF-Core-**Migrationen** statt `EnsureCreated()` (Schema-Updates ohne DB-Löschen).
- Anhänge bereits **beim Ticket-Erstellen** hochladen (derzeit erst in der
  Detailansicht).
- Lösungsvorschläge auch auf Basis der **Beschreibung** statt nur des Titels.
- **E-Mail-Benachrichtigungen** an Ersteller/Zusatzkontakte (sobald Mailversand
  verfügbar ist).
- Pagination/Filter-Erweiterung für große Ticketmengen; **Wiedereröffnen**
  geschlossener Tickets; SLA/Fälligkeitsdaten.
