using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopEasyMVC.Models
    {
    public class Order
        {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El número de orden es obligatorio.")]
        [StringLength(256, ErrorMessage = "El número de orden no puede superar los 256 caracteres.")]
        public string OrderNumber { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        [Range(typeof(decimal), "0.01", "99999999.99", ErrorMessage = "El monto total debe ser mayor que cero.")]
        public decimal TotalAmount { get; set; }

        [EnumDataType(typeof(OrderStatus), ErrorMessage = "El estado de la orden no es válido.")]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(256, ErrorMessage = "La dirección de envío no puede superar los 256 caracteres.")]
        public string? ShippingAddress { get; set; }

        [EnumDataType(typeof(PaymentMethod), ErrorMessage = "El método de pago no es válido.")]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un usuario válido.")]
        public int UserId { get; set; }

        public User User { get; set; } = null!;

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        }
    }