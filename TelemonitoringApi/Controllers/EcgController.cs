using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using TelemonitoringApi.Models;

namespace TelemonitoringApi.Controllers;

[ApiController]
[Route("api/ecg")]
public class EcgController : ControllerBase
{
    private readonly string _connectionString;

    public EcgController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("MySqlConnection")!;
    }

    [HttpPost("raw-batch")]
    public async Task<IActionResult> SaveRawEcgBatch([FromBody] List<EcgRawSample> samples)
    {
        if (samples == null || samples.Count == 0)
            return BadRequest("No ECG samples received.");

        using var connection = new MySqlConnection(_connectionString);

        string sql = @"
            INSERT INTO EcgRawSamples
            (PatientId, DeviceId, SessionId, PacketId, SampleIndex, RecordedAt,
             EcgChannel1, EcgChannel2, EcgChannel3)
            VALUES
            (@PatientId, @DeviceId, @SessionId, @PacketId, @SampleIndex, @RecordedAt,
             @EcgChannel1, @EcgChannel2, @EcgChannel3);
        ";

        await connection.ExecuteAsync(sql, samples);

        return Ok(new
        {
            Message = "Raw ECG batch saved",
            Count = samples.Count
        });
    }

    [HttpPost("accelerometer")]
    public async Task<IActionResult> SaveAccelerometerBatch([FromBody] List<AccelerometerSample> samples)
    {
        if (samples == null || samples.Count == 0)
            return BadRequest("No accelerometer samples received.");

        using var connection = new MySqlConnection(_connectionString);

        string sql = @"
            INSERT INTO AccelerometerSamples
            (PatientId, DeviceId, SessionId, RecordedAt, X, Y, Z)
            VALUES
            (@PatientId, @DeviceId, @SessionId, @RecordedAt, @X, @Y, @Z);
        ";

        await connection.ExecuteAsync(sql, samples);

        return Ok(new
        {
            Message = "Accelerometer batch saved",
            Count = samples.Count
        });
    }
}