using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using Avalonia.Controls;
using Avalonia.Interactivity;
using RealRestClient.ViewModels;
using MainWindowViewModel = RealRestClient.ViewModels.Requests.MainWindowViewModel;

namespace RealRestClient.Views;

public partial class MainWindow : Window
{
    public static HttpClient HttpClient { get; } = new();
    
    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void BtnInvoke_OnClick(object? sender, RoutedEventArgs e)
    {
        // var response = await HttpClient.SendAsync(new HttpRequestMessage(new HttpMethod(this.ViewModel.Method), this.ViewModel.Url));
        // if (response.IsSuccessStatusCode)
        // {
        //     var content = await response.Content.ReadAsStringAsync();
        //     Debug.WriteLine($"Response: {content}");
        // }
        // else
        // {
        //     Debug.WriteLine($"Error: {response.StatusCode}");
        // }
        Debug.WriteLine($"Method: {this.ViewModel.Method}, Url: {this.ViewModel.Url}, Headers: {string.Join(", ", this.ViewModel.Request.HeadersInput.Headers.Select(h => $"{h.Key}: {h.Value}"))}");
    }
}