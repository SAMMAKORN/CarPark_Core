using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CarPark.Migrations
{
    /// <inheritdoc />
    public partial class AddGateInOut : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParkingTransactions_ParkingGates_ParkingGateId",
                table: "ParkingTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Users_Username",
                table: "Users");

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"));

            migrationBuilder.RenameColumn(
                name: "ParkingGateId",
                table: "ParkingTransactions",
                newName: "OutGateId");

            migrationBuilder.RenameIndex(
                name: "IX_ParkingTransactions_ParkingGateId",
                table: "ParkingTransactions",
                newName: "IX_ParkingTransactions_OutGateId");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Password",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AddColumn<Guid>(
                name: "InGateId",
                table: "ParkingTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParkingTransactions_InGateId",
                table: "ParkingTransactions",
                column: "InGateId");

            migrationBuilder.AddForeignKey(
                name: "FK_ParkingTransactions_ParkingGates_InGateId",
                table: "ParkingTransactions",
                column: "InGateId",
                principalTable: "ParkingGates",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ParkingTransactions_ParkingGates_OutGateId",
                table: "ParkingTransactions",
                column: "OutGateId",
                principalTable: "ParkingGates",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParkingTransactions_ParkingGates_InGateId",
                table: "ParkingTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_ParkingTransactions_ParkingGates_OutGateId",
                table: "ParkingTransactions");

            migrationBuilder.DropIndex(
                name: "IX_ParkingTransactions_InGateId",
                table: "ParkingTransactions");

            migrationBuilder.DropColumn(
                name: "InGateId",
                table: "ParkingTransactions");

            migrationBuilder.RenameColumn(
                name: "OutGateId",
                table: "ParkingTransactions",
                newName: "ParkingGateId");

            migrationBuilder.RenameIndex(
                name: "IX_ParkingTransactions_OutGateId",
                table: "ParkingTransactions",
                newName: "IX_ParkingTransactions_ParkingGateId");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Password",
                table: "Users",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Users",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreateAt", "CreateBy", "DeleteAt", "DeletedBy", "IsDeleted", "MustChangePassword", "Name", "Password", "Role", "UpdateAt", "UpdateBy", "Username" },
                values: new object[,]
                {
                    { new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, false, true, "Normal User", "PBKDF2$SHA256$100000$W6wNPlF5a8q79g6h3inyRQ==$0N6YuOugjBTtdFCNrIBjdkXyi9B7xVfx6a75yU147r4=", 1, null, null, "user" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, false, true, "Parking Operator", "PBKDF2$SHA256$100000$ITDALgYVh6dezGnLr7jgFg==$Gu2S3x9mUOixGYSLAxZQqQUc1dU2xzIY2mUGT/aiYks=", 1, null, null, "operator" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ParkingTransactions_ParkingGates_ParkingGateId",
                table: "ParkingTransactions",
                column: "ParkingGateId",
                principalTable: "ParkingGates",
                principalColumn: "Id");
        }
    }
}
