using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JetPay.TonWatcher.Migrations
{
    /// <inheritdoc />
    public partial class UseAddressStruct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Account",
                table: "TrackedAddresses");

            migrationBuilder.DropColumn(
                name: "Workchain",
                table: "TrackedAddresses");

            migrationBuilder.AddColumn<byte[]>(
                name: "Address",
                table: "TrackedAddresses",
                type: "bytea",
                maxLength: 36,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "TrackedAddresses");

            migrationBuilder.AddColumn<byte[]>(
                name: "Account",
                table: "TrackedAddresses",
                type: "bytea",
                maxLength: 32,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Workchain",
                table: "TrackedAddresses",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
