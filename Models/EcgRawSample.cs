public class EcgRawSample
{
    public string PatientId { get; set; }
    public string DeviceId { get; set; }
    public string SessionId { get; set; }
    public string PacketId { get; set; }
    public int SampleIndex { get; set; }
    public DateTime RecordedAt { get; set; }
    public int? EcgChannel1 { get; set; }
    public int? EcgChannel2 { get; set; }
    public int? EcgChannel3 { get; set; }
}
