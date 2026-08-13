using System.Text.Json;
using Microsoft.AspNetCore.Http;
using ShopEasyMVC.Models;

namespace ShopEasyMVC.Helpers
    {
    public static class CartSessionExtensions
        {
        private const string SessionKey = "Cart";

        public static List<CartLine> GetCart(this ISession session)
            {
            var json = session.GetString(SessionKey);
            return json is null ? new List<CartLine>() : JsonSerializer.Deserialize<List<CartLine>>(json) ?? new List<CartLine>();
            }

        public static void SaveCart(this ISession session, List<CartLine> cart)
            {
            session.SetString(SessionKey, JsonSerializer.Serialize(cart));
            }

        public static void ClearCart(this ISession session)
            {
            session.Remove(SessionKey);
            }
        }
    }
