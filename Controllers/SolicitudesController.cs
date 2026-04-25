using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed; // IMPORTANTE PARA REDIS
using System.Text.Json; // IMPORTANTE PARA SERIALIZAR CACHE
using PlataformaCreditos.Data;
using PlataformaCreditos.Models;
using PlataformaCreditos.ViewModels;

namespace PlataformaCreditos.Controllers
{
    [Authorize]
    public class SolicitudesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IDistributedCache _cache; // Inyectamos Redis

        public SolicitudesController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IDistributedCache cache)
        {
            _context = context;
            _userManager = userManager;
            _cache = cache;
        }

        // ==========================================
        // 1. CATÁLOGO CON CACHÉ REDIS (Pregunta 4)
        // ==========================================
        public async Task<IActionResult> Index(CatalogoSolicitudesViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            string cacheKey = $"solicitudes_{userId}";
            bool isFiltering = model.EstadoFiltro.HasValue || model.MontoMinimo.HasValue || model.MontoMaximo.HasValue || model.FechaInicio.HasValue || model.FechaFin.HasValue;

            if (!isFiltering)
            {
                try 
                {
                    var cachedData = await _cache.GetStringAsync(cacheKey);
                    if (!string.IsNullOrEmpty(cachedData))
                    {
                        model.Solicitudes = JsonSerializer.Deserialize<List<SolicitudCredito>>(cachedData)!;
                        return View(model);
                    }
                }
                catch { /* Redis no responde, ignoramos */ }
            }

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

            // AQUI ESTA LA CORRECCION: Try-Catch también en la escritura
            if (!isFiltering)
            {
                try 
                {
                    var cacheOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60) };
                    await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(model.Solicitudes), cacheOptions);
                }
                catch { /* Redis no responde, no guardamos en caché, pero el usuario sigue viendo su info */ }
            }

            return View(model);
        }

        // ==========================================
        // 2. DETALLE Y SESIÓN (Pregunta 4)
        // ==========================================
        // Cuando el usuario entra a ver el detalle, guardamos su visita en la sesión
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);
            var solicitud = await _context.SolicitudesCredito
                .Include(s => s.Cliente)
                .FirstOrDefaultAsync(s => s.Id == id && s.Cliente!.UsuarioId == userId);

            if (solicitud == null) return NotFound();

            // GUARDAR EN SESIÓN: Última solicitud visitada
            HttpContext.Session.SetString("UltimaSolicitudId", solicitud.Id.ToString());
            HttpContext.Session.SetString("UltimaSolicitudMonto", solicitud.MontoSolicitado.ToString("C"));

            return View(solicitud);
        }

        // ==========================================
        // 3. REGISTRAR (Con invalidación de caché)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var userId = _userManager.GetUserId(User);
            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == userId);

            if (cliente == null)
            {
                cliente = new Cliente { UsuarioId = userId, IngresosMensuales = 2500, Activo = true };
                _context.Clientes.Add(cliente);
                await _context.SaveChangesAsync();
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SolicitudCredito model)
        {
            var userId = _userManager.GetUserId(User);
            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == userId);

            if (cliente == null) return NotFound();

            var tienePendiente = await _context.SolicitudesCredito.AnyAsync(s => s.ClienteId == cliente.Id && s.Estado == EstadoSolicitud.Pendiente);
            if (tienePendiente) ModelState.AddModelError(string.Empty, "Error: Ya tienes una solicitud en estado Pendiente.");

            var limiteCredito = cliente.IngresosMensuales * 10;
            if (model.MontoSolicitado > limiteCredito) ModelState.AddModelError("MontoSolicitado", $"Error: El monto supera tu capacidad. Límite: {limiteCredito:C}.");

            if (ModelState.IsValid)
            {
                model.ClienteId = cliente.Id;
                model.Estado = EstadoSolicitud.Pendiente;
                model.FechaSolicitud = DateTime.Now;

                _context.SolicitudesCredito.Add(model);
                await _context.SaveChangesAsync();

                // INVALIDAR EL CACHÉ: Para que la tabla muestre el nuevo registro inmediatamente
                await _cache.RemoveAsync($"solicitudes_{userId}");

                TempData["SuccessMessage"] = "¡Tu solicitud ha sido registrada con éxito!";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }
    }
}