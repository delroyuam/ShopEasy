using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopEasyMVC.Models
    {
    public class OrderItem
        {
        [Key]
        public int Id { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser al menos uno.")]
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Range(typeof(decimal), "0.01", "99999999.99", ErrorMessage = "El precio unitario debe ser mayor que cero.")]
        public decimal UnitPrice { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una orden válida.")]
        public int OrderId { get; set; }

        public Order Order { get; set; } = null!;

        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un producto válido.")]
        public int ProductId { get; set; }

        public Product Product { get; set; } = null!;
        }
    }