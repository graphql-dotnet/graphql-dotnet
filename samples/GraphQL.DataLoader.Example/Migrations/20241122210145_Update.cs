using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLoaderGql.Migrations
{
    /// <inheritdoc />
    public partial class Update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cars_Salespeople_SalespersonId",
                table: "Cars");

            migrationBuilder.RenameColumn(
                name: "SalespersonId",
                table: "Cars",
                newName: "SalesPersonId");

            migrationBuilder.RenameIndex(
                name: "IX_Cars_SalespersonId",
                table: "Cars",
                newName: "IX_Cars_SalesPersonId");

            migrationBuilder.AlterColumn<int>(
                name: "SalesPersonId",
                table: "Cars",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Cars_Salespeople_SalesPersonId",
                table: "Cars",
                column: "SalesPersonId",
                principalTable: "Salespeople",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cars_Salespeople_SalesPersonId",
                table: "Cars");

            migrationBuilder.RenameColumn(
                name: "SalesPersonId",
                table: "Cars",
                newName: "SalespersonId");

            migrationBuilder.RenameIndex(
                name: "IX_Cars_SalesPersonId",
                table: "Cars",
                newName: "IX_Cars_SalespersonId");

            migrationBuilder.AlterColumn<int>(
                name: "SalespersonId",
                table: "Cars",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_Cars_Salespeople_SalespersonId",
                table: "Cars",
                column: "SalespersonId",
                principalTable: "Salespeople",
                principalColumn: "Id");
        }
    }
}
