using System.ComponentModel.DataAnnotations;
using System.Reflection;
using ShopEasyMVC.Models;

namespace ShopEasyMVC.Helpers
{
    public static class EnumExtensions
    {
        public static string GetDisplayName(this Enum value)
        {
            var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
            var displayAttribute = member?.GetCustomAttribute<DisplayAttribute>();

            return displayAttribute?.Name ?? value.ToString();
        }

        public static string ToBadgeClass(this OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => "bg-warning text-dark",
                OrderStatus.Shipped => "bg-info text-dark",
                OrderStatus.Delivered => "bg-success",
                OrderStatus.Cancelled => "bg-danger",
                _ => "bg-secondary"
            };
        }
    }
}
