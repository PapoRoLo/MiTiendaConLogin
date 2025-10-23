using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MiTiendaConLogin.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>() ;
builder.Services.AddControllersWithViews();

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

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// ------------- INICIO DE CÓDIGO PARA CREAR ROLES -------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
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

app.Run();
