using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarPark.Migrations
{
    /// <inheritdoc />
    public partial class AddParkingRateConditions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParkingRateConditions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParkingRateRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConditionName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MinimumAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_ParkingRateConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParkingRateConditions_ParkingRateRules_ParkingRateRuleId",
                        column: x => x.ParkingRateRuleId,
                        principalTable: "ParkingRateRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParkingRateConditions_Users_CreateBy",
                        column: x => x.CreateBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ParkingRateConditions_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ParkingRateConditions_Users_UpdateBy",
                        column: x => x.UpdateBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParkingRateConditions_CreateBy",
                table: "ParkingRateConditions",
                column: "CreateBy");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingRateConditions_DeletedBy",
                table: "ParkingRateConditions",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingRateConditions_ParkingRateRuleId",
                table: "ParkingRateConditions",
                column: "ParkingRateRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingRateConditions_UpdateBy",
                table: "ParkingRateConditions",
                column: "UpdateBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParkingRateConditions");
        }
    }
}
