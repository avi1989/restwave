using CommunityToolkit.Mvvm.ComponentModel;

namespace RealRestClient.ViewModels;

public partial class AppViewModel : ViewModelBase
{
    [ObservableProperty] private HttpViewModel httpViewModel = new();
}