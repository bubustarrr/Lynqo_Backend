using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lynqo_Backend.Models;

[Table("api_tokens")]
public class ApiToken
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("token")]
    public string Token { get; set; } = ""; // The actual refresh string (random guid)

    [Column("scopes")]
    public string? Scopes { get; set; } = "refresh_token"; // Just "refresh_token" for now

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    // Navigation property
    public User? User { get; set; }
}
