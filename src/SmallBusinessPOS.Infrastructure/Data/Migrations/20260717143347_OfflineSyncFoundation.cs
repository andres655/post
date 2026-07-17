using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmallBusinessPOS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class OfflineSyncFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AggregateType",
                table: "OutboxMessages",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MaxRetries",
                table: "OutboxMessages",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<DateTime>(
                name: "OccurredAtUtc",
                table: "OutboxMessages",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "OutboxMessages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE OutboxMessages
                SET AggregateType = EventType
                WHERE AggregateType = '';
                """);

            migrationBuilder.Sql("""
                UPDATE OutboxMessages
                SET OccurredAtUtc = CreatedAtUtc
                WHERE OccurredAtUtc = '0001-01-01T00:00:00.0000000';
                """);

            migrationBuilder.Sql("""
                UPDATE OutboxMessages
                SET MaxRetries = 3
                WHERE MaxRetries <= 0;
                """);

            migrationBuilder.CreateTable(
                name: "SyncQueueItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EntityName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Operation = table.Column<int>(type: "int", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastAttemptAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SyncedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncQueueItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_BusinessId_Status_CreatedAtUtc",
                table: "OutboxMessages",
                columns: new[] { "BusinessId", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_OccurredAtUtc",
                table: "OutboxMessages",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SyncQueueItems_BusinessId_Status_Priority_CreatedAtUtc",
                table: "SyncQueueItems",
                columns: new[] { "BusinessId", "Status", "Priority", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncQueueItems_EntityName_EntityId_Operation",
                table: "SyncQueueItems",
                columns: new[] { "EntityName", "EntityId", "Operation" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncQueueItems_LastAttemptAtUtc",
                table: "SyncQueueItems",
                column: "LastAttemptAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncQueueItems");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_BusinessId_Status_CreatedAtUtc",
                table: "OutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_OccurredAtUtc",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "AggregateType",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "MaxRetries",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "OccurredAtUtc",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "OutboxMessages");
        }
    }
}
