using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Darkhorse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "data_history",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Exchange = table.Column<string>(type: "text", nullable: false),
                    Symbol = table.Column<string>(type: "text", nullable: false),
                    Timeframe = table.Column<string>(type: "text", nullable: false),
                    Ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Open = table.Column<decimal>(type: "numeric(24,8)", precision: 24, scale: 8, nullable: false),
                    High = table.Column<decimal>(type: "numeric(24,8)", precision: 24, scale: 8, nullable: false),
                    Low = table.Column<decimal>(type: "numeric(24,8)", precision: 24, scale: 8, nullable: false),
                    Close = table.Column<decimal>(type: "numeric(24,8)", precision: 24, scale: 8, nullable: false),
                    Volume = table.Column<decimal>(type: "numeric(32,8)", precision: 32, scale: 8, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_data_history", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    PasswordSalt = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationPreferences = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "text", nullable: false),
                    EntityType = table.Column<string>(type: "text", nullable: true),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_audit_logs_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "broker_credentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BrokerName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ApiKeyNonce = table.Column<byte[]>(type: "bytea", nullable: false),
                    ApiKeyCipher = table.Column<byte[]>(type: "bytea", nullable: false),
                    ApiKeyTag = table.Column<byte[]>(type: "bytea", nullable: false),
                    SecretNonce = table.Column<byte[]>(type: "bytea", nullable: false),
                    SecretCipher = table.Column<byte[]>(type: "bytea", nullable: false),
                    SecretTag = table.Column<byte[]>(type: "bytea", nullable: false),
                    KeyVersion = table.Column<int>(type: "integer", nullable: false),
                    FeeRate = table.Column<decimal>(type: "numeric(8,6)", precision: 8, scale: 6, nullable: false),
                    FundingRate = table.Column<decimal>(type: "numeric(8,6)", precision: 8, scale: 6, nullable: false),
                    IsSandbox = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LastTestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_broker_credentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_broker_credentials_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notifications_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "strategies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CredentialId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Symbol = table.Column<string>(type: "text", nullable: false),
                    Timeframe = table.Column<string>(type: "text", nullable: false),
                    Script = table.Column<string>(type: "text", nullable: false),
                    ScriptVersion = table.Column<int>(type: "integer", nullable: false),
                    Parameters = table.Column<string>(type: "jsonb", nullable: false),
                    Mode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    MaxPositionSize = table.Column<decimal>(type: "numeric(24,8)", precision: 24, scale: 8, nullable: true),
                    MaxDailyVolume = table.Column<decimal>(type: "numeric(24,8)", precision: 24, scale: 8, nullable: true),
                    CircuitState = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CircuitFailures = table.Column<int>(type: "integer", nullable: false),
                    CircuitOpenedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ScheduleInterval = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_strategies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_strategies_broker_credentials_CredentialId",
                        column: x => x.CredentialId,
                        principalTable: "broker_credentials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_strategies_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "executions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StrategyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScriptVersion = table.Column<int>(type: "integer", nullable: true),
                    Signal = table.Column<string>(type: "text", nullable: false),
                    SignalReason = table.Column<string>(type: "text", nullable: true),
                    Mode = table.Column<string>(type: "text", nullable: false),
                    ContextSnapshot = table.Column<string>(type: "jsonb", nullable: true),
                    OutputRaw = table.Column<string>(type: "jsonb", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    DurationMs = table.Column<int>(type: "integer", nullable: true),
                    ContainerExitCode = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_executions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_executions_strategies_StrategyId",
                        column: x => x.StrategyId,
                        principalTable: "strategies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StrategyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BrokerOrderId = table.Column<string>(type: "text", nullable: true),
                    Symbol = table.Column<string>(type: "text", nullable: false),
                    Side = table.Column<string>(type: "text", nullable: false),
                    OrderType = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(24,8)", precision: 24, scale: 8, nullable: false),
                    RequestedPrice = table.Column<decimal>(type: "numeric(24,8)", precision: 24, scale: 8, nullable: true),
                    FillPrice = table.Column<decimal>(type: "numeric(24,8)", precision: 24, scale: 8, nullable: true),
                    FillQuantity = table.Column<decimal>(type: "numeric(24,8)", precision: 24, scale: 8, nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Mode = table.Column<string>(type: "text", nullable: false),
                    Fees = table.Column<decimal>(type: "numeric(24,8)", precision: 24, scale: 8, nullable: true),
                    FeeCurrency = table.Column<string>(type: "text", nullable: true),
                    SignalReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FilledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_orders_strategies_StrategyId",
                        column: x => x.StrategyId,
                        principalTable: "strategies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_orders_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "strategy_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StrategyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Script = table.Column<string>(type: "text", nullable: false),
                    Parameters = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_strategy_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_strategy_versions_strategies_StrategyId",
                        column: x => x.StrategyId,
                        principalTable: "strategies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_UserId",
                table: "audit_logs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_broker_credentials_UserId",
                table: "broker_credentials",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_data_history_Exchange_Symbol_Timeframe_Ts",
                table: "data_history",
                columns: new[] { "Exchange", "Symbol", "Timeframe", "Ts" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_executions_StrategyId",
                table: "executions",
                column: "StrategyId");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_UserId",
                table: "notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_orders_StrategyId",
                table: "orders",
                column: "StrategyId");

            migrationBuilder.CreateIndex(
                name: "IX_orders_UserId",
                table: "orders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_strategies_CredentialId",
                table: "strategies",
                column: "CredentialId");

            migrationBuilder.CreateIndex(
                name: "IX_strategies_UserId",
                table: "strategies",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_strategy_versions_StrategyId_Version",
                table: "strategy_versions",
                columns: new[] { "StrategyId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "data_history");

            migrationBuilder.DropTable(
                name: "executions");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "strategy_versions");

            migrationBuilder.DropTable(
                name: "strategies");

            migrationBuilder.DropTable(
                name: "broker_credentials");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
