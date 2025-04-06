using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalWarehouse_BusinessObject.Migrations
{
    /// <inheritdoc />
    public partial class Unit_Medical : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "Medicals",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Unit",
                table: "Medicals");
        }
    }
}
