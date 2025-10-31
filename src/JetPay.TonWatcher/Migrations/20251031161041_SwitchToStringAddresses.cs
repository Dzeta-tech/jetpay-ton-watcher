using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JetPay.TonWatcher.Migrations
{
    /// <inheritdoc />
    public partial class SwitchToStringAddresses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Hash",
                table: "TrackedAddresses");

            migrationBuilder.DropColumn(
                name: "Workchain",
                table: "TrackedAddresses");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "TrackedAddresses",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "TrackedAddresses");

            migrationBuilder.AddColumn<byte[]>(
                name: "Hash",
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
