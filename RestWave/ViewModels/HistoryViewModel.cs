using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestWave.Models;
using RestWave.Services;
using RestWave.ViewModels.Requests;

namespace RestWave.ViewModels
{
    public partial class HistoryViewModel : ViewModelBase
    {
        private readonly HistoryManager _historyManager;

        [ObservableProperty]
        private ObservableCollection<RequestHistoryItem> _historyItems = new();

        [ObservableProperty]
        private RequestHistoryItem? _selectedHistoryItem;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _selectedMethodFilter = "All";

        [ObservableProperty]
        private string _selectedStatusFilter = "All";

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private int _currentPage = 0;

        [ObservableProperty]
        private int _itemsPerPage = 50;

        [ObservableProperty]
        private bool _hasMoreItems = true;

        public string[] MethodFilters { get; } = { "All", "GET", "POST", "PUT", "DELETE" };
        public string[] StatusFilters { get; } = { "All", "2xx", "3xx", "4xx", "5xx" };

        public HistoryViewModel()
        {
            _historyManager = new HistoryManager();
        }

        [RelayCommand]
        public async Task LoadHistoryAsync()
        {
            if (IsLoading) return;

            IsLoading = true;
            try
            {
                var urlFilter = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText;
                var methodFilter = SelectedMethodFilter == "All" ? null : SelectedMethodFilter;
                var statusFilter = SelectedStatusFilter == "All" ? null : SelectedStatusFilter;

                var items = await _historyManager.GetHistoryAsync(
                    ItemsPerPage, 
                    CurrentPage * ItemsPerPage, 
                    urlFilter, 
                    methodFilter, 
                    statusFilter);

                if (CurrentPage == 0)
                {
                    HistoryItems.Clear();
                }

                foreach (var item in items)
                {
                    HistoryItems.Add(item);
                }

                HasMoreItems = items.Count == ItemsPerPage;
            }
            catch (Exception ex)
            {
                // Handle error - could show a message to user
                System.Diagnostics.Debug.WriteLine($"Failed to load history: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task LoadMoreAsync()
        {
            if (!HasMoreItems || IsLoading) return;

            CurrentPage++;
            await LoadHistoryAsync();
        }

        [RelayCommand]
        public async Task RefreshAsync()
        {
            CurrentPage = 0;
            await LoadHistoryAsync();
        }

        [RelayCommand]
        public async Task SearchAsync()
        {
            CurrentPage = 0;
            await LoadHistoryAsync();
        }

        [RelayCommand]
        public async Task DeleteHistoryItemAsync(RequestHistoryItem? item)
        {
            if (item == null) return;

            try
            {
                await _historyManager.DeleteHistoryItemAsync(item.Id);
                HistoryItems.Remove(item);
                
                if (SelectedHistoryItem == item)
                {
                    SelectedHistoryItem = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to delete history item: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task ClearAllHistoryAsync()
        {
            try
            {
                await _historyManager.ClearHistoryAsync();
                HistoryItems.Clear();
                SelectedHistoryItem = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to clear history: {ex.Message}");
            }
        }

        public RequestViewModel? ReplayRequest(RequestHistoryItem? item)
        {
            if (item == null) return null;

            var request = new RequestViewModel
            {
                Url = item.Url,
                Method = item.Method,
                JsonBodyInput = new JsonBodyInputViewModel { JsonText = item.Body }
            };

            // Restore headers
            foreach (var header in item.Headers)
            {
                request.HeadersInput.Headers.Add(new HttpHeaderItemViewModel 
                { 
                    Key = header.Key, 
                    Value = header.Value 
                });
            }

            return request;
        }

        public string FormatTimestamp(DateTime timestamp)
        {
            var now = DateTime.UtcNow;
            var diff = now - timestamp;

            if (diff.TotalMinutes < 1)
                return "Just now";
            if (diff.TotalMinutes < 60)
                return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24)
                return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays < 7)
                return $"{(int)diff.TotalDays}d ago";
            
            return timestamp.ToLocalTime().ToString("MMM dd, yyyy");
        }

        public string FormatResponseTime(long milliseconds)
        {
            if (milliseconds < 1000)
                return $"{milliseconds}ms";
            
            return $"{milliseconds / 1000.0:F1}s";
        }

        public string FormatResponseSize(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024.0:F1} KB";
            
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        }

        public string GetStatusCodeColor(string statusCode)
        {
            if (statusCode.StartsWith("2"))
                return "Green";
            if (statusCode.StartsWith("3"))
                return "Orange";
            if (statusCode.StartsWith("4") || statusCode.StartsWith("5"))
                return "Red";
            
            return "Gray";
        }

        partial void OnSelectedMethodFilterChanged(string value)
        {
            _ = SearchAsync();
        }

        partial void OnSelectedStatusFilterChanged(string value)
        {
            _ = SearchAsync();
        }
    }
}
