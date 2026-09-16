using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SkyHelp.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EstadosTickets",
                columns: table => new
                {
                    IdEstado = table.Column<Guid>(type: "uuid", nullable: false),
                    NombreEstado = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstadosTickets", x => x.IdEstado);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    IDRol = table.Column<Guid>(type: "uuid", nullable: false),
                    NombreRol = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.IDRol);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    IdUsuario = table.Column<Guid>(type: "uuid", nullable: false),
                    IdRol = table.Column<Guid>(type: "uuid", nullable: false),
                    NombreUsuarios = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NombreCompleto = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Correo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Contrasena = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EstadoCuenta = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Telefono = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.IdUsuario);
                    table.ForeignKey(
                        name: "FK_Usuarios_Roles_IdRol",
                        column: x => x.IdRol,
                        principalTable: "Roles",
                        principalColumn: "IDRol",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Auditoria",
                columns: table => new
                {
                    IDLog = table.Column<Guid>(type: "uuid", nullable: false),
                    IDUsuario = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoEvento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TablaAfectada = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IDRegistro = table.Column<Guid>(type: "uuid", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    FechaEvento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DireccionIp = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auditoria", x => x.IDLog);
                    table.ForeignKey(
                        name: "FK_Auditoria_Usuarios_IDUsuario",
                        column: x => x.IDUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Domiciliarios",
                columns: table => new
                {
                    IdDomiciliario = table.Column<Guid>(type: "uuid", nullable: false),
                    NombreCompleto = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Telefono = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EstadoActividad = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IDUsuario = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Domiciliarios", x => x.IdDomiciliario);
                    table.ForeignKey(
                        name: "FK_Domiciliarios_Usuarios_IDUsuario",
                        column: x => x.IDUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reportes",
                columns: table => new
                {
                    IdReporte = table.Column<Guid>(type: "uuid", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TipoReporte = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FechaGeneracion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IdUsuario = table.Column<Guid>(type: "uuid", nullable: false),
                    IdOrigen = table.Column<Guid>(type: "uuid", nullable: false),
                    OrigenTabla = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Datos = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reportes", x => x.IdReporte);
                    table.ForeignKey(
                        name: "FK_Reportes_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Tecnicos",
                columns: table => new
                {
                    IdTecnico = table.Column<Guid>(type: "uuid", nullable: false),
                    IdUsuario = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tecnicos", x => x.IdTecnico);
                    table.ForeignKey(
                        name: "FK_Tecnicos_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pedidos",
                columns: table => new
                {
                    IdPedido = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroPedido = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdUsuario = table.Column<Guid>(type: "uuid", nullable: false),
                    IdDomiciliario = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaPedido = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DireccionEntrega = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EstadoPedido = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IdTicket = table.Column<Guid>(type: "uuid", nullable: true),
                    FechaEntrega = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pedidos", x => x.IdPedido);
                    table.ForeignKey(
                        name: "FK_Pedidos_Domiciliarios_IdDomiciliario",
                        column: x => x.IdDomiciliario,
                        principalTable: "Domiciliarios",
                        principalColumn: "IdDomiciliario",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Pedidos_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Tickets",
                columns: table => new
                {
                    IdTicket = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroTicket = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Categoria = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Prioridad = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaCierre = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Diagnostico = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FechaDiagnostico = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FallaEncontrada = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    PruebasRealizadas = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Recomendaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DireccionEntrega = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IdEstado = table.Column<Guid>(type: "uuid", nullable: false),
                    IdUsuario = table.Column<Guid>(type: "uuid", nullable: false),
                    IdDomiciliario = table.Column<Guid>(type: "uuid", nullable: true),
                    IdTecnico = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tickets", x => x.IdTicket);
                    table.ForeignKey(
                        name: "FK_Tickets_Domiciliarios_IdDomiciliario",
                        column: x => x.IdDomiciliario,
                        principalTable: "Domiciliarios",
                        principalColumn: "IdDomiciliario");
                    table.ForeignKey(
                        name: "FK_Tickets_EstadosTickets_IdEstado",
                        column: x => x.IdEstado,
                        principalTable: "EstadosTickets",
                        principalColumn: "IdEstado",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tickets_Tecnicos_IdTecnico",
                        column: x => x.IdTecnico,
                        principalTable: "Tecnicos",
                        principalColumn: "IdTecnico");
                    table.ForeignKey(
                        name: "FK_Tickets_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProgresoTickets",
                columns: table => new
                {
                    IdProgreso = table.Column<Guid>(type: "uuid", nullable: false),
                    IdTicket = table.Column<Guid>(type: "uuid", nullable: false),
                    Porcentaje = table.Column<int>(type: "integer", nullable: false),
                    Etapa = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IdTecnico = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgresoTickets", x => x.IdProgreso);
                    table.ForeignKey(
                        name: "FK_ProgresoTickets_Tecnicos_IdTecnico",
                        column: x => x.IdTecnico,
                        principalTable: "Tecnicos",
                        principalColumn: "IdTecnico");
                    table.ForeignKey(
                        name: "FK_ProgresoTickets_Tickets_IdTicket",
                        column: x => x.IdTicket,
                        principalTable: "Tickets",
                        principalColumn: "IdTicket",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "EstadosTickets",
                columns: new[] { "IdEstado", "Descripcion", "NombreEstado" },
                values: new object[,]
                {
                    { new Guid("66666666-6666-6666-6666-666666666666"), "Ticket recién creado, aún sin atender.", "Abierto" },
                    { new Guid("77777777-7777-7777-7777-777777777777"), "Ticket en espera de diagnóstico o de una acción posterior.", "Pendiente" },
                    { new Guid("88888888-8888-8888-8888-888888888888"), "El pedido asociado al ticket se está preparando para su entrega.", "En preparacion" },
                    { new Guid("99999999-9999-9999-9999-999999999999"), "El domiciliario va en camino a entregar el pedido.", "En ruta" },
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "El ticket fue atendido y cerrado.", "Resuelto" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "IDRol", "Descripcion", "NombreRol" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "Administrador del sistema, con acceso total.", "Administrador" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "Técnico encargado del diagnóstico y soporte de tickets.", "Tecnico" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "Domiciliario encargado de la entrega de pedidos.", "Domiciliario" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "Cliente que reporta tickets de soporte.", "Usuario" }
                });

            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "IdUsuario", "Contrasena", "Correo", "EstadoCuenta", "IdRol", "NombreCompleto", "NombreUsuarios", "Telefono" },
                values: new object[] { new Guid("55555555-5555-5555-5555-555555555555"), "$2a$11$iZtHw38ztzeNAfWiTfmQ9e8aig.dwYG/xb60VDRNYgZI5UKIPIemO", "admin@skyhelp.com", "Activo", new Guid("11111111-1111-1111-1111-111111111111"), "Administrador SkyHelp", "admin", null });

            migrationBuilder.CreateIndex(
                name: "IX_Auditoria_IDUsuario",
                table: "Auditoria",
                column: "IDUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Domiciliarios_IDUsuario",
                table: "Domiciliarios",
                column: "IDUsuario",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EstadosTickets_NombreEstado",
                table: "EstadosTickets",
                column: "NombreEstado",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_IdDomiciliario",
                table: "Pedidos",
                column: "IdDomiciliario");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_IdUsuario",
                table: "Pedidos",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_NumeroPedido",
                table: "Pedidos",
                column: "NumeroPedido",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgresoTickets_IdTecnico",
                table: "ProgresoTickets",
                column: "IdTecnico");

            migrationBuilder.CreateIndex(
                name: "IX_ProgresoTickets_IdTicket",
                table: "ProgresoTickets",
                column: "IdTicket");

            migrationBuilder.CreateIndex(
                name: "IX_Reportes_IdUsuario",
                table: "Reportes",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_NombreRol",
                table: "Roles",
                column: "NombreRol",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tecnicos_IdUsuario",
                table: "Tecnicos",
                column: "IdUsuario",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_IdDomiciliario",
                table: "Tickets",
                column: "IdDomiciliario");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_IdEstado",
                table: "Tickets",
                column: "IdEstado");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_IdTecnico",
                table: "Tickets",
                column: "IdTecnico");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_IdUsuario",
                table: "Tickets",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_NumeroTicket",
                table: "Tickets",
                column: "NumeroTicket",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Correo",
                table: "Usuarios",
                column: "Correo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_IdRol",
                table: "Usuarios",
                column: "IdRol");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Auditoria");

            migrationBuilder.DropTable(
                name: "Pedidos");

            migrationBuilder.DropTable(
                name: "ProgresoTickets");

            migrationBuilder.DropTable(
                name: "Reportes");

            migrationBuilder.DropTable(
                name: "Tickets");

            migrationBuilder.DropTable(
                name: "Domiciliarios");

            migrationBuilder.DropTable(
                name: "EstadosTickets");

            migrationBuilder.DropTable(
                name: "Tecnicos");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
