using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JetPay.TonWatcher.Migrations
{
    /// <inheritdoc />
    public partial class SwitchToShardBlocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MasterchainBlocks");

            migrationBuilder.CreateTable(
                name: "ShardBlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Workchain = table.Column<int>(type: "integer", nullable: false),
                    Shard = table.Column<long>(type: "bigint", nullable: false),
                    Seqno = table.Column<long>(type: "bigint", nullable: false),
                    RootHash = table.Column<string>(type: "text", nullable: false),
                    FileHash = table.Column<string>(type: "text", nullable: false),
                    IsProcessed = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShardBlocks", x => x.Id);
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

            migrationBuilder.CreateIndex(
                name: "IX_MasterchainBlocks_IsProcessed",
                table: "MasterchainBlocks",
                column: "IsProcessed");
        }
    }
}
