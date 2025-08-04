using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using Avalonia.Controls;
using Avalonia.Interactivity;
using RealRestClient.Models;
using RealRestClient.Services;
using RealRestClient.ViewModels;

namespace RealRestClient.Views;

public partial class HttpView : UserControl
{
    private readonly ConfigManager configManager;
    private Config config;

    public HttpView()
    {
        InitializeComponent();
        this.configManager = new ConfigManager();
        this.config = configManager.LoadConfiguration();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        RequestsManager requestsManager = new();
        var collections = requestsManager.GetCollections();
        foreach(var collection in collections)
        {
            this.ViewModel.Collections.Add(collection);
        }
    }

    public static HttpClient HttpClient { get; } = new();

    private HttpViewModel ViewModel => (HttpViewModel)DataContext!;
    
    private async void BtnInvoke_OnClick(object? sender, RoutedEventArgs e)
    {
        if (this.ViewModel.Response.IsLoading)
        {
            // If currently loading, cancel the operation
            this.ViewModel.Response.CancelOperation();
            return;
        }

        this.ViewModel.Response.StatusCode = string.Empty;
        this.ViewModel.Response.Body = string.Empty;
        this.ViewModel.Response.Headers.Clear();
        this.ViewModel.Response.StartOperation(); // This creates the cancellation token and sets IsLoading = true
        try
        {
            this.ViewModel.Response.IsLoading = true;
            var request = new HttpRequestMessage(new HttpMethod(this.ViewModel.Request.Method),
                this.ViewModel.Request.Url);
            foreach (var header in this.ViewModel.Request.HeadersInput.Headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }

            if (this.ViewModel.Request.IsBodyEnabled)
            {
                request.Content = new StringContent(this.ViewModel.Request.JsonBodyInput.JsonText,
                    System.Text.Encoding.UTF8, "application/json");
            }

            var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
                this.ViewModel.Response.CancellationToken);
            var responseContentType = response.Content.Headers.ContentType?.MediaType ?? "text/plain";

            this.ViewModel.Response.StatusCode = response.StatusCode.ToString();
            foreach (var header in response.Headers)
            {
                var newItem = new KeyValuePair<string, string>(header.Key, string.Join(", ", header.Value.ToList()));
                this.ViewModel.Response.Headers.Add(newItem);
            }

            if (responseContentType != "text/event-stream")
            {
                var content = await response.Content.ReadAsStringAsync();
                this.ViewModel.Response.Body = response.IsSuccessStatusCode
                    ? content
                    : $"Error {response.StatusCode}: {content}";
            }
            else
            {
                await using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);

                while (!this.ViewModel.Response.CancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync();
                    if (line == null) break;

                    if (!string.IsNullOrEmpty(line))
                    {
                        this.ViewModel.Response.Body += line + Environment.NewLine;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            this.ViewModel.Response.Body = $"Exception: {ex.Message}";
            Debug.WriteLine($"Exception: {ex.Message}");
        }
        finally
        {
            this.ViewModel.Response.CompleteOperation(); // This sets IsLoading = false and cleans up
        }
    }
}