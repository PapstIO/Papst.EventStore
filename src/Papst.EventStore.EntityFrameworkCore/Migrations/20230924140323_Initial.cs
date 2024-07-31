using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Papst.EventStore.EntityFrameworkCore.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StreamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Time = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MetaDataUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetaDataUserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetaDataTenantId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetaDataComment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetaDataAdditional = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Streams",
                columns: table => new
                {
                    StreamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Version = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    NextVersion = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Updated = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TargetType = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Streams", x => x.StreamId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_StreamId",
                table: "Documents",
                column: "StreamId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_StreamId_Version",
                table: "Documents",
                columns: new[] { "StreamId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_Version",
                table: "Documents",
                column: "Version");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "Streams");
        }
    }
}
