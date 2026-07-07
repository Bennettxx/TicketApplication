namespace TicketApplication.Data
{
    // Status eines Tickets. Bildet zugleich die Spalten des Kanban-Boards ab.
    // WICHTIG: Die Zahlenwerte sind fest verdrahtet (Frontend + DB). Reihenfolge
    // entspricht dem typischen Bearbeitungsfluss von links nach rechts.
    public enum TicketStatus
    {
        Open = 0,        // Neu / unbearbeitet  -> Kanban-Spalte "Offen"
        InProgress = 1,  // In Bearbeitung       -> Kanban-Spalte "In Bearbeitung"
        Closed = 2       // Erledigt / geschlossen -> Kanban-Spalte "Geschlossen"
    }
}
