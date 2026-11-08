using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace LimpaFacil.Api.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261019021500_AddCustomerRegistration")]
public partial class AddCustomerRegistration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "CustomerType",
            table: "users",
            type: "varchar(20)",
            maxLength: 20,
            nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AddColumn<string>(
            name: "TaxId",
            table: "users",
            type: "varchar(14)",
            maxLength: 14,
            nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_users_TaxId",
            table: "users",
            column: "TaxId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_users_TaxId", table: "users");
        migrationBuilder.DropColumn(name: "CustomerType", table: "users");
        migrationBuilder.DropColumn(name: "TaxId", table: "users");
    }
}
