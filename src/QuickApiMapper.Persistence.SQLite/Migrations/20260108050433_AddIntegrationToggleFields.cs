using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickApiMapper.Persistence.SQLite.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegrationToggleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GlobalToggles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalToggles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Endpoint = table.Column<string>(type: "TEXT", nullable: false),
                    SourceType = table.Column<string>(type: "TEXT", nullable: false),
                    DestinationType = table.Column<string>(type: "TEXT", nullable: false),
                    DestinationUrl = table.Column<string>(type: "TEXT", nullable: false),
                    DispatchFor = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    EnableInput = table.Column<bool>(type: "INTEGER", nullable: false),
                    EnableOutput = table.Column<bool>(type: "INTEGER", nullable: false),
                    EnableMessageCapture = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FieldMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IntegrationMappingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    Destination = table.Column<string>(type: "TEXT", nullable: true),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldMappings_IntegrationMappings_IntegrationMappingId",
                        column: x => x.IntegrationMappingId,
                        principalTable: "IntegrationMappings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GrpcConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IntegrationMappingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServiceName = table.Column<string>(type: "TEXT", nullable: true),
                    MethodName = table.Column<string>(type: "TEXT", nullable: true),
                    ProtoFile = table.Column<string>(type: "TEXT", nullable: true),
                    Package = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrpcConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GrpcConfigurations_IntegrationMappings_IntegrationMappingId",
                        column: x => x.IntegrationMappingId,
                        principalTable: "IntegrationMappings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RabbitMqConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IntegrationMappingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    HostName = table.Column<string>(type: "TEXT", nullable: true),
                    Port = table.Column<int>(type: "INTEGER", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    Password = table.Column<string>(type: "TEXT", nullable: true),
                    VirtualHost = table.Column<string>(type: "TEXT", nullable: true),
                    SourceExchange = table.Column<string>(type: "TEXT", nullable: true),
                    SourceQueue = table.Column<string>(type: "TEXT", nullable: true),
                    SourceRoutingKey = table.Column<string>(type: "TEXT", nullable: true),
                    DestinationExchange = table.Column<string>(type: "TEXT", nullable: true),
                    DestinationRoutingKey = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RabbitMqConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RabbitMqConfigurations_IntegrationMappings_IntegrationMappingId",
                        column: x => x.IntegrationMappingId,
                        principalTable: "IntegrationMappings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceBusConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IntegrationMappingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ConnectionString = table.Column<string>(type: "TEXT", nullable: true),
                    SourceQueue = table.Column<string>(type: "TEXT", nullable: true),
                    SourceTopic = table.Column<string>(type: "TEXT", nullable: true),
                    SourceSubscription = table.Column<string>(type: "TEXT", nullable: true),
                    DestinationQueue = table.Column<string>(type: "TEXT", nullable: true),
                    DestinationTopic = table.Column<string>(type: "TEXT", nullable: true),
                    MessageType = table.Column<string>(type: "TEXT", nullable: true),
                    AutoComplete = table.Column<bool>(type: "INTEGER", nullable: false),
                    MaxConcurrentCalls = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceBusConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceBusConfigurations_IntegrationMappings_IntegrationMappingId",
                        column: x => x.IntegrationMappingId,
                        principalTable: "IntegrationMappings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SoapConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IntegrationMappingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BodyWrapperFieldXPath = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoapConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SoapConfigurations_IntegrationMappings_IntegrationMappingId",
                        column: x => x.IntegrationMappingId,
                        principalTable: "IntegrationMappings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StaticValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IntegrationMappingId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    IsGlobal = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaticValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaticValues_IntegrationMappings_IntegrationMappingId",
                        column: x => x.IntegrationMappingId,
                        principalTable: "IntegrationMappings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Transformers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FieldMappingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Arguments = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transformers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transformers_FieldMappings_FieldMappingId",
                        column: x => x.FieldMappingId,
                        principalTable: "FieldMappings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SoapFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SoapConfigId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FieldType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    XPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Namespace = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Prefix = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Attributes = table.Column<string>(type: "TEXT", nullable: true),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoapFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SoapFields_SoapConfigurations_SoapConfigId",
                        column: x => x.SoapConfigId,
                        principalTable: "SoapConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FieldMappings_IntegrationMappingId",
                table: "FieldMappings",
                column: "IntegrationMappingId");

            migrationBuilder.CreateIndex(
                name: "IX_GlobalToggles_Key",
                table: "GlobalToggles",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GrpcConfigurations_IntegrationMappingId",
                table: "GrpcConfigurations",
                column: "IntegrationMappingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RabbitMqConfigurations_IntegrationMappingId",
                table: "RabbitMqConfigurations",
                column: "IntegrationMappingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceBusConfigurations_IntegrationMappingId",
                table: "ServiceBusConfigurations",
                column: "IntegrationMappingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SoapConfigurations_IntegrationMappingId",
                table: "SoapConfigurations",
                column: "IntegrationMappingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SoapFields_SoapConfigId_Order",
                table: "SoapFields",
                columns: new[] { "SoapConfigId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_StaticValues_IntegrationMappingId",
                table: "StaticValues",
                column: "IntegrationMappingId");

            migrationBuilder.CreateIndex(
                name: "IX_Transformers_FieldMappingId_Order",
                table: "Transformers",
                columns: new[] { "FieldMappingId", "Order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GlobalToggles");

            migrationBuilder.DropTable(
                name: "GrpcConfigurations");

            migrationBuilder.DropTable(
                name: "RabbitMqConfigurations");

            migrationBuilder.DropTable(
                name: "ServiceBusConfigurations");

            migrationBuilder.DropTable(
                name: "SoapFields");

            migrationBuilder.DropTable(
                name: "StaticValues");

            migrationBuilder.DropTable(
                name: "Transformers");

            migrationBuilder.DropTable(
                name: "SoapConfigurations");

            migrationBuilder.DropTable(
                name: "FieldMappings");

            migrationBuilder.DropTable(
                name: "IntegrationMappings");
        }
    }
}
