using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web_DACS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDanhGia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChiTietDatMons_BanAns_BanAnId",
                table: "ChiTietDatMons");

            migrationBuilder.DropForeignKey(
                name: "FK_ChiTietDatMons_DatBans_DatBanId",
                table: "ChiTietDatMons");

            migrationBuilder.DropTable(
                name: "ChiTietDatBans");

            migrationBuilder.DropIndex(
                name: "IX_ChiTietDatMons_BanAnId",
                table: "ChiTietDatMons");

            migrationBuilder.AlterColumn<int>(
                name: "DatBanId",
                table: "ChiTietDatMons",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "DanhGias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DatBanId = table.Column<int>(type: "int", nullable: false),
                    SoSao = table.Column<int>(type: "int", nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NgayDanhGia = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanhGias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DanhGias_DatBans_DatBanId",
                        column: x => x.DatBanId,
                        principalTable: "DatBans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DanhGias_DatBanId",
                table: "DanhGias",
                column: "DatBanId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChiTietDatMons_DatBans_DatBanId",
                table: "ChiTietDatMons",
                column: "DatBanId",
                principalTable: "DatBans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChiTietDatMons_DatBans_DatBanId",
                table: "ChiTietDatMons");

            migrationBuilder.DropTable(
                name: "DanhGias");

            migrationBuilder.AlterColumn<int>(
                name: "DatBanId",
                table: "ChiTietDatMons",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "ChiTietDatBans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DatBanId = table.Column<int>(type: "int", nullable: false),
                    MonAnId = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietDatBans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChiTietDatBans_DatBans_DatBanId",
                        column: x => x.DatBanId,
                        principalTable: "DatBans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChiTietDatBans_MonAns_MonAnId",
                        column: x => x.MonAnId,
                        principalTable: "MonAns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDatMons_BanAnId",
                table: "ChiTietDatMons",
                column: "BanAnId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDatBans_DatBanId",
                table: "ChiTietDatBans",
                column: "DatBanId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDatBans_MonAnId",
                table: "ChiTietDatBans",
                column: "MonAnId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChiTietDatMons_BanAns_BanAnId",
                table: "ChiTietDatMons",
                column: "BanAnId",
                principalTable: "BanAns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChiTietDatMons_DatBans_DatBanId",
                table: "ChiTietDatMons",
                column: "DatBanId",
                principalTable: "DatBans",
                principalColumn: "Id");
        }
    }
}
