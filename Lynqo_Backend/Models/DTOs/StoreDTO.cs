namespace LynqoBackend.Models.DTOs
{
    public class StoreItemDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int Price { get; set; }
        public int MaxQuantity { get; set; }
    }

    public class PurchaseRequest
    {
        public int ItemId { get; set; }
        public int Quantity { get; set; } = 1;
    }
}
