using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RedisDemo2.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Telemetries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceIMEI = table.Column<string>(type: "text", nullable: false),
                    DeviceIP = table.Column<string>(type: "text", nullable: false),
                    LocationX = table.Column<float>(type: "real", nullable: false),
                    LocationY = table.Column<float>(type: "real", nullable: false),
                    Altitude = table.Column<short>(type: "smallint", nullable: false),
                    Angle = table.Column<short>(type: "smallint", nullable: false),
                    Satellites = table.Column<byte>(type: "smallint", nullable: false),
                    Speed = table.Column<short>(type: "smallint", nullable: false),
                    BatteryLevel = table.Column<long>(type: "bigint", nullable: false),
                    BatteryCharging = table.Column<long>(type: "bigint", nullable: false),
                    ErrorCode = table.Column<long>(type: "bigint", nullable: false),
                    IgnitionStatus = table.Column<long>(type: "bigint", nullable: false),
                    Movement = table.Column<long>(type: "bigint", nullable: false),
                    GSMSignalStrength = table.Column<long>(type: "bigint", nullable: false),
                    SleepMode = table.Column<long>(type: "bigint", nullable: false),
                    GNSFStatus = table.Column<long>(type: "bigint", nullable: false),
                    AxisX = table.Column<long>(type: "bigint", nullable: false),
                    AxisY = table.Column<long>(type: "bigint", nullable: false),
                    AxisZ = table.Column<long>(type: "bigint", nullable: false),
                    LicencePlate = table.Column<string>(type: "text", nullable: false),
                    BlueToothLockStatus = table.Column<long>(type: "bigint", nullable: false),
                    BlueToothLockBatteryLevel = table.Column<long>(type: "bigint", nullable: false),
                    TelemetryDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RecievedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AlarmForBeingPushedInLockMode = table.Column<long>(type: "bigint", nullable: false),
                    TamperDetectionEvent = table.Column<long>(type: "bigint", nullable: false),
                    Unplug = table.Column<long>(type: "bigint", nullable: false),
                    FallDown = table.Column<long>(type: "bigint", nullable: false),
                    CurrentOperationMode = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Telemetries", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Telemetries");
        }
    }
}
