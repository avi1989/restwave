using CommunityToolkit.Mvvm.ComponentModel;

namespace RealRestClient.ViewModels.Requests;

public partial class RequestViewModel : ViewModelBase
{
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsBodyEnabled))]
    private string method = "POST";

    [ObservableProperty] private string url = "https://echo.websocket.org/.sse";

    [ObservableProperty] private HeadersInputViewModel headersInput = new();

    [ObservableProperty] public JsonBodyInputViewModel jsonBodyInput = new();

    public bool IsBodyEnabled => Method is "POST" or "PUT";
}