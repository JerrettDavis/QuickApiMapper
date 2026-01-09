using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickApiMapper.Persistence.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegrationToggleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "global_toggles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_global_toggles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "integrationmappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Endpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DestinationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DestinationUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DispatchFor = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    EnableInput = table.Column<bool>(type: "boolean", nullable: false),
                    EnableOutput = table.Column<bool>(type: "boolean", nullable: false),
                    EnableMessageCapture = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_integrationmappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "fieldmappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IntegrationMappingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Destination = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fieldmappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fieldmappings_integrationmappings_IntegrationMappingId",
                        column: x => x.IntegrationMappingId,
                        principalTable: "integrationmappings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "grpcconfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IntegrationMappingId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "text", nullable: true),
                    MethodName = table.Column<string>(type: "text", nullable: true),
                    ProtoFile = table.Column<string>(type: "text", nullable: true),
                    Package = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grpcconfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_grpcconfigurations_integrationmappings_IntegrationMappingId",
                        column: x => x.IntegrationMappingId,
                        principalTable: "integrationmappings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rabbitmqconfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IntegrationMappingId = table.Column<Guid>(type: "uuid", nullable: false),
                    HostName = table.Column<string>(type: "text", nullable: true),
                    Port = table.Column<int>(type: "integer", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    Password = table.Column<string>(type: "text", nullable: true),
                    VirtualHost = table.Column<string>(type: "text", nullable: true),
                    SourceExchange = table.Column<string>(type: "text", nullable: true),
                    SourceQueue = table.Column<string>(type: "text", nullable: true),
                    SourceRoutingKey = table.Column<string>(type: "text", nullable: true),
                    DestinationExchange = table.Column<string>(type: "text", nullable: true),
                    DestinationRoutingKey = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rabbitmqconfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rabbitmqconfigurations_integrationmappings_IntegrationMappi~",
                        column: x => x.IntegrationMappingId,
                        principalTable: "integrationmappings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "servicebusconfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IntegrationMappingId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionString = table.Column<string>(type: "text", nullable: true),
                    SourceQueue = table.Column<string>(type: "text", nullable: true),
                    SourceTopic = table.Column<string>(type: "text", nullable: true),
                    SourceSubscription = table.Column<string>(type: "text", nullable: true),
                    DestinationQueue = table.Column<string>(type: "text", nullable: true),
                    DestinationTopic = table.Column<string>(type: "text", nullable: true),
                    MessageType = table.Column<string>(type: "text", nullable: true),
                    AutoComplete = table.Column<bool>(type: "boolean", nullable: false),
                    MaxConcurrentCalls = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_servicebusconfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_servicebusconfigurations_integrationmappings_IntegrationMap~",
                        column: x => x.IntegrationMappingId,
                        principalTable: "integrationmappings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "soapconfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IntegrationMappingId = table.Column<Guid>(type: "uuid", nullable: false),
                    BodyWrapperFieldXPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_soapconfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_soapconfigurations_integrationmappings_IntegrationMappingId",
                        column: x => x.IntegrationMappingId,
                        principalTable: "integrationmappings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "staticvalues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IntegrationMappingId = table.Column<Guid>(type: "uuid", nullable: true),
                    Key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    IsGlobal = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staticvalues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_staticvalues_integrationmappings_IntegrationMappingId",
                        column: x => x.IntegrationMappingId,
                        principalTable: "integrationmappings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "transformers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldMappingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Arguments = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transformers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_transformers_fieldmappings_FieldMappingId",
                        column: x => x.FieldMappingId,
                        principalTable: "fieldmappings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "soapfields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SoapConfigId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    XPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Source = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Namespace = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Prefix = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Attributes = table.Column<string>(type: "jsonb", nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_soapfields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_soapfields_soapconfigurations_SoapConfigId",
                        column: x => x.SoapConfigId,
                        principalTable: "soapconfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fieldmappings_IntegrationMappingId_Order",
                table: "fieldmappings",
                columns: new[] { "IntegrationMappingId", "Order" });

            migrationBuilder.CreateIndex(
                name: "ix_global_toggles_key",
                table: "global_toggles",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_grpcconfigurations_IntegrationMappingId",
                table: "grpcconfigurations",
                column: "IntegrationMappingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_integrationmappings_Endpoint",
                table: "integrationmappings",
                column: "Endpoint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_integrationmappings_IsActive",
                table: "integrationmappings",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_integrationmappings_Name",
                table: "integrationmappings",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_rabbitmqconfigurations_IntegrationMappingId",
                table: "rabbitmqconfigurations",
                column: "IntegrationMappingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_servicebusconfigurations_IntegrationMappingId",
                table: "servicebusconfigurations",
                column: "IntegrationMappingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_soapconfigurations_IntegrationMappingId",
                table: "soapconfigurations",
                column: "IntegrationMappingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_soapfields_SoapConfigId_Order",
                table: "soapfields",
                columns: new[] { "SoapConfigId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_staticvalues_IntegrationMappingId_Key",
                table: "staticvalues",
                columns: new[] { "IntegrationMappingId", "Key" });

            migrationBuilder.CreateIndex(
                name: "IX_staticvalues_IsGlobal",
                table: "staticvalues",
                column: "IsGlobal");

            migrationBuilder.CreateIndex(
                name: "IX_transformers_FieldMappingId_Order",
                table: "transformers",
                columns: new[] { "FieldMappingId", "Order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "global_toggles");

            migrationBuilder.DropTable(
                name: "grpcconfigurations");

            migrationBuilder.DropTable(
                name: "rabbitmqconfigurations");

            migrationBuilder.DropTable(
                name: "servicebusconfigurations");

            migrationBuilder.DropTable(
                name: "soapfields");

            migrationBuilder.DropTable(
                name: "staticvalues");

            migrationBuilder.DropTable(
                name: "transformers");

            migrationBuilder.DropTable(
                name: "soapconfigurations");

            migrationBuilder.DropTable(
                name: "fieldmappings");

            migrationBuilder.DropTable(
                name: "integrationmappings");
        }
    }
}
