using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Threading;
using RestWave.ViewModels;
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

    private async void BtnCopyCurl_OnClick(object? sender, RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard == null) return;

        // Navigate up to the parent HttpViewModel to access the request data
        var parent = this.Parent;
        while (parent != null && parent.DataContext is not HttpViewModel)
        {
            parent = parent.Parent;
        }

        if (parent?.DataContext is not HttpViewModel httpVm) return;

        var request = httpVm.Request;
        var curl = BuildCurlCommand(request.Method, request.Url, request.HeadersInput, request.JsonBodyInput, request.IsBodyEnabled);
        await clipboard.SetTextAsync(curl);
    }

    private static string BuildCurlCommand(
        string method,
        string url,
        ViewModels.Requests.HeadersInputViewModel headersInput,
        ViewModels.Requests.JsonBodyInputViewModel jsonBodyInput,
        bool isBodyEnabled)
    {
        var sb = new StringBuilder();
        sb.Append("curl");

        if (method != "GET")
        {
            sb.Append($" -X {method}");
        }

        sb.Append($" '{EscapeSingleQuote(url)}'");

        foreach (var header in headersInput.Headers)
        {
            if (!string.IsNullOrWhiteSpace(header.Key) && !string.IsNullOrWhiteSpace(header.Value))
            {
                sb.Append($" \\\n  -H '{EscapeSingleQuote(header.Key)}: {EscapeSingleQuote(header.Value)}'");
            }
        }

        if (isBodyEnabled && !string.IsNullOrWhiteSpace(jsonBodyInput.JsonText))
        {
            sb.Append($" \\\n  -H 'Content-Type: application/json'");
            sb.Append($" \\\n  -d '{EscapeSingleQuote(jsonBodyInput.JsonText)}'");
        }

        return sb.ToString();
    }

    private static string EscapeSingleQuote(string value)
    {
        return value.Replace("'", "'\\''");
    }
}
