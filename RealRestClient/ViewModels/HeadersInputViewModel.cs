using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RealRestClient.ViewModels;

public partial class HeadersInputViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<HttpHeaderItemViewModel> headers = new() { new HttpHeaderItemViewModel() };

    public void AddNewHeader()
    {
        Headers.Add(new HttpHeaderItemViewModel());
    }

    public void RemoveHeader(HttpHeaderItemViewModel header)
    {
        if (Headers.Contains(header))
        {
            Headers.Remove(header);
        }
    }
}
