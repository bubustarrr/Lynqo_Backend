// UserPurchase.cs
namespace LynqoBackend.Models
{
    public class UserPurchase
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ItemId { get; set; }
        public int Quantity { get; set; }
        public DateTime PurchasedAt { get; set; }

        public User User { get; set; } = null!;
        public StoreItem Item { get; set; } = null!;
    }
}
