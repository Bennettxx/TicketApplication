DB-Admin (SSMS) anlegen:
-- Admin Login erstellen (Server-Ebene)
CREATE LOGIN TicketAdmin WITH PASSWORD = 'AdminPasswort123!';

-- Admin User für die DB erstellen
USE TicketApplicationTestDB;
CREATE USER TicketAdmin FOR LOGIN TicketAdmin;

-- Admin Rechte geben (alles außer DB löschen/besitzen)
ALTER ROLE db_datareader ADD MEMBER TicketAdmin;   -- Lesen
ALTER ROLE db_datawriter ADD MEMBER TicketAdmin;   -- Schreiben
ALTER ROLE db_ddladmin ADD MEMBER TicketAdmin;     -- Tabellen erstellen/ändern


App-User anlegen:
-- App User Login erstellen (Server-Ebene)
CREATE LOGIN TicketAppUser WITH PASSWORD = 'AppUserPasswort123!';

-- App User für die DB erstellen
USE TicketApplicationTestDB;
CREATE USER TicketAppUser FOR LOGIN TicketAppUser;

-- Nur Lesen und Schreiben - kein DDL (keine Tabellen anlegen/löschen)
ALTER ROLE db_datareader ADD MEMBER TicketAppUser;  -- Lesen
ALTER ROLE db_datawriter ADD MEMBER TicketAppUser;  -- Schreiben