using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Data;
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

        public async Task<IActionResult> Index(CatalogoSolicitudesViewModel model)
        {
            // 1. Validación de negocio en servidor (Fechas)
            if (model.FechaInicio.HasValue && model.FechaFin.HasValue && model.FechaInicio > model.FechaFin)
            {
                ModelState.AddModelError(string.Empty, "La fecha de inicio no puede ser mayor a la fecha de fin.");
            }

            // Si hay errores de validación (montos negativos o fechas inválidas), devolvemos la vista vacía pero con los errores
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 2. Obtener el ID del usuario actual
            var userId = _userManager.GetUserId(User);

            // 3. Iniciar la consulta LINQ filtrando solo las solicitudes del cliente logueado
            var query = _context.SolicitudesCredito
                .Include(s => s.Cliente)
                .Where(s => s.Cliente!.UsuarioId == userId)
                .AsQueryable();

            // 4. Aplicar los filtros dinámicamente
            if (model.EstadoFiltro.HasValue)
            {
                query = query.Where(s => s.Estado == model.EstadoFiltro.Value);
            }

            if (model.MontoMinimo.HasValue)
            {
                query = query.Where(s => s.MontoSolicitado >= model.MontoMinimo.Value);
            }

            if (model.MontoMaximo.HasValue)
            {
                query = query.Where(s => s.MontoSolicitado <= model.MontoMaximo.Value);
            }

            if (model.FechaInicio.HasValue)
            {
                query = query.Where(s => s.FechaSolicitud >= model.FechaInicio.Value);
            }

            if (model.FechaFin.HasValue)
            {
                query = query.Where(s => s.FechaSolicitud <= model.FechaFin.Value.AddDays(1).AddTicks(-1)); // Incluye todo el día final
            }

            // 5. Ejecutar la consulta y asignarla al ViewModel
            model.Solicitudes = await query.OrderByDescending(s => s.FechaSolicitud).ToListAsync();

            return View(model);
        }
    }
}