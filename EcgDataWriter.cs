using System.Net.Http;
using System.Net.Http.Json;

namespace CortriumBLE
{
    public class ECGDataWriter
    {
        private readonly HttpClient _httpClient;

        public ECGDataWriter()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://10.192.51.153:5000/");
            };
        }

        public async Task WriteEcgRawBatchAsync(List<EcgRawSample> samples)
        {
            var response = await _httpClient.PostAsJsonAsync("api/ecg/raw-batch", samples);
            response.EnsureSuccessStatusCode();
        }

        public async Task WriteAccelerometerBatchAsync(
            List<AccelerometerData> batch,
            string patientId,
            string sessionId,
            string deviceId)
        {
            var payload = batch.Select(a => new
            {
                PatientId = patientId,
                DeviceId = deviceId,
                SessionId = sessionId,
                RecordedAt = a.Timestamp,
                X = a.X,
                Y = a.Y,
                Z = a.Z
            }).ToList();

            var response = await _httpClient.PostAsJsonAsync("api/ecg/accelerometer", payload);
            response.EnsureSuccessStatusCode();
        }
    }
}
