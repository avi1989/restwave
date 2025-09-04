using CommunityToolkit.Mvvm.ComponentModel;

namespace RestWave.ViewModels.Requests;

public partial class RequestViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBodyEnabled))]
    private string method = "GET";

    [ObservableProperty] private string url = "";

    [ObservableProperty] private HeadersInputViewModel headersInput = new();

    [ObservableProperty] public JsonBodyInputViewModel jsonBodyInput = new();

    public bool IsBodyEnabled => Method is "POST" or "PUT";
}