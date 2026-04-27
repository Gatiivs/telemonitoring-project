

//using AndroidX.Core.Util;
using System;
using System.IO;
using System.Threading.Tasks;


namespace CortriumBLE.Utilities
{
    public class ECGDataWriter
    {
        public string filePath;

        public ECGDataWriter()
        {
#if WINDOWS
            // Define a platform-independent file path, e.g., in the app's data directory
            var dataDirectory = FileSystem.Current.AppDataDirectory;
            filePath = Path.Combine("c:\\dev", "ecg_data.csv");

            // Create or reset the CSV file with headers
            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, "Timestamp,ECGValue\n"); // Write header row
            }
#endif
        }

        public async Task WriteEcgValueAsync(int ecgValue)
        {
            try
            {
                // Prepare the CSV line with timestamp and ECG integer value
                string timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffffK"); // ISO 8601 timestamp
                string csvLine = $"{timestamp},{ecgValue}\n";


#if WINDOWS
                // Write the CSV line to file asynchronously
                await File.AppendAllTextAsync(filePath , csvLine);
#endif
                Console.WriteLine("ECG data written: " + csvLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing ECG data to CSV: {ex.Message}");
            }
        }
    }
}


