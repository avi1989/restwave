using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using HeadersInputViewModel = RestWave.ViewModels.Requests.HeadersInputViewModel;
using HttpHeaderItemViewModel = RestWave.ViewModels.Requests.HttpHeaderItemViewModel;
using Requests_HeadersInputViewModel = RestWave.ViewModels.Requests.HeadersInputViewModel;
using Requests_HttpHeaderItemViewModel = RestWave.ViewModels.Requests.HttpHeaderItemViewModel;

namespace RestWave.Views.Request;

public partial class HeadersInput : UserControl
{
    private Requests_HeadersInputViewModel? ViewModel => DataContext as Requests_HeadersInputViewModel;

    public HeadersInput()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void AddHeader_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is not null)
        {
            ViewModel.AddNewHeader();
        }
    }

    private void RemoveHeader_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is not null && sender is Button button && button.DataContext is Requests_HttpHeaderItemViewModel header)
        {
            ViewModel.RemoveHeader(header);
        }
    }
}