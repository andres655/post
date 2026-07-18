using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmallBusinessPOS.Infrastructure.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260718014500_SalePaymentTenderedAmount")]
public partial class SalePaymentTenderedAmount : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "TenderedAmount",
            table: "SalePayments",
            type: "decimal(18,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.Sql("UPDATE SalePayments SET TenderedAmount = Amount WHERE TenderedAmount = 0");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "TenderedAmount",
            table: "SalePayments");
    }
}
