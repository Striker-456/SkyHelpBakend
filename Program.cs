using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using SkyHelp.Context;
using Microsoft.OpenApi.Models;
using System.Text;

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Make configuration resilient when launching from bin/Debug (working dir may differ)
builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddExternal(builder.Configuration);
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "MicroserviceCRUD", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header using the Bearer scheme <br /> <br />
                        Enter Bearer [space] and then your token in the text input bellow <br /> <br />
                        Example: 'Bearer 123456abcdefg' <br /> <br />.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme()
            {
                Reference = new OpenApiReference()
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});
var extraOrigins = builder.Configuration["Cors:AllowedOrigins"]
    ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? Array.Empty<string>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddCors(options =>
{
    // Localhost, túneles de desarrollo, *.onrender.com y orígenes extra en Cors:AllowedOrigins.
    options.AddPolicy("AllowSpecificOrigin",
        corsBuilder => corsBuilder
            .SetIsOriginAllowed(origin =>
            {
                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)) return false;
                if (uri.IsLoopback || uri.Host.EndsWith(".devtunnels.ms", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (uri.Host.EndsWith(".onrender.com", StringComparison.OrdinalIgnoreCase))
                    return true;
                return extraOrigins.Any(allowed => string.Equals(allowed, origin, StringComparison.OrdinalIgnoreCase));
            })
            .AllowAnyMethod()
            .AllowAnyHeader());
});

builder.Services.AddRateLimiter(options =>
{
    // Frena fuerza bruta de login y spam de registro de cuentas.
    // Partición por IP: un cliente abusivo no debe agotar la cuota de los demás.
    options.AddPolicy("auth", httpContext => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "sin-ip",
        factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("Falta la configuración 'Jwt:Key'. Revisa appsettings.json o variables de entorno.");
if (string.IsNullOrWhiteSpace(jwtIssuer))
    throw new InvalidOperationException("Falta la configuración 'Jwt:Issuer'. Revisa appsettings.json o variables de entorno.");
if (string.IsNullOrWhiteSpace(jwtAudience))
    throw new InvalidOperationException("Falta la configuración 'Jwt:Audience'. Revisa appsettings.json o variables de entorno.");

var key = Encoding.ASCII.GetBytes(jwtKey);
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

var app = builder.Build();

// Aplica migraciones pendientes (esquema + seed de roles/estados/admin) al arrancar. Necesario
// para contenedores: un PostgreSQL recién levantado no tiene la base SkyHelp ni sus tablas
// hasta que algo las crea. Migrate() es seguro de llamar siempre: no hace nada si ya está al día.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SkyHelp.Context.SkyHelpContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    const int maxAttempts = 10;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            db.Database.Migrate();
            break;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(ex, "PostgreSQL no está listo (intento {Attempt}/{Max}). Reintentando...", attempt, maxAttempts);
            Thread.Sleep(TimeSpan.FromSeconds(2));
        }
    }
}

// Red de seguridad: cualquier excepción no controlada por un try/catch local
// (o que se les escape) termina aquí en vez de tumbar la respuesta sin registro.
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (feature?.Error is Exception ex)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Excepción no controlada en {Path}", context.Request.Path);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
        {
            mensaje = "Ocurrió un error interno. Intenta de nuevo más tarde."
        }));
    });
});

app.UseForwardedHeaders();

var swaggerEnabled = app.Environment.IsDevelopment()
    || app.Configuration.GetValue("Swagger:Enabled", false);
if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowSpecificOrigin");

app.UseRateLimiter();

app.UseAuthentication();
app.Use(async (context, next) =>
{
    await next();

    if (context.Response.StatusCode == StatusCodes.Status401Unauthorized)
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
        {
            mensaje = "No autenticado. Debe iniciar sesión para acceder a este recurso."
        }));
    }
    else if (context.Response.StatusCode == StatusCodes.Status403Forbidden)
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
        {
            mensaje = "Acceso denegado. No tiene permisos suficientes para realizar esta acción."
        }));
    }
});
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Servir el frontend DESPUÉS de los controladores API
var frontendPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Frontend"));
if (Directory.Exists(frontendPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(frontendPath),
        RequestPath = ""
    });
    app.MapFallback(async context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = 404;
            return;
        }
        context.Response.ContentType = "text/html";
        await context.Response.SendFileAsync(Path.Combine(frontendPath, "index.html"));
    });
}

app.Run();
