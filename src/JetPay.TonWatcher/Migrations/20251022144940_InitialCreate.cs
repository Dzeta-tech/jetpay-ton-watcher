using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JetPay.TonWatcher.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShardBlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Workchain = table.Column<int>(type: "integer", nullable: false),
                    Shard = table.Column<long>(type: "bigint", nullable: false),
                    Seqno = table.Column<long>(type: "bigint", nullable: false),
                    IsProcessed = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShardBlocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrackedAddresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Workchain = table.Column<int>(type: "integer", nullable: false),
                    Account = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: false),
                    IsTrackingActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackedAddresses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShardBlocks_Seqno",
                table: "ShardBlocks",
                column: "Seqno");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShardBlocks");

            migrationBuilder.DropTable(
                name: "TrackedAddresses");
        }
    }
}
