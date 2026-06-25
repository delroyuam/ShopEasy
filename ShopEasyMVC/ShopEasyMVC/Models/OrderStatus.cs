using System.ComponentModel.DataAnnotations;

namespace ShopEasyMVC.Models
    {
    public enum OrderStatus
        {
        [Display(Name = "Pendiente")]
        Pending,

        [Display(Name = "Enviado")]
        Shipped,

        [Display(Name = "Entregado")]
        Delivered,

        [Display(Name = "Cancelado")]
        Cancelled
        }
    }
