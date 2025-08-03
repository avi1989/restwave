using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using RealRestClient.ViewModels;
using MainWindowViewModel = RealRestClient.ViewModels.MainWindowViewModel;

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
        this.ViewModel.Response.Body = "Testing binding...";
        Debug.WriteLine($"Set Body to: {this.ViewModel.Response.Body}");
    
        // Add a small delay to see if the test value appears
        await Task.Delay(1000);
    
        try
        {
            var request = new HttpRequestMessage(new HttpMethod(this.ViewModel.Request.Method), this.ViewModel.Request.Url);
            foreach (var header in this.ViewModel.Request.HeadersInput.Headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }
        
            if (this.ViewModel.Request.IsBodyEnabled)
            {
                request.Content = new StringContent(this.ViewModel.Request.JsonBodyInput.JsonText, System.Text.Encoding.UTF8, "application/json");
            }
        
            var response = await HttpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            this.ViewModel.Response.StatusCode = response.StatusCode.ToString();

            foreach (var header in response.Headers)
            {
                var newItem = new KeyValuePair<string, string>(header.Key, string.Join(", ", header.Value.ToList()));
                this.ViewModel.Response.Headers.Add(newItem);
            }
            
            this.ViewModel.Response.Body = response.IsSuccessStatusCode 
                ? content 
                : $"Error {response.StatusCode}: {content}";
            
            Debug.WriteLine($"Final Body value: {this.ViewModel.Response.Body}");
        }
        catch (Exception ex)
        {
            this.ViewModel.Response.Body = $"Exception: {ex.Message}";
            Debug.WriteLine($"Exception: {ex.Message}");
        }

    }
}