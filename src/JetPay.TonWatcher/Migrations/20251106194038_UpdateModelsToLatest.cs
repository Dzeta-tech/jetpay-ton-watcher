using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JetPay.TonWatcher.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModelsToLatest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ShardBlocks_IsProcessed_Seqno",
                table: "ShardBlocks",
                columns: new[] { "IsProcessed", "Seqno" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShardBlocks_IsProcessed_Seqno",
                table: "ShardBlocks");
        }
    }
}
