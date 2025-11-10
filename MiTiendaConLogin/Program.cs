using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MiTiendaConLogin.Data;
using MiTiendaConLogin.Models;
using System.Globalization;
using SendGrid.Extensions.DependencyInjection; // Para SendGrid
using MiTiendaConLogin.Services;
using Npgsql.EntityFrameworkCore; // Para PostgreSQL

var builder = WebApplication.CreateBuilder(args);

// 1. Cargar la configuración de SendGrid desde User Secrets
var sendGridKey = builder.Configuration["SendGridKey"];

// 2. Registrar el servicio de SendGrid en la aplicación
builder.Services.AddSendGrid(options =>
{
    options.ApiKey = sendGridKey;
});

// 3. Registrar NUESTRO servicio (IEmailSender)
builder.Services.AddTransient<IAppEmailSender, SendGridEmailSender>();

// --- ESTE BLOQUE PARA LA CULTURA ---
var cultureInfo = new CultureInfo("es-CR");
cultureInfo.NumberFormat.CurrencySymbol = "₡"; // Forzar el símbolo de Colón

CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
// --- FIN DEL BLOQUE ---

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
// 1. Cargar la NUEVA cadena de conexión de PostgreSQL (desde los User Secrets)
var postgresConnectionString = builder.Configuration.GetConnectionString("PostgresConnection")
    ?? throw new InvalidOperationException("Connection string 'PostgresConnection' not found in User Secrets.");

// 2. Usar Npgsql (PostgreSQL) en lugar de Sqlite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(postgresConnectionString)
);

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>() ;
builder.Services.AddControllersWithViews();

builder.Services.AddRazorPages();

builder.Services.AddDistributedMemoryCache(); // Añade un lugar en memoria para guardar la sesión
builder.Services.AddSession(options =>        // Habilita el servicio de sesión
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // El carrito se borra si está inactivo 30 min
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. If I want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession(); // ¡Activa la sesión!

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Products}/{action=Index}/{id?}");
app.MapRazorPages();

// ------------- INICIO DE CÓDIGO PARA CREAR ROLES -------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // Tarea 1: Lista de roles que queremos que existan
        var roleNames = new[] { "Admin", "ProductManager", "OrderManager" };

        foreach (var roleName in roleNames)
        {
            // Crear el rol si no existe
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // Tarea 2: Asignar el rol "Admin" a tu usuario (como antes)
        string adminEmail = "bran506rolo@gmail.com"; // Tu email
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser != null && !await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Un error ocurrió al crear los roles.");
    }
}
// ------------- FIN DE CÓDIGO PARA CREAR ROLES -------------
// --- 1. Definición de la función para sembrar la BD ---
async Task SeedDatabase(IHost app)
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            // Obtener los servicios de Roles y Usuarios
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            // Crear nuestro RoleSeeder pasándole los servicios
            var roleSeeder = new RoleSeeder(roleManager, userManager);
            
            // Ejecutar la lógica (crear roles y asignar admin)
            await roleSeeder.SeedRolesAsync();
        }
        catch (Exception ex)
        {
            // Opcional: registrar el error si falla
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Ocurrió un error al sembrar los roles.");
        }
    }
}

// --- 2. Llamada a la función para que se ejecute al arrancar ---
await SeedDatabase(app);


// --- 3. Esta línea ya existe (es la última) ---
app.Run();