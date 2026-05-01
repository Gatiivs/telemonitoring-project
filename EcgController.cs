[HttpPost("raw-batch")]
public async Task<IActionResult> SaveRawEcgBatch([FromBody] List<EcgRawSample> samples)
{
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

    return Ok();
}

[HttpPost("accelerometer")]
public async Task<IActionResult> SaveAccelerometerBatch([FromBody] List<AccelerometerSample> samples)
{
    using var connection = new MySqlConnection(_connectionString);

    string sql = @"
        INSERT INTO AccelerometerSamples
        (PatientId, DeviceId, SessionId, RecordedAt, X, Y, Z)
        VALUES
        (@PatientId, @DeviceId, @SessionId, @RecordedAt, @X, @Y, @Z);
    ";

    await connection.ExecuteAsync(sql, samples);

    return Ok();
}
