using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JetPay.TonWatcher.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MasterchainBlocks",
                columns: table => new
                {
                    Seqno = table.Column<long>(type: "bigint", nullable: false),
                    IsProcessed = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterchainBlocks", x => x.Seqno);
                });

            migrationBuilder.CreateTable(
                name: "TrackedAddresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Address = table.Column<string>(type: "character varying(66)", maxLength: 66, nullable: false),
                    IsTrackingActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackedAddresses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MasterchainBlocks_IsProcessed",
                table: "MasterchainBlocks",
                column: "IsProcessed");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MasterchainBlocks");

            migrationBuilder.DropTable(
                name: "TrackedAddresses");
        }
    }
}
