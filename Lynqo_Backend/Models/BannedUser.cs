public class BannedUser
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? Reason { get; set; }
    public DateTime? BannedUntil { get; set; }
    public int? IssuedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public User User { get; set; } = null!;
    public User? Issuer { get; set; }
}