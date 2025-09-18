using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using RestWave.Models;
using RestWave.ViewModels.Requests;
using RestWave.ViewModels.Responses;

namespace RestWave.ViewModels;

public partial class HttpViewModel : ViewModelBase
{
    public HttpViewModel()
    {
        // Initialize Response through the property, not the field
        Request = new RequestViewModel();
        Response = new ResponseViewModel();
        Collections = new CollectionsViewModel();
        CurrentRequestHistory = new ObservableCollection<RequestHistoryEntry>();
    }

    [ObservableProperty] private RequestViewModel request;

    [ObservableProperty] private ResponseViewModel _response;

    [ObservableProperty] private CollectionsViewModel _collections;

    public string SubmitButtonText => this.Response.IsLoading ? "Cancel" : "Invoke";

    public string SubmitButtonIcon => this.Response.IsLoading ? "◼" : "▶";

    public string[] Methods { get; } = ["GET", "PUT", "POST", "DELETE"];

    [ObservableProperty] private ObservableCollection<RequestHistoryEntry> _currentRequestHistory = new();

    public void SetCurrentHistoryEntries(System.Collections.Generic.IReadOnlyList<RequestHistoryEntry> entries)
    {
        CurrentRequestHistory.Clear();
        foreach (var entry in entries)
        {
            CurrentRequestHistory.Add(entry);
        }
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