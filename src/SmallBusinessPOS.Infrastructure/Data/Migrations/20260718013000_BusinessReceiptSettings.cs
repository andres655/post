using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmallBusinessPOS.Infrastructure.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260718013000_BusinessReceiptSettings")]
public partial class BusinessReceiptSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "DefaultTaxRate",
            table: "BusinessSettings",
            type: "decimal(5,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<string>(
            name: "ReceiptHeader",
            table: "BusinessSettings",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ReceiptLogoPath",
            table: "BusinessSettings",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "DefaultTaxRate",
            table: "BusinessSettings");

        migrationBuilder.DropColumn(
            name: "ReceiptHeader",
            table: "BusinessSettings");

        migrationBuilder.DropColumn(
            name: "ReceiptLogoPath",
            table: "BusinessSettings");
    }
}
