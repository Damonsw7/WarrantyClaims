using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarrantyClaims.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Claims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CustomerName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CustomerEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SerialNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IssueDescription = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    AssignedAgent = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ManufacturerDeadline = table.Column<DateTime>(type: "TEXT", nullable: true),
                    WarrantyExpiry = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SubmittedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PhotoPath = table.Column<string>(type: "TEXT", nullable: true),
                    ReceiptPath = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Claims", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Claims");
        }
    }
}
