using Lynqo_Backend.Data;
using LynqoBackend.Models;
using LynqoBackend.Models.DTOs;
using Microsoft.EntityFrameworkCore;


namespace LynqoBackend.Models.Services
{
    public class StoreService
    {
        private readonly LynqoDbContext _context;

        public StoreService(LynqoDbContext context)
        {
            _context = context;
        }

        public async Task<List<StoreItemDTO>> GetItemsAsync()
        {
            return await _context.StoreItems
                .Select(i => new StoreItemDTO
                {
                    Id = i.Id,
                    Name = i.Name,
                    Description = i.Description,
                    Price = i.Price,
                    MaxQuantity = i.MaxQuantity
                })
                .ToListAsync();
        }

        public async Task<List<StoreItemDTO>> GetInventoryAsync(int userId)
        {
            return await _context.UserPurchases
                .Where(up => up.UserId == userId)
                .Include(up => up.Item)
                .Select(up => new StoreItemDTO
                {
                    Id = up.Item.Id,
                    Name = up.Item.Name,
                    Description = up.Item.Description,
                    Price = up.Item.Price,
                    MaxQuantity = up.Item.MaxQuantity
                })
                .ToListAsync();
        }

        public async Task PurchaseAsync(int userId, int itemId, int quantity)
        {
            var user = await _context.Users.FindAsync(userId)
                       ?? throw new InvalidOperationException("User not found.");

            var item = await _context.StoreItems.FindAsync(itemId)
                       ?? throw new InvalidOperationException("Item not found.");

            if (quantity <= 0) quantity = 1;

            var totalCost = item.Price * quantity;
            if (user.Coins < totalCost)
                throw new InvalidOperationException("Not enough coins.");

            // Optional: enforce max quantity per user
            var currentQty = await _context.UserPurchases
                .Where(up => up.UserId == userId && up.ItemId == itemId)
                .SumAsync(up => (int?)up.Quantity) ?? 0;

            if (currentQty + quantity > item.MaxQuantity)
                throw new InvalidOperationException("Max quantity reached for this item.");

            user.Coins -= totalCost;

            var purchase = new UserPurchase
            {
                UserId = userId,
                ItemId = itemId,
                Quantity = quantity,
                PurchasedAt = DateTime.UtcNow
            };

            _context.UserPurchases.Add(purchase);
            await _context.SaveChangesAsync();
        }
    }
}
