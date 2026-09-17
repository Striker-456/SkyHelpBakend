using SkyHelp;
using Microsoft.EntityFrameworkCore;
using SkyHelp.Models;
using SkyHelp.Authorization;

namespace SkyHelp.Context
{
    public class SkyHelpContext : DbContext
    {
        public SkyHelpContext(DbContextOptions<SkyHelpContext> options) : base(options)
        {
        }

        // ================================================================================
        // GUIDs fijos para el seed de roles/usuario administrador (HasData exige valores
        // deterministas: no se puede usar Guid.NewGuid() aquí, EF los usa para calcular el
        // diff de la migración). NO reutilizar estos IDs para otros registros.
        // ================================================================================
        private static readonly Guid RolAdministradorId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid RolTecnicoId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid RolDomiciliarioId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        private static readonly Guid RolUsuarioId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        private static readonly Guid UsuarioAdministradorId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        private static readonly Guid EstadoAbiertoId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        private static readonly Guid EstadoPendienteId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        private static readonly Guid EstadoEnPreparacionId = Guid.Parse("88888888-8888-8888-8888-888888888888");
        private static readonly Guid EstadoEnRutaId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        private static readonly Guid EstadoResueltoId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        public DbSet<Usuarios> Usuarios { get; set; }
        public DbSet<Roles> Roles { get; set; }
        public DbSet<Auditoria> Auditoria { get; set; }
        public DbSet<Domiciliarios> Domiciliarios { get; set; }
        public DbSet<Reportes> Reportes { get; set; }
        public DbSet<Pedidos> Pedidos { get; set; }
        public DbSet<Tecnicos> Tecnicos { get; set; }
        public DbSet<Tickets> Tickets { get; set; }
        public DbSet<EstadosTicket> EstadosTickets { get; set; }
        public DbSet<ProgresoTickets> ProgresoTickets { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ================================================================================
            // Estrategia general de DeleteBehavior (evita "multiple cascade paths" en SQL Server
            // y sigue la lógica de negocio ya implementada en los repositorios):
            //
            //  - Roles y EstadosTickets son tablas de referencia/lookup: NUNCA deben poder arrastrar
            //    en cascada el borrado de Usuarios/Tickets que las usan -> Restrict.
            //  - Domiciliarios y Tecnicos son "perfiles de rol" que solo existen en función de un
            //    Usuario (así lo hace UsuariosRepository.EliminarUsuario, que los borra junto con
            //    el usuario) -> Cascade desde Usuarios.
            //  - Tickets se borra en cascada junto con su Usuario (igual que arriba, ya implementado
            //    en UsuariosRepository.EliminarUsuario) -> Cascade desde Usuarios.
            //  - Domiciliarios->Pedidos es Cascade (así lo hace DomiciliariosRepository.EliminarDomiciliario,
            //    que borra los pedidos del domiciliario). Por lo tanto Usuarios->Pedidos (el cliente)
            //    se deja en Restrict: preserva el historial de pedidos del cliente y evita la doble
            //    ruta de cascada Usuario->Pedido / Usuario->Domiciliario->Pedido que causaba el error
            //    original "may cause cycles or multiple cascade paths".
            //  - Domiciliarios->Tickets y Tecnicos->Tickets son opcionales: al borrar el domiciliario/
            //    técnico se desasigna el ticket (no se borra) -> ClientSetNull explícito (ya es lo que
            //    hace DomiciliariosRepository.EliminarDomiciliario a mano). No se usa SetNull real en
            //    la base de datos porque, junto con Usuarios->Domiciliarios/Tecnicos ya en Cascade,
            //    volvería a producir múltiples rutas de cascada hacia Tickets.
            //  - Reportes es contenido/histórico generado por un Usuario sin limpieza manual en el
            //    repositorio -> Restrict, para no perderlo silenciosamente.
            //  - Auditoria es el log de auditoría: Restrict a nivel de BD (protege el rastro ante
            //    borrados directos), pero UsuariosRepository.EliminarUsuario ya lo limpia a mano al
            //    borrar la cuenta completa, así que ese flujo sigue funcionando igual.
            // ================================================================================

            // Configuración de la entidad Usuarios
            modelBuilder.Entity<Usuarios>(entity =>
            {
                entity.HasKey(e => e.IdUsuario);
                entity.Property(e => e.IdRol).IsRequired();
                entity.Property(e => e.NombreUsuarios).IsRequired().HasMaxLength(50);
                entity.Property(e => e.NombreCompleto).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Correo).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Contrasena).IsRequired().HasMaxLength(100);
                entity.Property(e => e.EstadoCuenta).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Telefono).HasMaxLength(20);
                // El correo identifica de forma única al usuario para el login (AuthController /
                // UsuariosRepository.ObtenerUsuarioPorCorreo asumen unicidad, pero no había
                // restricción a nivel de BD que la garantizara).
                entity.HasIndex(e => e.Correo).IsUnique();
                // RELACIÓN: Roles -> Usuarios. Restrict: nunca se debe poder borrar un Rol y que
                // eso arrastre en cascada a todos los usuarios que lo tienen asignado.
                entity.HasOne(e => e.Rol)
                      .WithMany(t => t.Usuario)
                      .HasForeignKey(e => e.IdRol)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.ToTable("Usuarios");

                // Seed: usuario Administrador predeterminado. La contraseña está hasheada con
                // BCrypt (Seguridad.Hashear) — el hash de abajo corresponde a "Admin123", ya
                // verificado que Seguridad.Verificar("Admin123", hash) da true. No se puede llamar
                // a Seguridad.Hashear aquí porque HasData exige valores fijos en tiempo de diseño.
                entity.HasData(new
                {
                    IdUsuario = UsuarioAdministradorId,
                    IdRol = RolAdministradorId,
                    NombreUsuarios = "admin",
                    NombreCompleto = "Administrador SkyHelp",
                    Correo = "admin@skyhelp.com",
                    Contrasena = "$2a$11$iZtHw38ztzeNAfWiTfmQ9e8aig.dwYG/xb60VDRNYgZI5UKIPIemO",
                    EstadoCuenta = "Activo",
                    Telefono = (string?)null
                });
            });
            // Configuración de la entidad Roles
            modelBuilder.Entity<Roles>(entity =>
            {
                entity.HasKey(e => e.IDRol);
                entity.Property(e => e.NombreRol).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Descripcion).HasMaxLength(200);
                // Nombre de rol único: es una tabla de referencia (Administrador, Técnico, etc.).
                entity.HasIndex(e => e.NombreRol).IsUnique();
                entity.ToTable("Roles");

                // Seed: los 4 roles predeterminados del sistema.
                entity.HasData(
                    new { IDRol = RolAdministradorId, NombreRol = RoleNames.Administrador, Descripcion = "Administrador del sistema, con acceso total." },
                    new { IDRol = RolTecnicoId, NombreRol = RoleNames.Tecnico, Descripcion = "Técnico encargado del diagnóstico y soporte de tickets." },
                    new { IDRol = RolDomiciliarioId, NombreRol = RoleNames.Domiciliario, Descripcion = "Domiciliario encargado de la entrega de pedidos." },
                    new { IDRol = RolUsuarioId, NombreRol = RoleNames.Usuario, Descripcion = "Cliente que reporta tickets de soporte." }
                );
            });
            // Configuración de la entidad Auditoria
            modelBuilder.Entity<Auditoria>(entity =>
            {
                entity.HasKey(e => e.IDLog);
                entity.Property(e => e.IDUsuario).IsRequired();
                entity.Property(e => e.TipoEvento).IsRequired().HasMaxLength(100);
                entity.Property(e => e.TablaAfectada).IsRequired();
                entity.Property(e => e.IDRegistro).IsRequired();
                entity.Property(e => e.Descripcion).IsRequired().HasMaxLength(300);
                entity.Property(e => e.FechaEvento).IsRequired();
                entity.Property(e => e.DireccionIp).HasMaxLength(45);
                // RELACIÓN: Usuarios -> Auditoria. Restrict: protege el rastro de auditoría de un
                // borrado directo/accidental. El borrado completo de una cuenta (que sí debe llevarse
                // su auditoría) sigue funcionando porque UsuariosRepository.EliminarUsuario la limpia
                // explícitamente antes de borrar al usuario.
                entity.HasOne(e => e.Usuario)
                      .WithMany(t => t.Auditorias)
                      .HasForeignKey(e => e.IDUsuario)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.ToTable("Auditoria");
            });

            // Configuración de la entidad Domiciliarios
            modelBuilder.Entity<Domiciliarios>(entity =>
            {
                entity.HasKey(e => e.IdDomiciliario);
                entity.Property(e => e.NombreCompleto).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Telefono).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(50);
                entity.Property(e => e.EstadoActividad).IsRequired().HasMaxLength(20);
                entity.Property(e => e.IDUsuario).IsRequired();
                // Un usuario no puede tener más de un perfil de Domiciliario (evita filas
                // duplicadas si algo vuelve a intentar crear el registro, p. ej.
                // UsuariosController.AsegurarRegistroDomiciliario o el endpoint manual).
                entity.HasIndex(e => e.IDUsuario).IsUnique();
                // RELACIÓN: Usuarios -> Domiciliarios. Cascade: el registro de Domiciliario es un
                // perfil de rol que sólo existe en función del Usuario (igual que ya asume
                // UsuariosRepository.EliminarUsuario, que lo borra junto con la cuenta).
                entity.HasOne(e => e.Usuario)
                      .WithMany(t => t.Domiciliarios)
                      .HasForeignKey(e => e.IDUsuario)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.ToTable("Domiciliarios");
            });
            //Configuracion de la entidada Reportes
            modelBuilder.Entity<Reportes>(entity =>
            {
                entity.HasKey(e => e.IdReporte);
                entity.Property(e => e.TipoReporte).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Descripcion).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Titulo).IsRequired().HasMaxLength(200);
                entity.Property(e => e.FechaGeneracion).IsRequired();
                entity.Property(e => e.IdUsuario).IsRequired();
                entity.Property(e => e.IdOrigen).IsRequired();
                entity.Property(e => e.OrigenTabla).IsRequired().HasMaxLength(100);
                // RELACIÓN: Usuarios -> Reportes. Restrict: preserva el historial de reportes
                // generados; no hay limpieza manual en ReportesRepository.
                entity.HasOne(e => e.Usuario)
                      .WithMany(t => t.Reportes)
                      .HasForeignKey(e => e.IdUsuario)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.ToTable("Reportes");
            });
            // Configuración de la entidad Pedidos
            modelBuilder.Entity<Pedidos>(entity =>
            {
                entity.HasKey(e => e.IdPedido);
                entity.Property(e => e.NumeroPedido).ValueGeneratedOnAdd();
                entity.HasIndex(e => e.NumeroPedido).IsUnique();
                entity.Property(e => e.FechaPedido).IsRequired();
                entity.Property(e => e.EstadoPedido).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DireccionEntrega).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Observaciones).HasMaxLength(500);
                entity.Property(e => e.IdUsuario).IsRequired();
                entity.Property(e => e.IdDomiciliario).IsRequired();
                // RELACIÓN: Usuarios -> Pedidos (cliente). Restrict: preserva el historial de pedidos
                // del cliente y evita la doble ruta de cascada hacia Pedidos (Usuario->Pedido y
                // Usuario->Domiciliario->Pedido) que producía el error original de SQL Server.
                entity.HasOne(e => e.Usuario)
                      .WithMany(t => t.Pedidos)
                      .HasForeignKey(e => e.IdUsuario)
                      .OnDelete(DeleteBehavior.Restrict);
                // RELACIÓN: Domiciliarios -> Pedidos. Cascade: así lo hace ya
                // DomiciliariosRepository.EliminarDomiciliario (borra los pedidos del domiciliario
                // antes de borrarlo). Como esta es la única ruta de cascada hacia Pedidos, no hay
                // conflicto de múltiples rutas.
                entity.HasOne(e => e.Domiciliario)
                      .WithMany(t => t.Pedidos)
                      .HasForeignKey(e => e.IdDomiciliario)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.ToTable("Pedidos");
            });

            // Configuración de la entidad Tecnicos
            modelBuilder.Entity<Tecnicos>(entity =>
            {
                entity.ToTable("Tecnicos");
                entity.HasKey(e => e.IdTecnico);
                entity.Property(e => e.IdUsuario).IsRequired();
                entity.Property(e => e.FechaRegistro).IsRequired();
                // Un usuario no puede tener más de un perfil de Técnico (mismo razonamiento que
                // el índice único de Domiciliarios.IDUsuario).
                entity.HasIndex(e => e.IdUsuario).IsUnique();
                // RELACIÓN: Usuarios -> Tecnicos. Cascade: mismo razonamiento que Domiciliarios, es
                // un perfil de rol atado al Usuario (UsuariosRepository.EliminarUsuario lo borra
                // junto con la cuenta).
                entity.HasOne(e => e.Usuario)
                      .WithMany(t => t.Tecnico)
                      .HasForeignKey(e => e.IdUsuario)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración de la entidad Tickets
            modelBuilder.Entity<Tickets>(entity =>
            {
                entity.ToTable("Tickets");
                entity.HasKey(e => e.IdTicket);
                entity.Property(e => e.NumeroTicket).ValueGeneratedOnAdd();
                entity.HasIndex(e => e.NumeroTicket).IsUnique();
                entity.Property(e => e.Descripcion).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Categoria).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Prioridad).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Diagnostico).HasMaxLength(500);
                entity.Property(e => e.FallaEncontrada).HasMaxLength(300);
                entity.Property(e => e.PruebasRealizadas).HasMaxLength(300);
                entity.Property(e => e.Observaciones).HasMaxLength(500);
                entity.Property(e => e.Recomendaciones).HasMaxLength(500);
                entity.Property(e => e.DireccionEntrega).HasMaxLength(200);
                entity.Property(e => e.FechaCreacion).IsRequired();
                entity.Property(e => e.IdEstado).IsRequired();
                entity.Property(e => e.IdUsuario).IsRequired();
                // RELACIÓN: Usuarios -> Tickets (cliente). Cascade: así lo hace ya
                // UsuariosRepository.EliminarUsuario (borra los tickets del usuario antes de
                // borrarlo). Es la única ruta de cascada hacia Tickets.
                entity.HasOne(e => e.Usuario)
                      .WithMany(u => u.Tickets)
                      .HasForeignKey(e => e.IdUsuario)
                      .OnDelete(DeleteBehavior.Cascade);
                // RELACIÓN: EstadosTickets -> Tickets. Restrict: es una tabla de referencia (Abierto,
                // Resuelto, etc.); nunca debe poder borrarse un estado arrastrando en cascada todos
                // los tickets que lo usan.
                entity.HasOne(e => e.EstadoTicket)
                      .WithMany(et => et.Tickets)
                      .HasForeignKey(e => e.IdEstado)
                      .OnDelete(DeleteBehavior.Restrict);
                // RELACIÓN: Tecnicos -> Tickets (asignación opcional). ClientSetNull explícito: al
                // borrar el técnico se desasigna el ticket (no se borra), igual que ya hace
                // DomiciliariosRepository.EliminarDomiciliario para el domiciliario. No se usa SetNull
                // real en la BD porque, sumado a que Usuarios->Tecnicos ya es Cascade, volvería a
                // producir múltiples rutas de cascada hacia Tickets.
                entity.HasOne(e => e.Tecnico)
                      .WithMany(t => t.Tickets)
                      .HasForeignKey(e => e.IdTecnico)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.ClientSetNull);
                // RELACIÓN: Domiciliarios -> Tickets (asignación opcional). Mismo razonamiento que
                // Tecnicos -> Tickets.
                entity.HasOne(e => e.Domiciliario)
                      .WithMany(d => d.Tickets)
                      .HasForeignKey(e => e.IdDomiciliario)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.ClientSetNull);
            });

            // Configuracion de la entidad Estados tickets
            modelBuilder.Entity<EstadosTicket>(entity =>
            {
                entity.ToTable("EstadosTickets");
                entity.HasKey(e => e.IdEstado);
                entity.Property(e => e.NombreEstado).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Descripcion).HasMaxLength(200);
                // Nombre de estado único: es una tabla de referencia (Abierto, Resuelto, etc.).
                entity.HasIndex(e => e.NombreEstado).IsUnique();

                // Seed: los 5 estados predeterminados del flujo de un ticket. "Resuelto" debe
                // llamarse exactamente así porque TicketsRepository.ActualizarEstadoTicket y
                // PedidosService.ConfirmarEntregaAsync lo buscan por ese nombre para marcar el
                // ticket como cerrado / la entrega como completada.
                entity.HasData(
                    new { IdEstado = EstadoAbiertoId, NombreEstado = "Abierto", Descripcion = "Ticket recién creado, aún sin atender." },
                    new { IdEstado = EstadoPendienteId, NombreEstado = "Pendiente", Descripcion = "Ticket en espera de diagnóstico o de una acción posterior." },
                    new { IdEstado = EstadoEnPreparacionId, NombreEstado = "En preparacion", Descripcion = "El pedido asociado al ticket se está preparando para su entrega." },
                    new { IdEstado = EstadoEnRutaId, NombreEstado = "En ruta", Descripcion = "El domiciliario va en camino a entregar el pedido." },
                    new { IdEstado = EstadoResueltoId, NombreEstado = "Resuelto", Descripcion = "El ticket fue atendido y cerrado." }
                );
            });

            // Configuración de la entidad ProgresoTickets
            modelBuilder.Entity<ProgresoTickets>(entity =>
            {
                entity.ToTable("ProgresoTickets");
                entity.HasKey(e => e.IdProgreso);
                entity.Property(e => e.IdTicket).IsRequired();
                entity.Property(e => e.Porcentaje).IsRequired();
                entity.Property(e => e.Etapa).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Descripcion).HasMaxLength(500);
                entity.Property(e => e.FechaRegistro).IsRequired();
                // RELACIÓN: ProgresoTickets -> Tickets
                entity.HasOne(e => e.Ticket)
                      .WithMany(t => t.ProgresoTickets)
                      .HasForeignKey(e => e.IdTicket)
                      .OnDelete(DeleteBehavior.Cascade);
                // RELACIÓN: ProgresoTickets -> Tecnicos (opcional; nulo si lo registró un Administrador).
                // ClientSetNull, no SetNull real: Usuarios->Tecnicos ya es Cascade, así que un SetNull
                // real aquí sumado a Usuarios->Tickets->ProgresoTickets (también Cascade) volvería a
                // producir dos rutas de cascada hacia ProgresoTickets (el mismo error de SQL Server
                // "may cause cycles or multiple cascade paths" que se corrigió para Domiciliarios/
                // Tecnicos -> Tickets).
                entity.HasOne(e => e.Tecnico)
                      .WithMany()
                      .HasForeignKey(e => e.IdTecnico)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.ClientSetNull);
            });
            base.OnModelCreating(modelBuilder);
        }
    }
}
