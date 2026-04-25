using System.ComponentModel.DataAnnotations;
using PlataformaCreditos.Models;

namespace PlataformaCreditos.ViewModels
{
    public class CatalogoSolicitudesViewModel
    {
        // La lista de resultados que mostraremos en la tabla
        public List<SolicitudCredito> Solicitudes { get; set; } = new List<SolicitudCredito>();

        // Campos para los filtros
        public EstadoSolicitud? EstadoFiltro { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "El monto mínimo no puede ser negativo.")]
        public decimal? MontoMinimo { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "El monto máximo no puede ser negativo.")]
        public decimal? MontoMaximo { get; set; }

        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
    }
}