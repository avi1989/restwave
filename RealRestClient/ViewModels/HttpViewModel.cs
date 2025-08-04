using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using RealRestClient.Models;
using RealRestClient.ViewModels.Requests;
using RealRestClient.ViewModels.Responses;

namespace RealRestClient.ViewModels;

public partial class HttpViewModel : ViewModelBase
{
    public HttpViewModel()
    {
        // Initialize Response through the property, not the field
        Request = new RequestViewModel();
        Response = new ResponseViewModel();
        Collections = new ObservableCollection<Node>();
    }

    [ObservableProperty] private RequestViewModel request;

    [ObservableProperty] private ResponseViewModel _response;
    
    [ObservableProperty] private ObservableCollection<Node> _collections;

    public string SubmitButtonText => this.Response.IsLoading ? "Cancel" : "Invoke";

    public string[] Methods { get; } = ["GET", "PUT", "POST", "DELETE"];
    
    partial void OnResponseChanged(ResponseViewModel value)
    {
        // Subscribe to property changes on the new ResponseViewModel
        value.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ResponseViewModel.IsLoading))
            {
                OnPropertyChanged(nameof(SubmitButtonText));
            }
        };
    }
}