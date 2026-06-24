using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ShopEasyMVC.Models
    {
    public class User
        {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre completo es obligatorio.")]
        [StringLength(256, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 256 caracteres.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [StringLength(256, ErrorMessage = "El correo no puede superar los 256 caracteres.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(256, ErrorMessage = "La contraseña no puede superar los 256 caracteres.")]
        [DataType(DataType.Password)]
        public string PasswordHash { get; set; } = string.Empty;

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

        public ICollection<Order> Orders { get; set; } = new List<Order>();
        }
    }
