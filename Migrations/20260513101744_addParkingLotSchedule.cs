using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarPark.Migrations
{
    /// <inheritdoc />
    public partial class addParkingLotSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParkingScheduleId",
                table: "ParkingRateRules",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ParkingLotSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParkingLotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DaysOfWeek = table.Column<int>(type: "int", nullable: false),
                    IsAllDay = table.Column<bool>(type: "bit", nullable: false),
                    OpenTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    CloseTime = table.Column<TimeSpan>(type: "time", nullable: false),
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
                    table.PrimaryKey("PK_ParkingLotSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParkingLotSchedules_ParkingLots_ParkingLotId",
                        column: x => x.ParkingLotId,
                        principalTable: "ParkingLots",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ParkingLotSchedules_Users_CreateBy",
                        column: x => x.CreateBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ParkingLotSchedules_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ParkingLotSchedules_Users_UpdateBy",
                        column: x => x.UpdateBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParkingRateRules_ParkingScheduleId",
                table: "ParkingRateRules",
                column: "ParkingScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingLotSchedules_CreateBy",
                table: "ParkingLotSchedules",
                column: "CreateBy");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingLotSchedules_DeletedBy",
                table: "ParkingLotSchedules",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingLotSchedules_ParkingLotId",
                table: "ParkingLotSchedules",
                column: "ParkingLotId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingLotSchedules_UpdateBy",
                table: "ParkingLotSchedules",
                column: "UpdateBy");

            migrationBuilder.AddForeignKey(
                name: "FK_ParkingRateRules_ParkingLotSchedules_ParkingScheduleId",
                table: "ParkingRateRules",
                column: "ParkingScheduleId",
                principalTable: "ParkingLotSchedules",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParkingRateRules_ParkingLotSchedules_ParkingScheduleId",
                table: "ParkingRateRules");

            migrationBuilder.DropTable(
                name: "ParkingLotSchedules");

            migrationBuilder.DropIndex(
                name: "IX_ParkingRateRules_ParkingScheduleId",
                table: "ParkingRateRules");

            migrationBuilder.DropColumn(
                name: "ParkingScheduleId",
                table: "ParkingRateRules");
        }
    }
}
