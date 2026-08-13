using System.ComponentModel.DataAnnotations;

namespace ShopEasyMVC.Models
    {
    public enum PaymentMethod
        {
        [Display(Name = "Tarjeta")]
        Card,

        [Display(Name = "Efectivo")]
        Cash,

        [Display(Name = "SINPE Móvil")]
        Sinpe
        }
    }
