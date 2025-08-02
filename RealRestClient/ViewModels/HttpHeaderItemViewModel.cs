using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RealRestClient.ViewModels;

public partial class HttpHeaderItemViewModel : ObservableObject
{
    private string _key = string.Empty;
    private string _value = string.Empty;

    [ObservableProperty] private string key = string.Empty;

    [ObservableProperty] private string value = string.Empty;
}
