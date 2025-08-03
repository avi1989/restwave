using CommunityToolkit.Mvvm.ComponentModel;
using RealRestClient.ViewModels.Requests;
using RealRestClient.ViewModels.Responses;

namespace RealRestClient.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel()
    {
        // Initialize Response through the property, not the field
        Response = new ResponseViewModel();
    }

    [ObservableProperty] private RequestViewModel request = new();

    [ObservableProperty] private ResponseViewModel _response;

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