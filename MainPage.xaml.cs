    ﻿//using Android.AdServices.Common;
using CortriumBLE.Cortrium;
using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.ComponentModel;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using CortriumBLE.Utilities;
using CortriumBLE.SignalProcessing;
using System.Linq.Expressions;
//using Windows.ApplicationModel.Background;

namespace CortriumBLE
{

    public class ViewModel
    {
        public ISeries[] Series { get; set; } = [
            new ColumnSeries<int>(3, 4, 2),
        new ColumnSeries<int>(4, 2, 6),
        new ColumnSeries<double, DiamondGeometry>(4, 3, 4)
        ];
    }

    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        int count = 0;

        public IBluetoothLE bluetoothLE { get; private set; }
        public IAdapter adapter { get; private set; }

        public  ObservableCollection<IDevice> DeviceList { get; private set; } = new ObservableCollection<IDevice>();
        public IDevice C3Device { get; private set; }

        private EventHandler<DeviceEventArgs> deviceDiscoveredHandler;

        public event EventHandler<byte[]> PublishData;

        private List<byte[]> rawData = new List<byte[]>();

        private DecodingByteArray decoder = new DecodingByteArray();

        private ECGBatchData ecgData = new ECGBatchData();

        private HeartRateCalculator hrCalculator = new HeartRateCalculator();


        private readonly List<DateTimePoint> _values = new();
        private readonly List<DateTimePoint> _values3 = new();

        private readonly DateTimeAxis _customAxis;
        private ObservableCollection<ISeries> _series;

        private readonly string patientId = "P001";
        private readonly string currentSessionId = Guid.NewGuid().ToString();

        public ObservableCollection<ISeries> Series
        {
            get => _series;
            set
            {
                _series = value;
                OnPropertyChanged();
            }
        }

        private Axis[] _xAxes;
        private double heartRate;
        private string information;
        private double slope;
        private double slopePeak;
        private double csi;
        private double modCsi;
        private double maxCSI;

        public Axis[] XAxes
        {
            get => _xAxes;
            set
            {
                _xAxes = value;
                OnPropertyChanged();
            }
        }

        public ECGDataWriter ecgWriter { get; private set; }
        public double HeartRate
        {
            get => heartRate;
            set
            {
                heartRate = value;
                OnPropertyChanged();
            }
        }


        public double ModCSI
        {
            get => modCsi;
            set
            {
                modCsi = value;
                OnPropertyChanged();
            }
        }

        public double CSI
        {
            get => csi;
            set
            {
                csi = value;
                OnPropertyChanged();
            }
        }


        public string Information
        {
            get => information;
            set
            {
                information= value;
                OnPropertyChanged();
            }
        }

        public double Slope
        {
            get => slope;
            set
            {
                slope= value;
                OnPropertyChanged();
            }
        }

        public double SlopePeak
        {
            get => slopePeak;
            set
            {
                slopePeak = value;
                OnPropertyChanged();
            }
        }


        public double MaxCSI
        {
            get => maxCSI;
            set
            {
                maxCSI = value;
                OnPropertyChanged();
            }
        }

        public MainPage()
        {
            InitializeComponent();

            BindingContext = this;

            Information = "Press Button to Start Search";

            Series = new ObservableCollection<ISeries>
            {
                new LineSeries<DateTimePoint>
                {
                    Values = _values,
                    Fill = null,
                    GeometryFill = null,
                    GeometryStroke = null
                }
                //, // Enable this to get ECG3 
                //new LineSeries<DateTimePoint>
                //{
                //    Values = _values3,
                //    Fill = null,
                //    GeometryFill = null,
                //    GeometryStroke = null
                //}
            };

            ECGChart.XAxes = new Axis[]
            {
                new Axis
                {
                    LabelsPaint = null
                }
            };

            //ECGChart.YAxes = new Axis[]
            //{
            //    new Axis
            //    {
            //        MinLimit = -800000,   // Start y-axis at 0
            //        MaxLimit = 800000    // End y-axis at 50
            //    }
            //};

            ECGChart.Series = Series;

            //_customAxis = new DateTimeAxis(TimeSpan.FromSeconds(1), Formatter)
            //{
            //    //CustomSeparators = GetSeparators(),
            //    //AnimationsSpeed = TimeSpan.FromMilliseconds(0.5),
            //    //SeparatorsPaint = new SolidColorPaint(SKColors.Black.WithAlpha(100))
            //};

            //XAxes = new Axis[] { _customAxis };
        }

        private double[] GetSeparators()
        {
            var now = DateTime.Now;

            return new double[]
            {
                now.AddSeconds(-2).Ticks,
                now.Ticks
            };
        }

        private static string Formatter(DateTime date)
        {
            var secsAgo = (DateTime.Now - date).TotalSeconds;

            return secsAgo < 1
                ? "now"
                : $"{secsAgo:N0}s ago";
        }

        private List<DateTimePoint> GenerateSampleData(int numberOfPoints)
        {
            Random rand = new Random();
            var data = new List<DateTimePoint>();
            DateTime now = DateTime.Now;

            for (int i = 0; i < numberOfPoints; i++)
            {
                data.Add(new DateTimePoint(now.AddMilliseconds(i * 100), rand.Next(-200, 200)));
            }

            return data;
        }
        private  void OnCounterClicked(object sender, EventArgs e)
        {

            //_values.Clear(); // Clear old values
            //_values.AddRange(GenerateSampleData(10)); // Generate new data

           //pdateChart();

            InitBluetooth();
            count++;

            if (count == 1)
                SearchBtn.Text = $"Search started {count} time";
            else
                SearchBtn.Text = $"Search started {count} times";

        }

        private async Task<bool> CheckAndRequestPermissionAsync<TPermission>() where TPermission : Permissions.BasePermission, new()
        {
            var status = await Permissions.CheckStatusAsync<TPermission>();
            if (status != PermissionStatus.Granted)
            {
                var result = await Permissions.RequestAsync<TPermission>();
                return result == PermissionStatus.Granted;
            }
            return true;
        }

        private async Task<bool> GetPermissions()
        {
            var bleGranted = await CheckAndRequestPermissionAsync<Permissions.Bluetooth>();
            var locGranted = true; //await CheckAndRequestPermissionAsync<Permissions.LocationWhenInUse>();
            //var locGrantedFull = await CheckAndRequestPermissionAsync<Permissions.LocationAlways>();

            return bleGranted && locGranted;
        }

        private async void InitBluetooth()
        {
            await GetPermissions();
            bluetoothLE = CrossBluetoothLE.Current;
            adapter = CrossBluetoothLE.Current.Adapter;
            adapter.ScanTimeout = 30000; // scan for 10 seconds
            adapter.ScanMode = ScanMode.LowLatency; //Low latency is the fastest scan mode, but it uses the most power. Balanced is a good compromise between speed and power consumption. Low power is the slowest scan mode, but it uses the least power.

            adapter.DeviceDiscovered += Adapter_DeviceDiscovered; 
            DeviceList.Clear();
            await adapter.StartScanningForDevicesAsync();

        }

        private async void Adapter_DeviceDiscovered(object? sender, DeviceEventArgs e)
        {

            Console.WriteLine($"DISCOVERED: {e.Device?.Name} | {e.Device?.Id}");

                //Console.WriteLine(args.Device.Name);
                if (e.Device != null && e.Device.Name != null &&
                     Regex.IsMatch(e.Device.Name, @"^C\d"))
                {
                    try
                    {
                        Information = "I found a new device - trying to connect";

                        //Just connect if possible
                        await Connect(e.Device);

                        // Check if the device is already in the list
                        if (!DeviceList.Any(d => d.Id == e.Device.Id))
                        {
                            MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                DeviceList.Add(e.Device);
                            });


                            Console.WriteLine("Added BLE device: " + e.Device.Name);
                        }
                        else
                            Console.WriteLine("Did not add BLE device: " + e.Device.Name);

                    }
                    catch { }
                }
      
        }

        private async void DeviceListView_ItemTapped(object sender, ItemTappedEventArgs e)

        { 

            ecgWriter = new ECGDataWriter();

            if (e.Item != null)
            {
                IDevice tappedDevice = (IDevice)e.Item;

                C3Device = tappedDevice;

                await Connect(C3Device);
                //if (resultSucceses)
                //{
                //    await Navigation.PushAsync(new MeasuringPage(patient));
                //}
                //else
                //{
                //    await DisplayAlert("Connection failed!", "Please check internet connection and retry", "OK");
                //}

            }
        }

        private async Task  Connect(IDevice device)
        {
            C3Device = device; //Refactor please
            //await adapter.StopScanningForDevicesAsync();

            adapter.DeviceConnected += Adapter_DeviceConnected;

            await adapter.ConnectToDeviceAsync(device);


            this.PublishData += ReadData;

            await this.ReadDataAsync();
        }

        private void Adapter_DeviceConnected(object? sender, DeviceEventArgs e)
        {
            Information = string.Format("Device {0} connected", e.Device);
        }

        private async void ReadData(object? sender, byte[] e)
        {
            try
            {
                Cortrium.ECGBatchData ecgData = decoder.DecodeBytes(Array.ConvertAll(e, x => (sbyte)x));

                var packetId = Guid.NewGuid().ToString();
                var deviceId = C3Device?.Id.ToString() ?? "UnknownDevice";
                var startTime = DateTime.UtcNow;

                var rawSamples = new List<EcgRawSample>();

                for (int i = 0; i < ecgData.ECGChannel1.Length; i++)
                {
                    rawSamples.Add(new EcgRawSample
                    {
                        PatientId = patientId,
                        DeviceId = deviceId,
                        SessionId = currentSessionId,
                        PacketId = packetId,
                        SampleIndex = i,
                        RecordedAt = startTime.AddMilliseconds(i * 1000.0 / 256.0),

                        EcgChannel1 = ecgData.ECGChannel1.Length > i ? ecgData.ECGChannel1[i] : null,
                        EcgChannel2 = ecgData.ECGChannel2.Length > i ? ecgData.ECGChannel2[i] : null,
                        EcgChannel3 = ecgData.ECGChannel3.Length > i ? ecgData.ECGChannel3[i] : null
                    });
                }

                if (ecgWriter != null && rawSamples.Count > 0)
                {
                    await ecgWriter.WriteEcgRawBatchAsync(rawSamples); //save the raw samples
                }

                //var combinedDataList = Enumerable.Range(0, ecgData.ECGChannel1.Length)
                //    .Where(index => index % 4 == 0) //Downsample to 64 Hz to avoid overloading the app
                //    //.Select(index => new[] { ecgData.ECGChannel1[index], ecgData.ECGChannel2[index], ecgData.ECGChannel3[index] }.Average())
                //    .Select(index => ecgData.ECGChannel1[index])
                //    .Select(Convert.ToInt32)
                //    .ToList();


                //var combinedDataList = Enumerable.Range(0, ecgData.ECGChannel1.Length)
                //.Where(index => index % 4 == 0)
                //     .Select(index => ecgData.ECGChannel1[index] ) //, ecgData.ECGChannel3[index] }.Average())
                //     .Select(Convert.ToInt32)
                //

                //keep this to mae the screen work
                int ecg1 = (ecgData.ECGChannel1[0] + ecgData.ECGChannel1[1] + ecgData.ECGChannel1[2] + ecgData.ECGChannel1[3] + ecgData.ECGChannel1[4] + ecgData.ECGChannel1[5]) / 6;
                int ecg3 = (ecgData.ECGChannel1[6] + ecgData.ECGChannel1[7] + ecgData.ECGChannel1[8] + ecgData.ECGChannel1[9] + ecgData.ECGChannel1[10] + ecgData.ECGChannel1[11]) / 6;

                //                for (int k = 0; k < combinedDataList.Count; k++)
                {
                    //await ecgWriter.WriteEcgValueAsync(combinedDataList[k]);
                    //if (ecgWriter != null)
                        //await ecgWriter.WriteEcgValueAsync(ecg1);

                    //accelerometer sends data here
                    var accelBatch = accelerometerService.GetBatchAndClear();

                    if (accelBatch.Count > 0 && ecgWriter != null)
                    {
                        await ecgWriter.WriteAccelerometerBatchAsync(
                            accelBatch,
                            patientId,
                            currentSessionId,
                            C3Device?.Id.ToString() ?? "UnknownDevice"
                        );
                    }

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        try {
                            //Console.WriteLine($"Data: ECGChannel1: {ecgData.ECGChannel1[k]}, ECGChannel2: {ecgData.ECGChannel2[k]}, ECGChannel3: {ecgData.ECGChannel3[k]}");
                            Console.WriteLine($"Data ECG1: {ecg1}");
                            // Add each new point with a timestamp based on 256 Hz sample rate
                            var pointTime = DateTime.Now; //.AddMilliseconds(3.9); // Rough interval for 256  is 3.9 - but for downsampling by 4 - it must be 3.9*4 = 15.6Hz

                            _values.Add(new DateTimePoint(pointTime, ecg1));
                            _values3.Add(new DateTimePoint(pointTime, ecg3));

                            //_values.Add(new DateTimePoint(DateTime.Now, combinedDataList[k]));

                            int maxVisiblePoints = 50;

#if WINDOWS
                                maxVisiblePoints = 75;
#endif

                            if (_values.Count > maxVisiblePoints)
                            {

                                try
                                {
                                    var signal = _values.Select(point => (int)point.Value).ToArray();
                                    List<int> peaks = (EcgUtils.DetectPeaks(signal, 256));
                                    var start = _values.First<DateTimePoint>().DateTime;
                                    var end = _values.Last<DateTimePoint>().DateTime;

                                    var time = (end - start).TotalSeconds;
                                    //Calculae the slope of the data
                                    var slope = EcgUtils.CalculateSlope(signal.ToList()); //or preeaks
                                    Slope = Math.Round(slope, 4);

                                    var slope_peaks = EcgUtils.CalculateSlope(peaks); //
                                    SlopePeak = Math.Round(slope_peaks, 4);
                                    //If the absolute slope is smaller than 0.06 and the
                                    //slope of the peaks is less than 15% - then we beleive signal is stable
                                    //then data are still settling - and the signal should not be used
                                    if (Math.Abs(slope) < 0.035 && (Math.Abs(slope_peaks) < 40))
                                    {
                                        double hr = peaks.Count * 60 / time;
                                        Console.WriteLine("Heart rate: " + hr);
                                        //HeartRate = Math.Round(hr,1);

                                        double timeInterval = time; // Time in seconds between peaks or sampling period

                                        double medianHR = hrCalculator.CalculateHeartRate(peaks, timeInterval);

                                        if (medianHR > 0)
                                            HeartRate = Math.Round(medianHR, 2);

                                        var result = SeizureDetect.CalcModCSI_and_CSI(peaks, peaks.Count);

                                        CSI = Math.Round(result[0],0);
                                        ModCSI = Math.Round(result[1],0);

                                        if (CSI > MaxCSI && CSI < 10000000)
                                            MaxCSI = CSI;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("Error during CSI and HR cacl: " + ex);
                                }

                                // Keep only the latest `MaxVisiblePoints` points for a sliding window effect
                                if (_values.Count > maxVisiblePoints)
                                {
                                    _values.RemoveAt(0);
                                    _values3.RemoveAt(0);

                                }

                                UpdateChart(); // Refresh the chart                           
                                Console.WriteLine($"Total points in chart: {_values.Count}");
                                }
                            } 
                            catch (Exception ex)
                            {
                                Console.WriteLine("Error during Invoke: " + e);
                            }
                    });
                }

                //_customAxis.CustomSeparators = GetSeparators();
               
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex);
                //throw new Exception(ex.Message);
            }
        }

        private void UpdateChart()
        {
            // This line triggers the chart to refresh
            if (Series[0] != null)
            {
                // Update the Values property by assigning a new ObservableCollection with _values
                Series[0].Values = new List<DateTimePoint>(_values); // Use List<DateTimePoint> if needed
            }
            //this.Chart.Update();
        }
            public async Task ReadDataAsync()
        {
            if (C3Device.State != DeviceState.Connected)
            {
                throw new Exception("Device not connected");
            }

            try
            {
                var services = await C3Device.GetServicesAsync();

                foreach (var service in services)
                {
                    var characteristics = await service.GetCharacteristicsAsync();

                    foreach (var characteristic in characteristics)
                    {
                        if (characteristic.CanUpdate)
                        {
                            var reader = characteristic;

                            await reader.StartUpdatesAsync();

                            reader.ValueUpdated += ReaderValueUpdated;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message.ToString());
            }
        }


        private async void ReaderValueUpdated(object sender, CharacteristicUpdatedEventArgs e)
        {
            try
            {
                byte[] bytes = e.Characteristic.Value;

                if (bytes != null)
                {
                    lock (rawData)
                    {
                        rawData.Add(bytes);
                        if (rawData.Count >= 1)
                        {
                            byte[] concatenatedBytes = rawData.SelectMany(arr => arr).ToArray();
                            OnPublishData(concatenatedBytes);
                            rawData.Clear();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message.ToString());
            }
        }
        private void OnPublishData(byte[] data)
        {
            PublishData?.Invoke(this, data);
        }

        public new event PropertyChangedEventHandler PropertyChanged;
        protected override void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }

}
