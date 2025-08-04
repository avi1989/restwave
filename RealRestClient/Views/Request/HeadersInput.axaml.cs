using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using HeadersInputViewModel = RealRestClient.ViewModels.Requests.HeadersInputViewModel;
using HttpHeaderItemViewModel = RealRestClient.ViewModels.Requests.HttpHeaderItemViewModel;

namespace RealRestClient.Views.Request;

public partial class HeadersInput : UserControl
{
    private HeadersInputViewModel? ViewModel => DataContext as HeadersInputViewModel;

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
        if (ViewModel is not null && sender is Button button && button.DataContext is HttpHeaderItemViewModel header)
        {
            ViewModel.RemoveHeader(header);
        }
    }
}