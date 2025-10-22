using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JetPay.TonWatcher.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAddressType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "TrackedAddresses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "TrackedAddresses",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
