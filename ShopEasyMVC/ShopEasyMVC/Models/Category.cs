using System.ComponentModel.DataAnnotations;

namespace ShopEasyMVC.Models
    {
    public class Category
        {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(256, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 256 caracteres.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(256, ErrorMessage = "La descripción no puede superar los 256 caracteres.")]
        public string? Description { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
        }
    }
