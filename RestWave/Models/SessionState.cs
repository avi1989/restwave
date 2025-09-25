using System;
using System.Collections.Generic;

namespace RestWave.Models
{
    public class SessionState
    {
        public DateTime LastSaved { get; set; } = DateTime.UtcNow;
        public string CurrentUrl { get; set; } = string.Empty;
        public string CurrentMethod { get; set; } = "GET";
        public Dictionary<string, string> CurrentHeaders { get; set; } = new();
        public string CurrentBody { get; set; } = string.Empty;
        public string LastOpenedCollection { get; set; } = string.Empty;
        public string LastOpenedRequest { get; set; } = string.Empty;
        public List<string> ExpandedFolders { get; set; } = new();
        public List<string> RecentUrls { get; set; } = new();
        public string SelectedResponseTab { get; set; } = string.Empty;
        public Dictionary<string, object> UiState { get; set; } = new();
        public int WindowWidth { get; set; } = 1200;
        public int WindowHeight { get; set; } = 800;
        public bool IsMaximized { get; set; } = false;
    }
}
