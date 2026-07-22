using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmallBusinessPOS.Infrastructure.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260722020000_ExpenseAndProductTypeCatalogs")]
public partial class ExpenseAndProductTypeCatalogs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ExpenseCategories",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                SortOrder = table.Column<int>(type: "int", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ExpenseCategories", x => x.Id);
                table.ForeignKey(
                    name: "FK_ExpenseCategories_Businesses_BusinessId",
                    column: x => x.BusinessId,
                    principalTable: "Businesses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ProductTypeOptions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Value = table.Column<int>(type: "int", nullable: false),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                SortOrder = table.Column<int>(type: "int", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProductTypeOptions", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProductTypeOptions_Businesses_BusinessId",
                    column: x => x.BusinessId,
                    principalTable: "Businesses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.AddColumn<Guid>(
            name: "ExpenseCategoryId",
            table: "Expenses",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.CreateIndex(name: "IX_Expenses_ExpenseCategoryId", table: "Expenses", column: "ExpenseCategoryId");
        migrationBuilder.CreateIndex(name: "IX_ExpenseCategories_BusinessId_IsActive", table: "ExpenseCategories", columns: new[] { "BusinessId", "IsActive" });
        migrationBuilder.CreateIndex(name: "IX_ExpenseCategories_BusinessId_Name", table: "ExpenseCategories", columns: new[] { "BusinessId", "Name" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_ExpenseCategories_BusinessId_SortOrder", table: "ExpenseCategories", columns: new[] { "BusinessId", "SortOrder" });
        migrationBuilder.CreateIndex(name: "IX_ProductTypeOptions_BusinessId_IsActive", table: "ProductTypeOptions", columns: new[] { "BusinessId", "IsActive" });
        migrationBuilder.CreateIndex(name: "IX_ProductTypeOptions_BusinessId_SortOrder", table: "ProductTypeOptions", columns: new[] { "BusinessId", "SortOrder" });
        migrationBuilder.CreateIndex(name: "IX_ProductTypeOptions_BusinessId_Value", table: "ProductTypeOptions", columns: new[] { "BusinessId", "Value" }, unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_Expenses_ExpenseCategories_ExpenseCategoryId",
            table: "Expenses",
            column: "ExpenseCategoryId",
            principalTable: "ExpenseCategories",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.Sql("""
            INSERT INTO ProductTypeOptions (Id, BusinessId, Value, Name, Description, SortOrder, IsActive, CreatedAtUtc, CreatedBy)
            SELECT NEWID(), b.Id, v.Value, v.Name, v.Description, v.SortOrder, 1, SYSUTCDATETIME(), 'migration'
            FROM Businesses b
            CROSS APPLY (VALUES
                (1, N'Estandar', N'Producto de venta directa', 1),
                (2, N'Preparado', N'Producto preparado o porcionado', 2),
                (3, N'Combo', N'Paquete compuesto por varios productos', 3),
                (4, N'Servicio', N'Servicio sin inventario', 4),
                (5, N'Ingrediente', N'Insumo de produccion', 5),
                (6, N'Empaque', N'Material de empaque', 6)
            ) v(Value, Name, Description, SortOrder)
            WHERE NOT EXISTS (
                SELECT 1 FROM ProductTypeOptions pto
                WHERE pto.BusinessId = b.Id AND pto.Value = v.Value
            )
            """);

        migrationBuilder.Sql("""
            INSERT INTO ExpenseCategories (Id, BusinessId, Name, Description, SortOrder, IsActive, CreatedAtUtc, CreatedBy)
            SELECT NEWID(), b.Id, v.Name, v.Description, v.SortOrder, 1, SYSUTCDATETIME(), 'migration'
            FROM Businesses b
            CROSS APPLY (VALUES
                (N'Operativo', N'Gastos generales de operacion', 1),
                (N'Servicios', N'Electricidad, agua, internet y telefono', 2),
                (N'Combustible', N'Gas, gasolina y transporte', 3),
                (N'Mantenimiento', N'Reparaciones y mantenimiento', 4),
                (N'Compras menores', N'Compras no inventariadas', 5)
            ) v(Name, Description, SortOrder)
            WHERE NOT EXISTS (
                SELECT 1 FROM ExpenseCategories ec
                WHERE ec.BusinessId = b.Id AND ec.Name = v.Name
            )
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_Expenses_ExpenseCategories_ExpenseCategoryId", table: "Expenses");
        migrationBuilder.DropIndex(name: "IX_Expenses_ExpenseCategoryId", table: "Expenses");
        migrationBuilder.DropTable(name: "ProductTypeOptions");
        migrationBuilder.DropTable(name: "ExpenseCategories");
        migrationBuilder.DropColumn(name: "ExpenseCategoryId", table: "Expenses");
    }
}
