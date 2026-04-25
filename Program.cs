using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Data;

var builder = WebApplication.CreateBuilder(args);

// Configuración de Base de Datos
if (builder.Environment.IsDevelopment())
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(connectionString));
}
else
{
    var pgConnectionString = builder.Configuration.GetConnectionString("PostgresConnection");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(pgConnectionString));
}

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Corregido: AddDefaultIdentity requiere Microsoft.AspNetCore.Identity.UI instalado
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

// Configuración Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
    options.InstanceName = "PlataformaCreditos_";
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Seeding
// --- INICIO DEL SEEDING DE ROLES Y USUARIOS ---
using (var scope = app.Services.CreateScope())
{
    // 1. Obtener el contexto de la base de datos y APLICAR MIGRACIONES
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync(); // <--- ¡ESTA ES LA LÍNEA MÁGICA QUE TE FALTA!

    // 2. Ahora sí, crear roles y usuarios
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    if (!await roleManager.RoleExistsAsync("Analista"))
    {
        await roleManager.CreateAsync(new IdentityRole("Analista"));
    }

    var analistaEmail = "analista@banco.com";
    var user = await userManager.FindByEmailAsync(analistaEmail);
    if (user == null)
    {
        user = new IdentityUser { UserName = analistaEmail, Email = analistaEmail, EmailConfirmed = true };
        await userManager.CreateAsync(user, "Password123!");
        await userManager.AddToRoleAsync(user, "Analista");
    }
}
// --- FIN DEL SEEDING ---

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// IMPORTANTE: Esto reemplaza a MapStaticAssets en .NET 8
app.UseStaticFiles(); 

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();