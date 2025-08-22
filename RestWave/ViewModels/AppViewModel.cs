using CommunityToolkit.Mvvm.ComponentModel;

namespace RestWave.ViewModels;

public partial class AppViewModel : ViewModelBase
{
    [ObservableProperty] private HttpViewModel httpViewModel = new();
}