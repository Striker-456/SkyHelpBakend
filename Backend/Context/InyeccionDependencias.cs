using Microsoft.EntityFrameworkCore;
using SkyHelp.Repositories;
using SkyHelp.Repositories.Interfaces;
using SkyHelp.Services;
using SkyHelp.Services.Interfaces;
using System.Net.Security;

namespace SkyHelp.Context
{
    public static class InyeccionDependencias
    {
        public static IServiceCollection AddExternal(this IServiceCollection services, IConfiguration _Configuration)// Método de extensión para agregar dependencias externas
        {
            String connectionString = "";
            connectionString = _Configuration["ConnectionStrings:SQL"];// Obtener la cadena de conexión desde la configuración

            services.AddDbContext<SkyHelpContext>(options =>
                options.UseNpgsql(connectionString, npgsqlOptions =>
                    npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null)
                ));// Configurar el contexto de la base de datos con PostgreSQL
            services.AddScoped<IUsuariosRepository, UsuariosRepository>();// Inyección de dependencia del repositorio de usuarios
            services.AddScoped<IRolRepository, RolRepository>();// Inyección de dependencia del repositorio de roles
            services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();//Inyección de dependencia del repositorio de auditoría
            services.AddScoped<IDomiciliariosRepository, DomiciliariosRepository>();//Inyección de dependencia del repositorio de domiciliarios
            services.AddScoped<IReportesRepository, ReportesRepository>();//Inyección de dependencia del repositorio de reportes
            services.AddScoped<IPedidosRepository, PedidosRepository>();//Inyección de dependencia del repositorio de pedidos
            services.AddScoped<ITecnicosRepository, TecnicosRepository>();//Inyección de dependencia del repositorio de técnicos
            services.AddScoped<ITicketsRepository, TicketsRepository>();//Inyección de dependencia del repositorio de tickets
            services.AddScoped<IEstadosTicketsRepository, EstadosTicketsRepository>();//Inyección de dependencia del repositorio de estados de tickets
            services.AddScoped<IProgresoTicketsRepository, ProgresoTicketsRepository>();//Inyección de dependencia del repositorio de progreso de tickets

            services.AddScoped<IAuditoriaService, AuditoriaService>();
            services.AddScoped<IReportesService, ReportesService>();
            services.AddScoped<IReporteExportService, ReporteExportService>();
            services.AddScoped<IEstadisticasService, EstadisticasService>();
            services.AddScoped<IPedidosService, PedidosService>();
            return services;
            
        }
    }
}
