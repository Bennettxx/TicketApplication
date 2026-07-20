// app.js - gemeinsame logik: token, api-wrapper, profil-cache, sidebar

// token aus dem localstorage, wird beim login gesetzt
const TOKEN = localStorage.getItem('meinToken');

// ohne token direkt zum login (login-seite bindet app.js nicht ein)
if (!TOKEN) {
    window.location.href = '/index.html';
}

// eigenes profil, gecached
let MICH = null;

// fetch-wrapper: auth-header + json, wirft bei fehlern, gibt json oder null (204) zurück
async function api(path, options = {}) {
    const opts = Object.assign({ method: 'GET' }, options);
    opts.headers = Object.assign({
        'Authorization': `Bearer ${TOKEN}`,
        'Content-Type': 'application/json'
    }, options.headers || {});

    const res = await fetch(path, opts);

    // token abgelaufen -> login
    if (res.status === 401) {
        localStorage.removeItem('meinToken');
        window.location.href = '/index.html';
        throw new Error('Nicht angemeldet.');
    }

    if (!res.ok) {
        let msg = `Fehler ${res.status}`;
        try {
            const body = await res.json();
            // asp.net validierungsfehler kommen als { errors: { feld: [..] } }
            if (body.errors) {
                msg = Object.values(body.errors).flat().join(' ');
            } else if (typeof body === 'string') {
                msg = body;
            } else if (body.title) {
                msg = body.title;
            }
        } catch { }
        throw new Error(msg);
    }

    if (res.status === 204) return null;
    const text = await res.text();
    return text ? JSON.parse(text) : null;
}

// eigenes profil laden, gecached
async function getMich() {
    if (MICH) return MICH;
    MICH = await api('/api/user/me');
    return MICH;
}

// admin oder support?
function istStaff(mich) {
    return mich && (mich.role === 'Admin' || mich.role === 'Support');
}

// token löschen, zurück zum login
function abmelden() {
    localStorage.removeItem('meinToken');
    window.location.href = '/index.html';
}

// sidebar-gruppe auf/zuklappen
function navToggle(kopf) {
    kopf.parentElement.classList.toggle('offen');
}

// sidebar rollenabhängig aufbauen, gruppe mit aktivem punkt ist offen
async function seitenleisteAufbauen(aktiv) {
    const mich = await getMich();
    const staff = istStaff(mich);
    const admin = mich.role === 'Admin';
    const isUser = mich.role === 'User';

    const startLink =
        `<a href="/dashboard.html" class="nav-top ${aktiv === 'dashboard' ? 'aktiv' : ''}">Startseite</a>`;

    const gruppen = [
        {
            titel: 'Tickets', sichtbar: true, punkte: [
                { key: 'tickets', label: 'Offene Tickets', href: '/startseite.html', sichtbar: true },
                { key: 'verlauf', label: 'Verlauf', href: '/verlauf.html', sichtbar: true },
                { key: 'erstellen', label: 'Ticket erstellen', href: '/TicketErstellen.html', sichtbar: isUser },
                // problem melden nur für support und user, nicht für admin
                { key: 'problem', label: 'Problem melden', href: '/problemMelden.html', sichtbar: !admin }
            ]
        },
        {
            titel: 'Bearbeitung', sichtbar: staff, punkte: [
                { key: 'kanban', label: 'Kanban-Board', href: '/kanban.html', sichtbar: staff },
                { key: 'wissen', label: 'Wissensdatenbank', href: '/wissen.html', sichtbar: staff },
                { key: 'themen', label: 'Abteilungen & Themen', href: '/themen.html', sichtbar: staff }
            ]
        },
        {
            titel: 'Verwaltung', sichtbar: admin, punkte: [
                { key: 'statistik', label: 'Statistik', href: '/statistik.html', sichtbar: admin },
                { key: 'benutzer', label: 'Benutzer', href: '/benutzer.html', sichtbar: admin },
                { key: 'probleme', label: 'Problemmeldungen', href: '/probleme.html', sichtbar: admin },
                { key: 'einstellungen', label: 'Einstellungen', href: '/einstellungen.html', sichtbar: admin }
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

    // benutzername oben rechts
    const benutzer = document.getElementById('benutzername');
    if (benutzer) {
        const name = (mich.firstName || mich.secondName)
            ? `${mich.firstName} ${mich.secondName}`.trim()
            : mich.email;
        benutzer.innerText = `${name} (${mich.role})`;
    }
}

// html-escaping gegen eingeschleustes markup
function esc(s) {
    if (s === null || s === undefined) return '';
    return String(s)
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;');
}

// minuten -> "1h 30m"
function minutenFormat(min) {
    min = min || 0;
    const h = Math.floor(min / 60);
    const m = min % 60;
    if (h > 0) return `${h}h ${m}m`;
    return `${m}m`;
}

// mapping status/prio-code -> text und farbe, passend zu den backend-enums
const STATUS_TEXT = { 0: 'Offen', 1: 'In Bearbeitung', 2: 'Geschlossen' };
const STATUS_FARBE = { 0: '#6c757d', 1: '#ffc107', 2: '#28a745' };
const PRIO_TEXT = { 0: 'Low', 1: 'Medium', 2: 'High' };
const PRIO_FARBE = { 0: '#28a745', 1: '#ffc107', 2: '#dc3545' };
