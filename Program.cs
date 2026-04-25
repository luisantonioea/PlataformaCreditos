using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// --- INICIO DEL SEEDING DE ROLES Y USUARIOS ---
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Microsoft.AspNetCore.Identity.IdentityUser>>();

    // 1. Crear el rol Analista si no existe
    if (!await roleManager.RoleExistsAsync("Analista"))
    {
        await roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole("Analista"));
    }

    // 2. Crear el usuario Analista de prueba si no existe
    var analistaEmail = "analista@banco.com";
    var user = await userManager.FindByEmailAsync(analistaEmail);
    if (user == null)
    {
        user = new Microsoft.AspNetCore.Identity.IdentityUser { UserName = analistaEmail, Email = analistaEmail, EmailConfirmed = true };
        await userManager.CreateAsync(user, "Password123!"); // Contraseña del analista
        await userManager.AddToRoleAsync(user, "Analista");
    }
}
// --- FIN DEL SEEDING ---

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
