using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmallBusinessPOS.Infrastructure.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260722010000_CustomersCreditAndPartialReturns")]
public partial class CustomersCreditAndPartialReturns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Customers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                DocumentNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Customers", x => x.Id);
                table.ForeignKey(
                    name: "FK_Customers_Businesses_BusinessId",
                    column: x => x.BusinessId,
                    principalTable: "Businesses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.AddColumn<Guid>(
            name: "CustomerId",
            table: "Sales",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "SaleReturns",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CashSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ReturnNumber = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                RefundReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                ReturnedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SaleReturns", x => x.Id);
                table.ForeignKey(
                    name: "FK_SaleReturns_Branches_BranchId",
                    column: x => x.BranchId,
                    principalTable: "Branches",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SaleReturns_Businesses_BusinessId",
                    column: x => x.BusinessId,
                    principalTable: "Businesses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SaleReturns_CashSessions_CashSessionId",
                    column: x => x.CashSessionId,
                    principalTable: "CashSessions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_SaleReturns_Sales_SaleId",
                    column: x => x.SaleId,
                    principalTable: "Sales",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "SaleReturnDetails",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SaleReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SaleDetailId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProductCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SaleReturnDetails", x => x.Id);
                table.ForeignKey(
                    name: "FK_SaleReturnDetails_Products_ProductId",
                    column: x => x.ProductId,
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SaleReturnDetails_SaleDetails_SaleDetailId",
                    column: x => x.SaleDetailId,
                    principalTable: "SaleDetails",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SaleReturnDetails_SaleReturns_SaleReturnId",
                    column: x => x.SaleReturnId,
                    principalTable: "SaleReturns",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_Sales_CustomerId", table: "Sales", column: "CustomerId");
        migrationBuilder.CreateIndex(name: "IX_Customers_BusinessId_Name", table: "Customers", columns: new[] { "BusinessId", "Name" });
        migrationBuilder.CreateIndex(name: "IX_Customers_BusinessId_DocumentNumber", table: "Customers", columns: new[] { "BusinessId", "DocumentNumber" }, unique: true, filter: "[DocumentNumber] IS NOT NULL");
        migrationBuilder.CreateIndex(name: "IX_Customers_IsActive", table: "Customers", column: "IsActive");
        migrationBuilder.CreateIndex(name: "IX_SaleReturns_BranchId", table: "SaleReturns", column: "BranchId");
        migrationBuilder.CreateIndex(name: "IX_SaleReturns_BusinessId", table: "SaleReturns", column: "BusinessId");
        migrationBuilder.CreateIndex(name: "IX_SaleReturns_CashSessionId", table: "SaleReturns", column: "CashSessionId");
        migrationBuilder.CreateIndex(name: "IX_SaleReturns_ReturnNumber", table: "SaleReturns", column: "ReturnNumber", unique: true);
        migrationBuilder.CreateIndex(name: "IX_SaleReturns_SaleId_ReturnedAtUtc", table: "SaleReturns", columns: new[] { "SaleId", "ReturnedAtUtc" });
        migrationBuilder.CreateIndex(name: "IX_SaleReturnDetails_ProductId", table: "SaleReturnDetails", column: "ProductId");
        migrationBuilder.CreateIndex(name: "IX_SaleReturnDetails_SaleDetailId", table: "SaleReturnDetails", column: "SaleDetailId");
        migrationBuilder.CreateIndex(name: "IX_SaleReturnDetails_SaleReturnId", table: "SaleReturnDetails", column: "SaleReturnId");

        migrationBuilder.AddForeignKey(
            name: "FK_Sales_Customers_CustomerId",
            table: "Sales",
            column: "CustomerId",
            principalTable: "Customers",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.Sql("""
            INSERT INTO PaymentMethods (Id, BusinessId, Code, Name, Type, IsActive, CreatedAtUtc, CreatedBy)
            SELECT NEWID(), b.Id, 'CREDIT', 'Credito cliente', 6, 1, SYSUTCDATETIME(), 'migration'
            FROM Businesses b
            WHERE NOT EXISTS (
                SELECT 1 FROM PaymentMethods pm
                WHERE pm.BusinessId = b.Id AND pm.Code = 'CREDIT'
            )
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_Sales_Customers_CustomerId", table: "Sales");
        migrationBuilder.DropTable(name: "SaleReturnDetails");
        migrationBuilder.DropTable(name: "SaleReturns");
        migrationBuilder.DropTable(name: "Customers");
        migrationBuilder.DropIndex(name: "IX_Sales_CustomerId", table: "Sales");
        migrationBuilder.DropColumn(name: "CustomerId", table: "Sales");
        migrationBuilder.Sql("DELETE FROM PaymentMethods WHERE Code = 'CREDIT' AND CreatedBy = 'migration'");
    }
}
