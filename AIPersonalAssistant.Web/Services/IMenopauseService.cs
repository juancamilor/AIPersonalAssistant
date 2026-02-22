using AIPersonalAssistant.Web.Models;

namespace AIPersonalAssistant.Web.Services;

public interface IMenopauseService
{
    Task<MenopauseData> GetDataAsync(string userId);
    Task<MenopauseData?> GetByShareTokenAsync(string shareToken);

    Task<DailyCheckIn> AddCheckInAsync(string userId, DailyCheckInRequest request);
    Task<DailyCheckIn?> UpdateCheckInAsync(string userId, string id, DailyCheckInRequest request);
    Task<bool> DeleteCheckInAsync(string userId, string id);

    Task<SymptomEntry> AddSymptomAsync(string userId, SymptomEntryRequest request);
    Task<SymptomEntry?> UpdateSymptomAsync(string userId, string id, SymptomEntryRequest request);
    Task<bool> DeleteSymptomAsync(string userId, string id);

    Task<HotFlashEntry> AddHotFlashAsync(string userId, HotFlashEntryRequest request);
    Task<HotFlashEntry?> UpdateHotFlashAsync(string userId, string id, HotFlashEntryRequest request);
    Task<bool> DeleteHotFlashAsync(string userId, string id);

    Task<SleepLog> AddSleepLogAsync(string userId, SleepLogRequest request);
    Task<SleepLog?> UpdateSleepLogAsync(string userId, string id, SleepLogRequest request);
    Task<bool> DeleteSleepLogAsync(string userId, string id);
}
