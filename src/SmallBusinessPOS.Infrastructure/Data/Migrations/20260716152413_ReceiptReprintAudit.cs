using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmallBusinessPOS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReceiptReprintAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReceiptReprintAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaleNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReprintedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReprintedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptReprintAudits", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptReprintAudits_BusinessId_BranchId_ReprintedAtUtc",
                table: "ReceiptReprintAudits",
                columns: new[] { "BusinessId", "BranchId", "ReprintedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptReprintAudits_SaleId_ReprintedAtUtc",
                table: "ReceiptReprintAudits",
                columns: new[] { "SaleId", "ReprintedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReceiptReprintAudits");
        }
    }
}
