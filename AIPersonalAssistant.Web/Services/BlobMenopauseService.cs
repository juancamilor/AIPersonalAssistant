using AIPersonalAssistant.Web.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Text.Json;

namespace AIPersonalAssistant.Web.Services;

public class BlobMenopauseService : IMenopauseService
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<BlobMenopauseService> _logger;

    public BlobMenopauseService(IConfiguration configuration, ILogger<BlobMenopauseService> logger)
    {
        _logger = logger;

        var connectionString = configuration["AzureStorage:ConnectionString"];
        var containerName = "menopause";

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("AzureStorage:ConnectionString is not configured");
        }

        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        _containerClient.CreateIfNotExists();
    }

    private string GetBlobName(string userId)
    {
        var safeUserId = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(userId))
            .Replace("/", "_").Replace("+", "-");
        return $"{safeUserId}.json";
    }

    private async Task<MenopauseData> LoadDataAsync(string userId)
    {
        var blobName = GetBlobName(userId);
        var blobClient = _containerClient.GetBlobClient(blobName);

        try
        {
            if (!await blobClient.ExistsAsync())
            {
                return new MenopauseData
                {
                    Profile = new MenopauseProfile
                    {
                        UserId = userId
                    }
                };
            }

            var response = await blobClient.DownloadContentAsync();
            var json = response.Value.Content.ToString();
            return JsonSerializer.Deserialize<MenopauseData>(json) ?? new MenopauseData
            {
                Profile = new MenopauseProfile
                {
                    UserId = userId
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading menopause data for user {UserId}", userId);
            return new MenopauseData
            {
                Profile = new MenopauseProfile
                {
                    UserId = userId
                }
            };
        }
    }

    private async Task SaveDataAsync(string userId, MenopauseData data)
    {
        var blobName = GetBlobName(userId);
        var blobClient = _containerClient.GetBlobClient(blobName);

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        var content = BinaryData.FromString(json);

        await blobClient.UploadAsync(content, overwrite: true);
    }

    public async Task<MenopauseData> GetDataAsync(string userId)
    {
        return await LoadDataAsync(userId);
    }

    public async Task<MenopauseData?> GetByShareTokenAsync(string shareToken)
    {
        try
        {
            await foreach (var blobItem in _containerClient.GetBlobsAsync())
            {
                var blobClient = _containerClient.GetBlobClient(blobItem.Name);
                var response = await blobClient.DownloadContentAsync();
                var json = response.Value.Content.ToString();
                var data = JsonSerializer.Deserialize<MenopauseData>(json);
                if (data?.Profile?.ShareToken == shareToken)
                {
                    return data;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching menopause data by share token");
        }

        return null;
    }

    public async Task<DailyCheckIn> AddCheckInAsync(string userId, DailyCheckInRequest request)
    {
        var data = await LoadDataAsync(userId);

        var checkIn = new DailyCheckIn
        {
            Id = Guid.NewGuid().ToString(),
            Date = request.Date,
            Mood = request.Mood,
            Energy = request.Energy,
            SleepQuality = request.SleepQuality,
            OverallWellness = request.OverallWellness,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        data.CheckIns.Add(checkIn);
        await SaveDataAsync(userId, data);

        return checkIn;
    }

    public async Task<DailyCheckIn?> UpdateCheckInAsync(string userId, string id, DailyCheckInRequest request)
    {
        var data = await LoadDataAsync(userId);
        var checkIn = data.CheckIns.FirstOrDefault(c => c.Id == id);

        if (checkIn == null)
        {
            return null;
        }

        checkIn.Date = request.Date;
        checkIn.Mood = request.Mood;
        checkIn.Energy = request.Energy;
        checkIn.SleepQuality = request.SleepQuality;
        checkIn.OverallWellness = request.OverallWellness;
        checkIn.Notes = request.Notes;

        await SaveDataAsync(userId, data);

        return checkIn;
    }

    public async Task<bool> DeleteCheckInAsync(string userId, string id)
    {
        var data = await LoadDataAsync(userId);
        var checkIn = data.CheckIns.FirstOrDefault(c => c.Id == id);

        if (checkIn == null)
        {
            return false;
        }

        data.CheckIns.Remove(checkIn);
        await SaveDataAsync(userId, data);

        return true;
    }

    public async Task<SymptomEntry> AddSymptomAsync(string userId, SymptomEntryRequest request)
    {
        var data = await LoadDataAsync(userId);

        var symptom = new SymptomEntry
        {
            Id = Guid.NewGuid().ToString(),
            Date = request.Date,
            Symptom = request.Symptom,
            Severity = request.Severity,
            Notes = request.Notes,
            Time = request.Time,
            Trigger = request.Trigger,
            CreatedAt = DateTime.UtcNow
        };

        data.Symptoms.Add(symptom);
        await SaveDataAsync(userId, data);

        return symptom;
    }

    public async Task<SymptomEntry?> UpdateSymptomAsync(string userId, string id, SymptomEntryRequest request)
    {
        var data = await LoadDataAsync(userId);
        var symptom = data.Symptoms.FirstOrDefault(s => s.Id == id);

        if (symptom == null)
        {
            return null;
        }

        symptom.Date = request.Date;
        symptom.Symptom = request.Symptom;
        symptom.Severity = request.Severity;
        symptom.Notes = request.Notes;
        symptom.Time = request.Time;
        symptom.Trigger = request.Trigger;

        await SaveDataAsync(userId, data);

        return symptom;
    }

    public async Task<bool> DeleteSymptomAsync(string userId, string id)
    {
        var data = await LoadDataAsync(userId);
        var symptom = data.Symptoms.FirstOrDefault(s => s.Id == id);

        if (symptom == null)
        {
            return false;
        }

        data.Symptoms.Remove(symptom);
        await SaveDataAsync(userId, data);

        return true;
    }

    public async Task<HotFlashEntry> AddHotFlashAsync(string userId, HotFlashEntryRequest request)
    {
        var data = await LoadDataAsync(userId);

        var hotFlash = new HotFlashEntry
        {
            Id = Guid.NewGuid().ToString(),
            Date = request.Date,
            Time = request.Time,
            Severity = request.Severity,
            Duration = request.Duration,
            Trigger = request.Trigger,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        data.HotFlashes.Add(hotFlash);
        await SaveDataAsync(userId, data);

        return hotFlash;
    }

    public async Task<HotFlashEntry?> UpdateHotFlashAsync(string userId, string id, HotFlashEntryRequest request)
    {
        var data = await LoadDataAsync(userId);
        var hotFlash = data.HotFlashes.FirstOrDefault(h => h.Id == id);

        if (hotFlash == null)
        {
            return null;
        }

        hotFlash.Date = request.Date;
        hotFlash.Time = request.Time;
        hotFlash.Severity = request.Severity;
        hotFlash.Duration = request.Duration;
        hotFlash.Trigger = request.Trigger;
        hotFlash.Notes = request.Notes;

        await SaveDataAsync(userId, data);

        return hotFlash;
    }

    public async Task<bool> DeleteHotFlashAsync(string userId, string id)
    {
        var data = await LoadDataAsync(userId);
        var hotFlash = data.HotFlashes.FirstOrDefault(h => h.Id == id);

        if (hotFlash == null)
        {
            return false;
        }

        data.HotFlashes.Remove(hotFlash);
        await SaveDataAsync(userId, data);

        return true;
    }

    public async Task<SleepLog> AddSleepLogAsync(string userId, SleepLogRequest request)
    {
        var data = await LoadDataAsync(userId);

        var sleepLog = new SleepLog
        {
            Id = Guid.NewGuid().ToString(),
            Date = request.Date,
            BedTime = request.BedTime,
            WakeTime = request.WakeTime,
            Quality = request.Quality,
            NightSweats = request.NightSweats,
            Interruptions = request.Interruptions,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        data.SleepLogs.Add(sleepLog);
        await SaveDataAsync(userId, data);

        return sleepLog;
    }

    public async Task<SleepLog?> UpdateSleepLogAsync(string userId, string id, SleepLogRequest request)
    {
        var data = await LoadDataAsync(userId);
        var sleepLog = data.SleepLogs.FirstOrDefault(s => s.Id == id);

        if (sleepLog == null)
        {
            return null;
        }

        sleepLog.Date = request.Date;
        sleepLog.BedTime = request.BedTime;
        sleepLog.WakeTime = request.WakeTime;
        sleepLog.Quality = request.Quality;
        sleepLog.NightSweats = request.NightSweats;
        sleepLog.Interruptions = request.Interruptions;
        sleepLog.Notes = request.Notes;

        await SaveDataAsync(userId, data);

        return sleepLog;
    }

    public async Task<bool> DeleteSleepLogAsync(string userId, string id)
    {
        var data = await LoadDataAsync(userId);
        var sleepLog = data.SleepLogs.FirstOrDefault(s => s.Id == id);

        if (sleepLog == null)
        {
            return false;
        }

        data.SleepLogs.Remove(sleepLog);
        await SaveDataAsync(userId, data);

        return true;
    }
}
