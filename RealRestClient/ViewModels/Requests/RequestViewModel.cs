using CommunityToolkit.Mvvm.ComponentModel;

namespace RealRestClient.ViewModels.Requests;

public partial class RequestViewModel : ViewModelBase
{
    [ObservableProperty] 
    private HeadersInputViewModel headersInput = new();

    [ObservableProperty] public JsonBodyInputViewModel jsonBodyInput = new();
}