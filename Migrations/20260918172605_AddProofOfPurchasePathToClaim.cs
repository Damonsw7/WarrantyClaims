using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarrantyClaims.Migrations
{
    /// <inheritdoc />
    public partial class AddProofOfPurchasePathToClaim : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProofOfPurchasePath",
                table: "Claims",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProofOfPurchasePath",
                table: "Claims");
        }
    }
}
