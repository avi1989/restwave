using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestWave.Models;
using RestWave.Services;
using RestWave.ViewModels.Requests;
using RestWave.ViewModels.Responses;

namespace RestWave.ViewModels;

public partial class HttpViewModel : ViewModelBase
{
    private readonly HistoryManager _historyManager;

    public HttpViewModel()
    {
        // Initialize Response through the property, not the field
        Request = new RequestViewModel();
        Response = new ResponseViewModel();
        Collections = new CollectionsViewModel();
        RequestHistory = new ObservableCollection<RequestHistoryItem>();
        _historyManager = new HistoryManager();

        // Subscribe to URL changes to load request-specific history
        Request.PropertyChanged += async (s, e) =>
        {
            if (e.PropertyName == nameof(RequestViewModel.Url))
            {
                await LoadRequestHistoryAsync();
            }
        };
    }

    [ObservableProperty] private RequestViewModel request;

    partial void OnRequestChanged(RequestViewModel value)
    {
        // Subscribe to property changes on the new RequestViewModel
        value.PropertyChanged += async (s, e) =>
        {
            if (e.PropertyName == nameof(RequestViewModel.Url))
            {
                await LoadRequestHistoryAsync();
            }
        };
        
        // Load history for the new request immediately
        _ = LoadRequestHistoryAsync();
    }

    [ObservableProperty] private ResponseViewModel _response;

    [ObservableProperty] private CollectionsViewModel _collections;

    [ObservableProperty] private ObservableCollection<RequestHistoryItem> _requestHistory;

    [ObservableProperty] private bool _isHistoryPaneVisible = false;

    public string SubmitButtonText => this.Response.IsLoading ? "Cancel" : "Invoke";

    public string SubmitButtonIcon => this.Response.IsLoading ? "◼" : "▶";

    public string[] Methods { get; } = ["GET", "PUT", "POST", "DELETE"];

    [RelayCommand]
    public void ReplayHistory(RequestHistoryItem historyItem)
    {
        if (historyItem == null) return;

        var historyViewModel = new HistoryViewModel();
        var replayedRequest = historyViewModel.ReplayRequest(historyItem);
        if (replayedRequest != null)
        {
            Request = replayedRequest;
            
            // Reset response state and populate with historical response
            Response.IsStreaming = false; // Historical responses are never streaming
            Response.StatusCode = historyItem.StatusCode;
            Response.Body = historyItem.ResponseBody;
            Response.Headers.Clear();
            
            foreach (var header in historyItem.ResponseHeaders)
            {
                Response.Headers.Add(new KeyValuePair<string, string>(header.Key, header.Value));
            }

            // Try to find and select the corresponding request in collections
            if (!string.IsNullOrEmpty(historyItem.CollectionName) && !string.IsNullOrEmpty(historyItem.RequestName))
            {
                Collections.SelectRequestByName(historyItem.CollectionName, historyItem.RequestName);
            }
        }
    }

    [RelayCommand]
    public void ShowHistoryPane()
    {
        IsHistoryPaneVisible = true;
    }

    [RelayCommand]
    public void HideHistoryPane()
    {
        IsHistoryPaneVisible = false;
    }

    private async Task LoadRequestHistoryAsync()
    {
        if (string.IsNullOrEmpty(Request.Url))
        {
            RequestHistory.Clear();
            return;
        }

        try
        {
            // Get history for this specific URL
            var history = await _historyManager.GetHistoryAsync(20, 0, Request.Url);
            
            RequestHistory.Clear();
            foreach (var item in history)
            {
                RequestHistory.Add(item);
            }
        }
        catch(Exception e)
        {
            Console.WriteLine(e);
            // Handle error silently
            RequestHistory.Clear();
        }
    }

    public async Task RefreshRequestHistoryAsync()
    {
        await LoadRequestHistoryAsync();
    }

    partial void OnResponseChanged(ResponseViewModel value)
    {
        // Subscribe to property changes on the new ResponseViewModel
        value.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ResponseViewModel.IsLoading))
            {
                OnPropertyChanged(nameof(SubmitButtonText));
                OnPropertyChanged(nameof(SubmitButtonIcon));
            }
        };
    }
}
