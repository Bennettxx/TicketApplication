// =====================================================================
// app.js  -  Gemeinsame Frontend-Logik für alle Seiten
// ---------------------------------------------------------------------
// Enthält: Token-Handling, ein fetch-Wrapper mit Auth-Header,
// das Laden des eigenen Profils und den Aufbau der Seitenleiste
// (rollenabhängig: Kanban/Statistik nur für Admin/Support).
// =====================================================================

// Token aus dem LocalStorage. Wird beim Login gesetzt.
const TOKEN = localStorage.getItem('meinToken');

// Auf Seiten, die einen Login erfordern, gleich zum Login umleiten.
// (Die Login-Seite selbst bindet app.js nicht ein.)
if (!TOKEN) {
    window.location.href = '/index.html';
}

// Zwischengespeichertes eigenes Profil, damit wir es nicht mehrfach laden.
let MICH = null;

// ---------------------------------------------------------------------
// api(path, options)
// Kleiner fetch-Wrapper: hängt den Authorization-Header an, setzt
// JSON-Content-Type und wirft bei Fehlern eine aussagekräftige Exception.
// Gibt das geparste JSON zurück (oder null bei 204 No Content).
// ---------------------------------------------------------------------
async function api(path, options = {}) {
    const opts = Object.assign({ method: 'GET' }, options);
    opts.headers = Object.assign({
        'Authorization': `Bearer ${TOKEN}`,
        'Content-Type': 'application/json'
    }, options.headers || {});

    const res = await fetch(path, opts);

    // Token abgelaufen/ungültig -> zurück zum Login.
    if (res.status === 401) {
        localStorage.removeItem('meinToken');
        window.location.href = '/index.html';
        throw new Error('Nicht angemeldet.');
    }

    if (!res.ok) {
        let msg = `Fehler ${res.status}`;
        try {
            const body = await res.json();
            // ASP.NET Validierungsfehler kommen als { errors: { Feld: [..] } }.
            if (body.errors) {
                msg = Object.values(body.errors).flat().join(' ');
            } else if (typeof body === 'string') {
                msg = body;
            } else if (body.title) {
                msg = body.title;
            }
        } catch { /* kein JSON-Body */ }
        throw new Error(msg);
    }

    if (res.status === 204) return null;
    const text = await res.text();
    return text ? JSON.parse(text) : null;
}

// Eigenes Profil laden (gecached).
async function getMich() {
    if (MICH) return MICH;
    MICH = await api('/api/user/me');
    return MICH;
}

// Ist der aktuelle User Admin oder Support?
function istStaff(mich) {
    return mich && (mich.role === 'Admin' || mich.role === 'Support');
}

// Abmelden: Token löschen, zurück zum Login.
function abmelden() {
    localStorage.removeItem('meinToken');
    window.location.href = '/index.html';
}

// Klappt eine Navigationsgruppe auf/zu (Dropdown-Verhalten der Sidebar).
function navToggle(kopf) {
    kopf.parentElement.classList.toggle('offen');
}

// ---------------------------------------------------------------------
// seitenleisteAufbauen(aktiv)
// Baut die linke Navigationsleiste als Dropdown-Menü in #sidebar.
// Die Funktionen sind in aufklappbare Gruppen unterteilt; die Gruppe mit
// dem aktiven Punkt ist beim Laden geöffnet.
// Kanban/Statistik/Benutzer erscheinen nur für Staff bzw. Admin.
// ---------------------------------------------------------------------
async function seitenleisteAufbauen(aktiv) {
    const mich = await getMich();
    const staff = istStaff(mich);
    const admin = mich.role === 'Admin';
    const isUser = mich.role === 'User'; // nur normale User dürfen Tickets erstellen

    // Eigenständiger Top-Link "Startseite" (kein Dropdown).
    const startLink =
        `<a href="/dashboard.html" class="nav-top ${aktiv === 'dashboard' ? 'aktiv' : ''}">Startseite</a>`;

    // Menü in Gruppen (Dropdowns) organisiert.
    const gruppen = [
        {
            titel: 'Tickets', sichtbar: true, punkte: [
                { key: 'tickets', label: 'Meine Tickets', href: '/startseite.html', sichtbar: true },
                // Tickets erstellen/melden nur für die Rolle User.
                { key: 'erstellen', label: 'Ticket erstellen', href: '/TicketErstellen.html', sichtbar: isUser },
                { key: 'problem', label: 'Problem melden', href: '/problemMelden.html', sichtbar: isUser }
            ]
        },
        {
            titel: 'Bearbeitung', sichtbar: staff, punkte: [
                { key: 'kanban', label: 'Kanban-Board', href: '/kanban.html', sichtbar: staff },
                { key: 'wissen', label: 'Wissensdatenbank', href: '/wissen.html', sichtbar: staff }
            ]
        },
        {
            titel: 'Verwaltung', sichtbar: admin, punkte: [
                { key: 'statistik', label: 'Statistik', href: '/statistik.html', sichtbar: admin },
                { key: 'benutzer', label: 'Benutzer', href: '/benutzer.html', sichtbar: admin }
            ]
        },
        {
            titel: 'Konto', sichtbar: true, punkte: [
                { key: 'profil', label: 'Mein Profil', href: '/profil.html', sichtbar: true }
            ]
        }
    ];

    const html = gruppen
        .filter(g => g.sichtbar && g.punkte.some(p => p.sichtbar))
        .map(g => {
            const punkte = g.punkte.filter(p => p.sichtbar);
            const enthältAktiv = punkte.some(p => p.key === aktiv);
            const links = punkte
                .map(p => `<a href="${p.href}" class="${p.key === aktiv ? 'aktiv' : ''}">${p.label}</a>`)
                .join('');
            return `
                <div class="nav-gruppe ${enthältAktiv ? 'offen' : ''}">
                    <div class="nav-kopf" onclick="navToggle(this)">
                        <span>${g.titel}</span><span class="pfeil">▾</span>
                    </div>
                    <div class="nav-links">${links}</div>
                </div>`;
        })
        .join('');

    const sidebar = document.getElementById('sidebar');
    if (sidebar) {
        sidebar.innerHTML = `
            <h2>Ticket System</h2>
            ${startLink}
            ${html}
            <a href="#" class="abmelden" onclick="abmelden(); return false;">Abmelden</a>
        `;
    }

    // Benutzername oben rechts (falls Element vorhanden).
    const benutzer = document.getElementById('benutzername');
    if (benutzer) {
        const name = (mich.firstName || mich.secondName)
            ? `${mich.firstName} ${mich.secondName}`.trim()
            : mich.email;
        benutzer.innerText = `${name} (${mich.role})`;
    }
}

// HTML-Escaping gegen versehentliches Einschleusen von Markup.
function esc(s) {
    if (s === null || s === undefined) return '';
    return String(s)
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;');
}

// Minuten -> "1h 30m" lesbar machen.
function minutenFormat(min) {
    min = min || 0;
    const h = Math.floor(min / 60);
    const m = min % 60;
    if (h > 0) return `${h}h ${m}m`;
    return `${m}m`;
}

// Status-Code -> Text/Farbe (passend zum TicketStatus-Enum im Backend).
const STATUS_TEXT = { 0: 'Offen', 1: 'In Bearbeitung', 2: 'Geschlossen' };
const STATUS_FARBE = { 0: '#6c757d', 1: '#ffc107', 2: '#28a745' };
const PRIO_TEXT = { 0: 'Low', 1: 'Medium', 2: 'High' };
const PRIO_FARBE = { 0: '#28a745', 1: '#ffc107', 2: '#dc3545' };
