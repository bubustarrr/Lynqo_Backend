namespace Lynqo_Backend.Models.DTOs
{
    public class UserSettingsDTO
    {
        public bool DarkMode { get; set; }
        public bool SoundEnabled { get; set; }
        public string UiLanguage { get; set; } = "en";
        public bool NotificationsEnabled { get; set; }
    }
}
