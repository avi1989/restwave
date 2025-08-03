using CommunityToolkit.Mvvm.ComponentModel;

namespace RealRestClient.ViewModels.Requests;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string method = "GET";

    [ObservableProperty]
    private string url = "https://api.example.com/data";

    [ObservableProperty]
    private RequestViewModel request = new();
    
    public string[] Methods { get; } = ["GET", "PUT", "POST", "DELETE"];
}
