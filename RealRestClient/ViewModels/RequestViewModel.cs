using CommunityToolkit.Mvvm.ComponentModel;

namespace RealRestClient.ViewModels;

public partial class RequestViewModel : ViewModelBase
{
    [ObservableProperty] 
    private HeadersInputViewModel headersInput = new();
}