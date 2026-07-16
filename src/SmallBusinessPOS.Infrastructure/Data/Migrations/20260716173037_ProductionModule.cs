using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmallBusinessPOS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProductionModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductionEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ProductionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConfirmedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionEntries_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionEntries_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionEntryDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductionEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantityProduced = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionEntryDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionEntryDetails_ProductionEntries_ProductionEntryId",
                        column: x => x.ProductionEntryId,
                        principalTable: "ProductionEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionEntryDetails_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionEntries_BranchId",
                table: "ProductionEntries",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionEntries_BusinessId_BranchId_ProductionDate",
                table: "ProductionEntries",
                columns: new[] { "BusinessId", "BranchId", "ProductionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionEntries_BusinessId_Number",
                table: "ProductionEntries",
                columns: new[] { "BusinessId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionEntries_Status",
                table: "ProductionEntries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionEntryDetails_ProductId",
                table: "ProductionEntryDetails",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionEntryDetails_ProductionEntryId",
                table: "ProductionEntryDetails",
                column: "ProductionEntryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductionEntryDetails");

            migrationBuilder.DropTable(
                name: "ProductionEntries");
        }
    }
}
