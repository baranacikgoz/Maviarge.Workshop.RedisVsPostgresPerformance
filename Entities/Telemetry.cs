using System.ComponentModel.DataAnnotations;

namespace RedisDemo2.Entities;

public class Telemetry
{
    [Key]
    public Guid Id { get; set; }

    public string DeviceIMEI { get; set; }
    public string DeviceIP { get; set; }
    public float LocationX { get; set; }
    public float LocationY { get; set; }
    public short Altitude { get; set; }
    public short Angle { get; set; }
    public byte Satellites { get; set; }
    public short Speed { get; set; }

    //352
    public long BatteryLevel { get; set; }

    //347
    public long BatteryCharging { get; set; }

    //341
    public long ErrorCode { get; set; }

    //239
    public long IgnitionStatus { get; set; }

    //240
    public long Movement { get; set; }

    //21
    public long GSMSignalStrength { get; set; }

    //200
    public long SleepMode { get; set; }

    //69
    public long GNSFStatus { get; set; }

    //17 AxisX
    public long AxisX { get; set; }

    //18 AxisY
    public long AxisY { get; set; }

    //19 AxisZ
    public long AxisZ { get; set; }

    public string LicencePlate { get; set; }

    //871
    public long BlueToothLockStatus { get; set; }

    public long BlueToothLockBatteryLevel { get; set; }

    public DateTime TelemetryDateTime { get; set; }
    public DateTime RecievedAt { get; set; }

    public long AlarmForBeingPushedInLockMode { get; set; }

    public long TamperDetectionEvent { get; set; }

    public long Unplug { get; set; }

    public long FallDown { get; set; }

    public long CurrentOperationMode { get; set; }
}