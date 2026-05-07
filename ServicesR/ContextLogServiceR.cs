using System.Text;

namespace CortriumBLE.Services;

public class ContextLogService
{
    private readonly List<string> rows = new();

    public void AddRow(DateTime timestamp, string activity, double motionScore)
    {
        rows.Add($"{timestamp:O},{activity},{motionScore:F5}");
    }

    public async Task<string> SaveAsync()
    {
        string fileName = $"context_log_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        string path = Path.Combine(FileSystem.AppDataDirectory, fileName);

        var csv = new StringBuilder();
        csv.AppendLine("timestamp,activity,motion_score");

        foreach (var row in rows)
            csv.AppendLine(row);

        await File.WriteAllTextAsync(path, csv.ToString());

        return path;
    }
}