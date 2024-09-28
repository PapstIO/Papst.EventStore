using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Papst.EventStore.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class V52_StreamMetaData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MetaDataComment",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "MetaDataTenantId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "MetaDataUserId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "MetaDataUserName",
                table: "Documents");

            migrationBuilder.RenameColumn(
                name: "MetaDataAdditional",
                table: "Documents",
                newName: "MetaData");

            migrationBuilder.AddColumn<decimal>(
                name: "LatestSnapshotVersion",
                table: "Streams",
                type: "decimal(20,0)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaDataAdditionJson",
                table: "Streams",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaDataComment",
                table: "Streams",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaDataTenantId",
                table: "Streams",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaDataUserId",
                table: "Streams",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaDataUserName",
                table: "Streams",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TargetType",
                table: "Documents",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Documents",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "DataType",
                table: "Documents",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LatestSnapshotVersion",
                table: "Streams");

            migrationBuilder.DropColumn(
                name: "MetaDataAdditionJson",
                table: "Streams");

            migrationBuilder.DropColumn(
                name: "MetaDataComment",
                table: "Streams");

            migrationBuilder.DropColumn(
                name: "MetaDataTenantId",
                table: "Streams");

            migrationBuilder.DropColumn(
                name: "MetaDataUserId",
                table: "Streams");

            migrationBuilder.DropColumn(
                name: "MetaDataUserName",
                table: "Streams");

            migrationBuilder.RenameColumn(
                name: "MetaData",
                table: "Documents",
                newName: "MetaDataAdditional");

            migrationBuilder.AlterColumn<string>(
                name: "TargetType",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "DataType",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "MetaDataComment",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaDataTenantId",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaDataUserId",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaDataUserName",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
