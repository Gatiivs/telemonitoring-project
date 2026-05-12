
namespace CortriumBLE
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Maui.Devices.Sensors;

    public class AccelerometerService
    {
        private List<AccelerometerData> _accelBatch = new List<AccelerometerData>();
        private readonly object _lock = new object();

        private string _sessionId;
        public AccelerometerService(string sessionId)
        {
            _sessionId = sessionId;
        }

        public void ToggleAccelerometer()
        {
            if (Accelerometer.Default.IsSupported)
            {
                if (!Accelerometer.Default.IsMonitoring)
                {
                    Accelerometer.Default.ReadingChanged += Accelerometer_ReadingChanged;
                    Accelerometer.Default.Start(SensorSpeed.Game);
                }
                else
                {
                    Accelerometer.Default.Stop();
                    Accelerometer.Default.ReadingChanged -= Accelerometer_ReadingChanged;
                }
            } else
            {
                return;
            }
        }

        private void Accelerometer_ReadingChanged(object sender, AccelerometerChangedEventArgs e)
        {
            var data = e.Reading.Acceleration;

            var accelData = new AccelerometerData
            {
                SessionId = _sessionId,
                X = data.X,
                Y = data.Y,
                Z = data.Z,
                Timestamp = DateTime.UtcNow
            };

            lock (_lock)
            {
                _accelBatch.Add(accelData);
            }
        }

        public List<AccelerometerData> GetBatch()
        {
            lock (_lock)
            {
                var batchCopy = new List<AccelerometerData>(_accelBatch); 
                return batchCopy;
            }
        }


        //we can remove these 2 later
        public void ReadData()
        {
            var batch = GetBatch();

            if (batch.Count > 0)
            {
                SendToDatabase(batch);
            }
        }

        private void SendToDatabase(List<AccelerometerData> data)
        {
            Console.WriteLine($"Sender {data.Count} målinger til database...");
        }
    }
    //

    public class AccelerometerData
    {
        public required string SessionId { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public DateTime Timestamp { get; set; }
    }


}
