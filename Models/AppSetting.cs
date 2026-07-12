namespace TicketApplication.Models
{
    // key-value-tabelle für laufzeit-einstellungen (z.b. smtp), pflege per admin-ui
    public class AppSetting
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
