using Microsoft.EntityFrameworkCore;
using DevNetControl.Api.Infrastructure.Persistence; // <--- AGREGÁ ESTA LÍNEA

var builder = WebApplication.CreateBuilder(args);

// --- SERVICIOS ---
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Configuración Híbrida: Detecta si usamos SQLite (trabajo) o Postgres (casa)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (connectionString != null && connectionString.Contains("Data Source"))
        options.UseSqlite(connectionString);
    else
        options.UseNpgsql(connectionString);
});

var app = builder.Build();

// --- PIPELINE HTTP ---
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Para que funcione nuestro Dashboard de Humo
app.UseAuthorization();
app.MapControllers();

// --- INICIALIZACIÓN DE DATOS (MIGRACIONES + ADMIN SEED) ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        // Esto aplica los cambios a la DB automáticamente al dar "Play"
        await context.Database.MigrateAsync();
        // Crea el admin si la tabla está vacía
        await DbInitializer.SeedAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error al iniciar la base de datos.");
    }
}

app.Run();