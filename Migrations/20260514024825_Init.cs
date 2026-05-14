using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarPark.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    MustChangePassword = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Users_CreateBy",
                        column: x => x.CreateBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Users_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Users_Users_UpdateBy",
                        column: x => x.UpdateBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ParkingLots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LotCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LotName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsAllDay = table.Column<bool>(type: "bit", nullable: false),
                    OpenTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    CloseTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    HasOvernightPenalty = table.Column<bool>(type: "bit", nullable: false),
                    OvernightPenaltyAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_ParkingLots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParkingLots_Users_CreateBy",
                        column: x => x.CreateBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ParkingLots_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ParkingLots_Users_UpdateBy",
                        column: x => x.UpdateBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

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

            migrationBuilder.CreateTable(
                name: "ParkingTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParkingLotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlateNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OutAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalMinutes = table.Column<int>(type: "int", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsOvernight = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    InGateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OutGateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_ParkingTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParkingTransactions_ParkingGates_InGateId",
                        column: x => x.InGateId,
                        principalTable: "ParkingGates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ParkingTransactions_ParkingGates_OutGateId",
                        column: x => x.OutGateId,
                        principalTable: "ParkingGates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ParkingTransactions_ParkingLots_ParkingLotId",
                        column: x => x.ParkingLotId,
                        principalTable: "ParkingLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParkingTransactions_Users_CreateBy",
                        column: x => x.CreateBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ParkingTransactions_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ParkingTransactions_Users_UpdateBy",
                        column: x => x.UpdateBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ParkingRateRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParkingLotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RuleName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    StartMinute = table.Column<int>(type: "int", nullable: false),
                    EndMinute = table.Column<int>(type: "int", nullable: true),
                    CalculationType = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BillingStepMinutes = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ParkingScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_ParkingRateRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParkingRateRules_ParkingLotSchedules_ParkingScheduleId",
                        column: x => x.ParkingScheduleId,
                        principalTable: "ParkingLotSchedules",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ParkingRateRules_ParkingLots_ParkingLotId",
                        column: x => x.ParkingLotId,
                        principalTable: "ParkingLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParkingRateRules_Users_CreateBy",
                        column: x => x.CreateBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ParkingRateRules_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ParkingRateRules_Users_UpdateBy",
                        column: x => x.UpdateBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreateAt", "CreateBy", "DeleteAt", "DeletedBy", "IsDeleted", "MustChangePassword", "Name", "Password", "Role", "UpdateAt", "UpdateBy", "Username" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, false, true, "System Admin", "PBKDF2$SHA256$100000$CNMtqGjCwsaY4gv7R9CXhw==$lGxIuuvrpQU4rT2dzxg8Y7sbv2kmTK8mG2kMgrOUMc4=", 0, null, null, "admin" });

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

            migrationBuilder.CreateIndex(
                name: "IX_ParkingLots_CreateBy",
                table: "ParkingLots",
                column: "CreateBy");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingLots_DeletedBy",
                table: "ParkingLots",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingLots_UpdateBy",
                table: "ParkingLots",
                column: "UpdateBy");

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

            migrationBuilder.CreateIndex(
                name: "IX_ParkingRateRules_CreateBy",
                table: "ParkingRateRules",
                column: "CreateBy");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingRateRules_DeletedBy",
                table: "ParkingRateRules",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingRateRules_ParkingLotId",
                table: "ParkingRateRules",
                column: "ParkingLotId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingRateRules_ParkingScheduleId",
                table: "ParkingRateRules",
                column: "ParkingScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingRateRules_UpdateBy",
                table: "ParkingRateRules",
                column: "UpdateBy");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingTransactions_CreateBy",
                table: "ParkingTransactions",
                column: "CreateBy");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingTransactions_DeletedBy",
                table: "ParkingTransactions",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingTransactions_InGateId",
                table: "ParkingTransactions",
                column: "InGateId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingTransactions_OutGateId",
                table: "ParkingTransactions",
                column: "OutGateId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingTransactions_ParkingLotId",
                table: "ParkingTransactions",
                column: "ParkingLotId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingTransactions_UpdateBy",
                table: "ParkingTransactions",
                column: "UpdateBy");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreateBy",
                table: "Users",
                column: "CreateBy");

            migrationBuilder.CreateIndex(
                name: "IX_Users_DeletedBy",
                table: "Users",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UpdateBy",
                table: "Users",
                column: "UpdateBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParkingRateRules");

            migrationBuilder.DropTable(
                name: "ParkingTransactions");

            migrationBuilder.DropTable(
                name: "ParkingLotSchedules");

            migrationBuilder.DropTable(
                name: "ParkingGates");

            migrationBuilder.DropTable(
                name: "ParkingLots");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
