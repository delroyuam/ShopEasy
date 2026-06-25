using System.ComponentModel.DataAnnotations;

namespace ShopEasyMVC.Models
    {
    public class UserRole
        {
        public const string NamePattern = @"^[A-Za-z\u00C0-\u017F\s]+$";

        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del rol es obligatorio.")]
        [StringLength(256, MinimumLength = 2, ErrorMessage = "El rol debe tener entre 2 y 256 caracteres.")]
        [RegularExpression(NamePattern, ErrorMessage = "El rol solo puede contener letras y espacios.")]
        public string Name { get; set; } = string.Empty;

        public int? UserId { get; set; }

        public User? User { get; set; }
        }
    }
