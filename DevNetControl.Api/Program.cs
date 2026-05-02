using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using FluentValidation;
using FluentValidation.AspNetCore;
using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Infrastructure.Security;
using DevNetControl.Api.Infrastructure.Services;
using DevNetControl.Api.Infrastructure.Middleware;
using DevNetControl.Api.Infrastructure.RateLimiting;
using DevNetControl.Api.Validators;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "DevNetControl API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresá 'Bearer' [espacio] y tu token.\n\nEjemplo: 'Bearer eyJhbGciOiJIUzI1Ni...'"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.Services.AddHttpContextAccessor(); // Para AuditService
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<CreditService>();
builder.Services.AddScoped<EncryptionService>();
builder.Services.AddScoped<SshService>();
builder.Services.AddScoped<SshUserManager>();
builder.Services.AddScoped<SshSanitizerService>();
builder.Services.AddScoped<UserProvisioningService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<BulkOperationService>();
builder.Services.AddScoped<NodeHealthService>();
builder.Services.AddScoped<PlanValidationService>();
builder.Services.AddHostedService<UserExpirationBackgroundService>();
builder.Services.AddHostedService<NodeHealthBackgroundService>();

// Rate Limiting
builder.Services.AddRateLimiting();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (connectionString != null && connectionString.Contains("Data Source"))
    {
        options.UseSqlite(connectionString);
    }
    else
    {
        options.UseNpgsql(connectionString);
    }
});

var jwtKey = Environment.GetEnvironmentVariable("DEVNETCONTROL_JWT_KEY")
    ?? builder.Configuration["Jwt:Key"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),
            RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
            NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdminOnly", policy => policy.RequireRole("SuperAdmin"));
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("SuperAdmin", "Admin"));
    options.AddPolicy("ResellerOrAbove", policy => policy.RequireRole("SuperAdmin", "Admin", "Reseller"));
    options.AddPolicy("SubResellerOrAbove", policy => policy.RequireRole("SuperAdmin", "Admin", "Reseller", "SubReseller"));
});

// HSTS en producción (1 año)
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
    options.Preload = true;
});

var app = builder.Build();

app.UseGlobalExceptionHandler();

// Configurar Rate Limiting
var rateLimitService = app.Services.GetRequiredService<RateLimitService>();
rateLimitService.ConfigureDefaultPolicies();
app.UseRateLimit();
app.UseRateLimitCleanup(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHttpsRedirection(); // Para pruebas en dev
}
else
{
    // Enforce HTTPS en producción
    app.UseHttpsRedirection();
    app.UseHsts(); // HSTS configurado en services
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();
        await DbInitializer.SeedAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error critico durante la inicializacion.");
    }
}

app.Run();
