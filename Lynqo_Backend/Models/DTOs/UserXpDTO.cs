namespace Lynqo_Backend.Models.DTOs
{
    public class UserXpDTO
    {
        public int TotalXp { get; set; }
        public List<UserXpEntryDTO> RecentXp { get; set; } = new();
    }

    public class UserXpEntryDTO
    {
        public int XpAmount { get; set; }
        public string Source { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
