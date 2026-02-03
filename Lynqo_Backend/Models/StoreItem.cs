// StoreItem.cs
namespace LynqoBackend.Models
{
    public class StoreItem
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int Price { get; set; }
        public int MaxQuantity { get; set; }
    }
}
