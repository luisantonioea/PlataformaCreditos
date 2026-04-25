using System.ComponentModel.DataAnnotations;

namespace PlataformaCreditos.Models
{
    public class Cliente
    {
        public int Id { get; set; }
        
        [Required]
        public string UsuarioId { get; set; } // Se vincula con el usuario autenticado
        
        [Required(ErrorMessage = "Los ingresos son obligatorios.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Los ingresos mensuales deben ser mayores a 0.")]
        public decimal IngresosMensuales { get; set; }
        
        public bool Activo { get; set; } = true;
    }
}