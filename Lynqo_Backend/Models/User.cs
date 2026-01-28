using Lynqo_Backend.Models;
using System.ComponentModel.DataAnnotations.Schema;

public class User
{
    public int Id { get; set; }

    public string Username { get; set; }

    [Column("display_name")]
    public string? DisplayName { get; set; }

    public string Email { get; set; }

    [Column("password_hash")]
    public string PasswordHash { get; set; }

    [Column("profile_pic_url")]
    public string? ProfilePicUrl { get; set; }

    public int Hearts { get; set; }

    public int Coins { get; set; }

    [Column("is_premium")]
    public bool IsPremium { get; set; }

    public string Role { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    public List<UserXp> XpHistory { get; set; } = new();
    public List<UserBadge> Badges { get; set; } = new();
    public List<LeaderboardEntry> LeaderboardEntries { get; set; } = new();
}

