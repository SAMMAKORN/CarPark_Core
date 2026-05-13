using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarPark.Migrations
{
    /// <inheritdoc />
    public partial class AddParkingGate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParkingGateId",
                table: "ParkingTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "CloseTime",
                table: "ParkingLots",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<bool>(
                name: "IsAllDay",
                table: "ParkingLots",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "OpenTime",
                table: "ParkingLots",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.CreateTable(
                name: "ParkingGates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParkingLotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GateName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreateBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreateAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdateAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingGates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParkingGates_ParkingLots_ParkingLotId",
                        column: x => x.ParkingLotId,
                        principalTable: "ParkingLots",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ParkingGates_Users_CreateBy",
                        column: x => x.CreateBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ParkingGates_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ParkingGates_Users_UpdateBy",
                        column: x => x.UpdateBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParkingTransactions_ParkingGateId",
                table: "ParkingTransactions",
                column: "ParkingGateId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingGates_CreateBy",
                table: "ParkingGates",
                column: "CreateBy");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingGates_DeletedBy",
                table: "ParkingGates",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingGates_ParkingLotId",
                table: "ParkingGates",
                column: "ParkingLotId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingGates_UpdateBy",
                table: "ParkingGates",
                column: "UpdateBy");

            migrationBuilder.AddForeignKey(
                name: "FK_ParkingTransactions_ParkingGates_ParkingGateId",
                table: "ParkingTransactions",
                column: "ParkingGateId",
                principalTable: "ParkingGates",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParkingTransactions_ParkingGates_ParkingGateId",
                table: "ParkingTransactions");

            migrationBuilder.DropTable(
                name: "ParkingGates");

            migrationBuilder.DropIndex(
                name: "IX_ParkingTransactions_ParkingGateId",
                table: "ParkingTransactions");

            migrationBuilder.DropColumn(
                name: "ParkingGateId",
                table: "ParkingTransactions");

            migrationBuilder.DropColumn(
                name: "CloseTime",
                table: "ParkingLots");

            migrationBuilder.DropColumn(
                name: "IsAllDay",
                table: "ParkingLots");

            migrationBuilder.DropColumn(
                name: "OpenTime",
                table: "ParkingLots");
        }
    }
}
