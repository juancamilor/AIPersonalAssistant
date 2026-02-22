namespace AIPersonalAssistant.Web.Models;

public class MenopauseData
{
    public MenopauseProfile Profile { get; set; } = new();
    public List<DailyCheckIn> CheckIns { get; set; } = new();
    public List<SymptomEntry> Symptoms { get; set; } = new();
    public List<HotFlashEntry> HotFlashes { get; set; } = new();
    public List<SleepLog> SleepLogs { get; set; } = new();
}

public class MenopauseProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string ShareToken { get; set; } = Guid.NewGuid().ToString();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class DailyCheckIn
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Date { get; set; } = string.Empty;
    public int Mood { get; set; }
    public int Energy { get; set; }
    public int SleepQuality { get; set; }
    public int OverallWellness { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class SymptomEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Date { get; set; } = string.Empty;
    public string Symptom { get; set; } = string.Empty;
    public int Severity { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string Trigger { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class HotFlashEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Date { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public int Severity { get; set; }
    public string Duration { get; set; } = string.Empty;
    public string Trigger { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class SleepLog
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Date { get; set; } = string.Empty;
    public string BedTime { get; set; } = string.Empty;
    public string WakeTime { get; set; } = string.Empty;
    public int Quality { get; set; }
    public int NightSweats { get; set; }
    public int Interruptions { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class DailyCheckInRequest
{
    public string Date { get; set; } = string.Empty;
    public int Mood { get; set; }
    public int Energy { get; set; }
    public int SleepQuality { get; set; }
    public int OverallWellness { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class SymptomEntryRequest
{
    public string Date { get; set; } = string.Empty;
    public string Symptom { get; set; } = string.Empty;
    public int Severity { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string Trigger { get; set; } = string.Empty;
}

public class HotFlashEntryRequest
{
    public string Date { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public int Severity { get; set; }
    public string Duration { get; set; } = string.Empty;
    public string Trigger { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class SleepLogRequest
{
    public string Date { get; set; } = string.Empty;
    public string BedTime { get; set; } = string.Empty;
    public string WakeTime { get; set; } = string.Empty;
    public int Quality { get; set; }
    public int NightSweats { get; set; }
    public int Interruptions { get; set; }
    public string Notes { get; set; } = string.Empty;
}
