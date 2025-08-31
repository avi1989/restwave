namespace RestWave.Models
{
    public class Config
    {
        public string? RequestsDirectoryPath { get; set; }
        public System.Collections.Generic.List<string>? ExpandedFolders { get; set; }
        public string? LastOpenedFilePath { get; set; }
        public string Theme { get; set; } = "System";
    }
}

