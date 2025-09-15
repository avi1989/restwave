using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using RestWave.Models;
using RestWave.ViewModels.Requests;

namespace RestWave.Services
{
    public class SessionManager : IDisposable
    {
        private readonly Config _config;
        private readonly string _sessionFilePath;
        private readonly Timer _autoSaveTimer;
        private SessionState _currentSession;
        private bool _isDirty = false;

        public SessionManager()
        {
            var configManager = new ConfigManager();
            _config = configManager.Current;
            
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RestWave");
            Directory.CreateDirectory(appDataPath);
            
            _sessionFilePath = Path.Combine(appDataPath, "session.json");
            _currentSession = new SessionState();
            
            // Set up auto-save timer
            _autoSaveTimer = new Timer(_config.SessionAutoSaveIntervalSeconds * 1000);
            _autoSaveTimer.Elapsed += OnAutoSaveTimer;
            
            if (_config.EnableSessionPersistence)
            {
                _autoSaveTimer.Start();
            }
        }

        public SessionState CurrentSession => _currentSession;

        public async Task LoadSessionAsync()
        {
            if (!_config.EnableSessionPersistence || !_config.RestoreLastSession)
                return;

            try
            {
                if (File.Exists(_sessionFilePath))
                {
                    var json = await File.ReadAllTextAsync(_sessionFilePath);
                    var session = JsonSerializer.Deserialize<SessionState>(json);
                    if (session != null)
                    {
                        _currentSession = session;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - we can continue with default session
                Console.WriteLine($"Failed to load session: {ex.Message}");
            }
        }

        public async Task SaveSessionAsync()
        {
            if (!_config.EnableSessionPersistence)
                return;

            try
            {
                _currentSession.LastSaved = DateTime.UtcNow;
                var json = JsonSerializer.Serialize(_currentSession, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                await File.WriteAllTextAsync(_sessionFilePath, json);
                _isDirty = false;
            }
            catch (Exception ex)
            {
                // Log error but don't throw
                Console.WriteLine($"Failed to save session: {ex.Message}");
            }
        }

        public void UpdateCurrentRequest(RequestViewModel request)
        {
            if (!_config.EnableSessionPersistence)
                return;

            _currentSession.CurrentUrl = request.Url;
            _currentSession.CurrentMethod = request.Method;
            _currentSession.CurrentHeaders = ConvertHeadersToDict(request.HeadersInput);
            _currentSession.CurrentBody = request.JsonBodyInput.JsonText;
            
            // Add to recent URLs if not already present
            if (!string.IsNullOrEmpty(request.Url) && !_currentSession.RecentUrls.Contains(request.Url))
            {
                _currentSession.RecentUrls.Insert(0, request.Url);
                
                // Keep only the most recent URLs
                while (_currentSession.RecentUrls.Count > _config.MaxRecentUrls)
                {
                    _currentSession.RecentUrls.RemoveAt(_currentSession.RecentUrls.Count - 1);
                }
            }
            
            MarkDirty();
        }

        public void UpdateLastOpenedRequest(string collectionName, string requestName)
        {
            if (!_config.EnableSessionPersistence)
                return;

            _currentSession.LastOpenedCollection = collectionName;
            _currentSession.LastOpenedRequest = requestName;
            MarkDirty();
        }

        public void UpdateExpandedFolders(System.Collections.Generic.List<string> expandedFolders)
        {
            if (!_config.EnableSessionPersistence)
                return;

            _currentSession.ExpandedFolders = new System.Collections.Generic.List<string>(expandedFolders);
            MarkDirty();
        }

        public void UpdateSelectedResponseTab(string tabName)
        {
            if (!_config.EnableSessionPersistence)
                return;

            _currentSession.SelectedResponseTab = tabName;
            MarkDirty();
        }

        public void UpdateWindowState(int width, int height, bool isMaximized)
        {
            if (!_config.EnableSessionPersistence)
                return;

            _currentSession.WindowWidth = width;
            _currentSession.WindowHeight = height;
            _currentSession.IsMaximized = isMaximized;
            MarkDirty();
        }

        public void UpdateUiState(string key, object value)
        {
            if (!_config.EnableSessionPersistence)
                return;

            _currentSession.UiState[key] = value;
            MarkDirty();
        }

        public T? GetUiState<T>(string key, T? defaultValue = default)
        {
            if (!_config.EnableSessionPersistence || !_currentSession.UiState.ContainsKey(key))
                return defaultValue;

            try
            {
                var value = _currentSession.UiState[key];
                if (value is JsonElement jsonElement)
                {
                    return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
                }
                return (T?)value;
            }
            catch
            {
                return defaultValue;
            }
        }

        public RequestViewModel RestoreCurrentRequest()
        {
            var request = new RequestViewModel
            {
                Url = _currentSession.CurrentUrl,
                Method = _currentSession.CurrentMethod,
                JsonBodyInput = new JsonBodyInputViewModel { JsonText = _currentSession.CurrentBody }
            };

            // Restore headers
            foreach (var header in _currentSession.CurrentHeaders)
            {
                request.HeadersInput.Headers.Add(new HttpHeaderItemViewModel 
                { 
                    Key = header.Key, 
                    Value = header.Value 
                });
            }

            return request;
        }

        public async Task ClearSessionAsync()
        {
            _currentSession = new SessionState();
            _isDirty = true;
            await SaveSessionAsync();
        }

        private void MarkDirty()
        {
            _isDirty = true;
        }

        private async void OnAutoSaveTimer(object? sender, ElapsedEventArgs e)
        {
            if (_isDirty)
            {
                await SaveSessionAsync();
            }
        }

        private System.Collections.Generic.Dictionary<string, string> ConvertHeadersToDict(HeadersInputViewModel headersInput)
        {
            var headers = new System.Collections.Generic.Dictionary<string, string>();
            foreach (var header in headersInput.Headers)
            {
                if (!string.IsNullOrEmpty(header.Key) && !string.IsNullOrEmpty(header.Value))
                {
                    headers[header.Key] = header.Value;
                }
            }
            return headers;
        }

        public void Dispose()
        {
            _autoSaveTimer?.Stop();
            _autoSaveTimer?.Dispose();
            
            // Save session on dispose if dirty
            if (_isDirty && _config.EnableSessionPersistence)
            {
                SaveSessionAsync().Wait();
            }
        }
    }
}
