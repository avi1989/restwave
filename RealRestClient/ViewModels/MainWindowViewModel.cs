using CommunityToolkit.Mvvm.ComponentModel;
using RealRestClient.ViewModels.Requests;
using RealRestClient.ViewModels.Responses;

namespace RealRestClient.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private RequestViewModel request = new();
    
    [ObservableProperty]
    private ResponseViewModel _response = new();
    
    public string[] Methods { get; } = ["GET", "PUT", "POST", "DELETE"];
}
