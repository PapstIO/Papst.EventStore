using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Papst.EventStore.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class V71_EventSigning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LatestSignature",
                table: "Streams",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SigningAlgorithm",
                table: "Streams",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SigningCertificateThumbprint",
                table: "Streams",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Signature",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LatestSignature",
                table: "Streams");

            migrationBuilder.DropColumn(
                name: "SigningAlgorithm",
                table: "Streams");

            migrationBuilder.DropColumn(
                name: "SigningCertificateThumbprint",
                table: "Streams");

            migrationBuilder.DropColumn(
                name: "Signature",
                table: "Documents");
        }
    }
}
