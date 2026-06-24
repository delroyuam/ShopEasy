using System.ComponentModel.DataAnnotations;

namespace ShopEasyMVC.Models
    {
    public class UserRole
        {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del rol es obligatorio.")]
        [StringLength(256, MinimumLength = 2, ErrorMessage = "El rol debe tener entre 2 y 256 caracteres.")]
        public string Name { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un usuario válido.")]
        public int UserId { get; set; }

        public User User { get; set; } = null!;
        }
    }