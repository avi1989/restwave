namespace RestWave.Models
{
    public class Config
    {
        public string? RequestsDirectoryPath { get; set; }
        public System.Collections.Generic.List<string>? ExpandedFolders { get; set; }
        public string? LastOpenedFilePath { get; set; }
        public string Theme { get; set; } = "System";
        
        // History Settings
        public bool EnableHistory { get; set; } = true;
        public int MaxHistoryItems { get; set; } = 1000;
        public int HistoryRetentionDays { get; set; } = 30;
        public bool SaveResponseBodies { get; set; } = true;
        public bool SaveSensitiveData { get; set; } = false;
        
        // Session Settings
        public bool EnableSessionPersistence { get; set; } = true;
        public int SessionAutoSaveIntervalSeconds { get; set; } = 30;
        public bool RestoreLastSession { get; set; } = true;
        public int MaxRecentUrls { get; set; } = 20;
    }
}
