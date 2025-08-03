using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RealRestClient.ViewModels.Responses;

public partial class ResponseViewModel : ViewModelBase
{
    [ObservableProperty] private string _statusCode = string.Empty;
    
    [ObservableProperty] private string _body = string.Empty;
    
    [ObservableProperty]
    private ObservableCollection<KeyValuePair<string, string>> _headers = new();
}