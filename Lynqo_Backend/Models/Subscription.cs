// Subscription.cs
namespace LynqoBackend.Models
{
    public class Subscription
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? PlanName { get; set; }
        public int QuantityMonths { get; set; }
        public DateTime? StartsAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool AutoRenew { get; set; }
        public string? Provider { get; set; }
        public string? TransactionId { get; set; }

        public User User { get; set; } = null!;
    }
}
