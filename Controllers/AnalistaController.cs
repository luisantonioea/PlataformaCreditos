using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using PlataformaCreditos.Data;
using PlataformaCreditos.Models;

namespace PlataformaCreditos.Controllers
{
    [Authorize(Roles = "Analista")] // Seguridad: Solo Analistas
    public class AnalistaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache;

        public AnalistaController(ApplicationDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // GET: Mostrar lista de solicitudes pendientes
        public async Task<IActionResult> Index()
        {
            var solicitudesPendientes = await _context.SolicitudesCredito
                .Include(s => s.Cliente)
                .Where(s => s.Estado == EstadoSolicitud.Pendiente)
                .OrderBy(s => s.FechaSolicitud)
                .ToListAsync();

            return View(solicitudesPendientes);
        }

        // POST: Aprobar Solicitud
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Aprobar(int id)
        {
            var solicitud = await _context.SolicitudesCredito
                .Include(s => s.Cliente)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (solicitud == null || solicitud.Estado != EstadoSolicitud.Pendiente)
            {
                TempData["ErrorMessage"] = "La solicitud no existe o ya fue procesada.";
                return RedirectToAction(nameof(Index));
            }

            // REGLA: No aprobar si el monto excede 5 veces los ingresos
            var limiteAprobacion = solicitud.Cliente!.IngresosMensuales * 5;
            if (solicitud.MontoSolicitado > limiteAprobacion)
            {
                TempData["ErrorMessage"] = $"No se puede aprobar. El monto ({solicitud.MontoSolicitado:C}) supera el límite permitido para este cliente ({limiteAprobacion:C}).";
                return RedirectToAction(nameof(Index));
            }

            solicitud.Estado = EstadoSolicitud.Aprobado;
            await _context.SaveChangesAsync();

            // Invalidar el caché del cliente para que vea su aprobación de inmediato
            await _cache.RemoveAsync($"solicitudes_{solicitud.Cliente.UsuarioId}");

            TempData["SuccessMessage"] = "Solicitud aprobada exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Rechazar Solicitud
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rechazar(int id, string motivo)
        {
            if (string.IsNullOrWhiteSpace(motivo))
            {
                TempData["ErrorMessage"] = "Debe proporcionar un motivo de rechazo obligatorio.";
                return RedirectToAction(nameof(Index));
            }

            var solicitud = await _context.SolicitudesCredito
                .Include(s => s.Cliente)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (solicitud == null || solicitud.Estado != EstadoSolicitud.Pendiente)
            {
                TempData["ErrorMessage"] = "La solicitud no existe o ya fue procesada.";
                return RedirectToAction(nameof(Index));
            }

            solicitud.Estado = EstadoSolicitud.Rechazado;
            solicitud.MotivoRechazo = motivo;
            await _context.SaveChangesAsync();

            // Invalidar el caché
            await _cache.RemoveAsync($"solicitudes_{solicitud.Cliente.UsuarioId}");

            TempData["SuccessMessage"] = "Solicitud rechazada correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}