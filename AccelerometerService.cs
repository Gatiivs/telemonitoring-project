namespace CortriumBLE
{
using System;
using System.Collections.Generic;
using Microsoft.Maui.Devices.Sensors;

public class AccelerometerService
{
    private List<AccelerometerData> _accelBatch = new List<AccelerometerData>();
    private readonly object _lock = new object();

    public void ToggleAccelerometer()
    {
        if (Accelerometer.Default.IsSupported)
        {
            if (!Accelerometer.Default.IsMonitoring)
            {
                Accelerometer.Default.ReadingChanged += Accelerometer_ReadingChanged;
                Accelerometer.Default.Start(SensorSpeed.UI);
            }
            else
            {
                Accelerometer.Default.Stop();
                Accelerometer.Default.ReadingChanged -= Accelerometer_ReadingChanged;
            }
        }
    }

    private void Accelerometer_ReadingChanged(object sender, AccelerometerChangedEventArgs e)
    {
        var data = e.Reading.Acceleration;

        var accelData = new AccelerometerData
        {
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

    public List<AccelerometerData> GetBatchAndClear()
    {
        lock (_lock)
        {
            var batchCopy = new List<AccelerometerData>(_accelBatch);
            _accelBatch.Clear();
            return batchCopy;
        }
    }


    //we can remove these 2 later
    public void ReadData()
    {
        var batch = GetBatchAndClear();

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
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public DateTime Timestamp { get; set; }
} 

  
}
