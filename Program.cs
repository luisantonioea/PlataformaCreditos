using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. FORZAR POSTGRES Y LA CONEXIÓN (El secreto para que la migración salga bien)
var pgConnectionString = "Host=dpg-d7mi04e8bjmc738c62v0-a.oregon-postgres.render.com;Database=db_plataforma_creditos_5n5s;Username=db_plataforma_creditos_5n5s_user;Password=NdJ0NKStoBmN0C5xR6dlEJOI6auVbAVf;Port=5432;SSL Mode=Require;Trust Server Certificate=true;";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(pgConnectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

// 2. BLINDAJE REDIS (Si falla en Render, usa memoria para NO CRASHEAR)
var redisConn = builder.Configuration["Redis:ConnectionString"];
if (!string.IsNullOrEmpty(redisConn))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConn;
        options.InstanceName = "PlataformaCreditos_";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// 3. BLINDAJE TRY-CATCH (Evita el Error 139 al arrancar)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync(); 

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
    catch (Exception ex)
    {
        Console.WriteLine($"\n\n=== ERROR CRITICO DE BASE DE DATOS EVITADO ===\n{ex.Message}\n=====================================\n\n");
    }
}

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