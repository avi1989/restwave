using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RestWave.ViewModels.Requests;

public partial class HeadersInputViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<HttpHeaderItemViewModel> headers = new() { new HttpHeaderItemViewModel()
    {
        Key = "",
        Value = ""
    }};

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
