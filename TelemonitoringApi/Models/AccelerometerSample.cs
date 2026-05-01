namespace TelemonitoringApi.Models;

public class AccelerometerSample
{
    public string PatientId { get; set; } = "";
    public string DeviceId { get; set; } = "";
    public string SessionId { get; set; } = "";
    public DateTime RecordedAt { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
}