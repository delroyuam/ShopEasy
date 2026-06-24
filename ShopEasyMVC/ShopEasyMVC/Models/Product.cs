using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopEasyMVC.Models
    {
    public class Product
        {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
        [StringLength(256, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 256 caracteres.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(256, ErrorMessage = "La descripción no puede superar los 256 caracteres.")]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Range(typeof(decimal), "0.01", "99999999.99", ErrorMessage = "El precio debe ser mayor que cero.")]
        public decimal CurrentPrice { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo.")]
        public int Stock { get; set; }

        [StringLength(256, ErrorMessage = "La URL de la imagen no puede superar los 256 caracteres.")]
        [Url(ErrorMessage = "Ingrese una URL válida.")]
        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una categoría válida.")]
        public int CategoryId { get; set; }

        public Category Category { get; set; } = null!;
        }
    }
