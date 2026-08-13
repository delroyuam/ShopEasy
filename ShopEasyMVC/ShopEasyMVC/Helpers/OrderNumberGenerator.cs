using Microsoft.EntityFrameworkCore;
using ShopEasyMVC.Data;

namespace ShopEasyMVC.Helpers
    {
    public static class OrderNumberGenerator
        {
        public static async Task<string> GenerateAsync(AppDbContext context, int year)
            {
            var prefix = $"ORD-{year}-";

            var existingNumbers = await context.Orders
                .Where(o => o.OrderNumber.StartsWith(prefix))
                .Select(o => o.OrderNumber)
                .ToListAsync();

            var maxSequence = 0;

            foreach (var number in existingNumbers)
                {
                if (int.TryParse(number.AsSpan(prefix.Length), out var sequence) && sequence > maxSequence)
                    {
                    maxSequence = sequence;
                    }
                }

            return $"{prefix}{maxSequence + 1:D3}";
            }
        }
    }
