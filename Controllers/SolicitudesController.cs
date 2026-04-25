using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Data;
using PlataformaCreditos.Models; // ¡Importante para que reconozca SolicitudCredito!
using PlataformaCreditos.ViewModels;

namespace PlataformaCreditos.Controllers
{
    [Authorize] // Solo usuarios logueados pueden ver sus solicitudes
    public class SolicitudesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public SolicitudesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ==========================================
        // 1. CATÁLOGO Y FILTROS (Pregunta 2)
        // ==========================================
        public async Task<IActionResult> Index(CatalogoSolicitudesViewModel model)
        {
            if (model.FechaInicio.HasValue && model.FechaFin.HasValue && model.FechaInicio > model.FechaFin)
            {
                ModelState.AddModelError(string.Empty, "La fecha de inicio no puede ser mayor a la fecha de fin.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User);

            var query = _context.SolicitudesCredito
                .Include(s => s.Cliente)
                .Where(s => s.Cliente!.UsuarioId == userId)
                .AsQueryable();

            if (model.EstadoFiltro.HasValue) query = query.Where(s => s.Estado == model.EstadoFiltro.Value);
            if (model.MontoMinimo.HasValue) query = query.Where(s => s.MontoSolicitado >= model.MontoMinimo.Value);
            if (model.MontoMaximo.HasValue) query = query.Where(s => s.MontoSolicitado <= model.MontoMaximo.Value);
            if (model.FechaInicio.HasValue) query = query.Where(s => s.FechaSolicitud >= model.FechaInicio.Value);
            if (model.FechaFin.HasValue) query = query.Where(s => s.FechaSolicitud <= model.FechaFin.Value.AddDays(1).AddTicks(-1));

            model.Solicitudes = await query.OrderByDescending(s => s.FechaSolicitud).ToListAsync();

            return View(model);
        }

        // ==========================================
        // 2. REGISTRAR NUEVA SOLICITUD (Pregunta 3)
        // ==========================================
        
        // GET: Mostrar el formulario
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var userId = _userManager.GetUserId(User);
            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == userId);

            // Si el usuario recién se registró, le creamos su perfil de cliente automáticamente
            if (cliente == null)
            {
                cliente = new Cliente { UsuarioId = userId, IngresosMensuales = 2500, Activo = true };
                _context.Clientes.Add(cliente);
                await _context.SaveChangesAsync();
            }

            return View();
        }

        // POST: Procesar y validar la solicitud
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SolicitudCredito model)
        {
            var userId = _userManager.GetUserId(User);
            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == userId);

            if (cliente == null) return NotFound();

            // REGLA DE NEGOCIO 1: Validar si ya tiene una solicitud pendiente
            var tienePendiente = await _context.SolicitudesCredito
                .AnyAsync(s => s.ClienteId == cliente.Id && s.Estado == EstadoSolicitud.Pendiente);

            if (tienePendiente)
            {
                ModelState.AddModelError(string.Empty, "Error: Ya tienes una solicitud en estado Pendiente. Debes esperar a que sea evaluada.");
            }

            // REGLA DE NEGOCIO 2: Validar el límite de 10 veces el ingreso
            var limiteCredito = cliente.IngresosMensuales * 10;
            if (model.MontoSolicitado > limiteCredito)
            {
                ModelState.AddModelError("MontoSolicitado", $"Error: El monto supera tu capacidad de pago. Tu límite actual es {limiteCredito:C}.");
            }

            if (ModelState.IsValid)
            {
                // Asignar valores obligatorios
                model.ClienteId = cliente.Id;
                model.Estado = EstadoSolicitud.Pendiente; // Inicia en Pendiente obligatoriamente
                model.FechaSolicitud = DateTime.Now;

                _context.SolicitudesCredito.Add(model);
                await _context.SaveChangesAsync();

                // Feedback claro de éxito
                TempData["SuccessMessage"] = "¡Tu solicitud ha sido registrada con éxito y está pendiente de evaluación!";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }
    }
}