using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Threading;
using RestWave.ViewModels.Responses;

namespace RestWave.Views.Response;

public partial class ResponseView : UserControl
{
    public ResponseView()
    {
        InitializeComponent();
    }

    private async void BtnCopy_OnClick(object? sender, RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard == null) return;

        var vm = DataContext as ResponseViewModel;
        if (vm == null) return;

        var text = vm.BodyDocument.Text ?? string.Empty;
        await clipboard.SetTextAsync(text);
    }
}