using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using RealRestClient.Models;
using RealRestClient.Services;
using RealRestClient.ViewModels;

namespace RealRestClient.Views;

public static class VisualTreeExtensions
{
    public static T? FindDescendantOfType<T>(this Visual visual) where T : class
    {
        if (visual is T result)
            return result;

        foreach (var child in visual.GetVisualChildren())
        {
            var descendant = child.FindDescendantOfType<T>();
            if (descendant != null)
                return descendant;
        }

        return null;
    }

    public static IEnumerable<T> FindDescendantsOfType<T>(this Visual visual) where T : class
    {
        if (visual is T result)
            yield return result;

        foreach (var child in visual.GetVisualChildren())
        {
            foreach (var descendant in child.FindDescendantsOfType<T>())
            {
                yield return descendant;
            }
        }
    }
}

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

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        this.ViewModel.Collections.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(CollectionsViewModel.SelectedNode))
            {
                OnSelectedNodeChanged();
            }
        };
    }

    private async void BtnSave_OnClick(object? sender, RoutedEventArgs e)
    {
        var requestsManager = new RequestsManager();
        var selectedNode = this.ViewModel.Collections.SelectedNode;

        try
        {
            if (selectedNode != null)
            {
                if (selectedNode.IsFolder)
                {
                    // If a folder (collection) is selected, prompt for a filename and save to that collection
                    var saveDialog = new SaveRequestDialog();
                    var topLevel = TopLevel.GetTopLevel(this) as Window;
                    
                    if (topLevel == null) return;
                    
                    var requestName = await saveDialog.ShowDialog<string?>(topLevel);
                    
                    if (!string.IsNullOrEmpty(requestName))
                    {
                        var treeView = this.FindControl<TreeView>("CollectionsTreeView");
                        requestsManager.SaveRequestToCollection(this.ViewModel.Request, selectedNode.CollectionName!, requestName);
                        this.ViewModel.Collections.RefreshCollections(treeView);
                    }
                }
                else
                {
                    // If a file is selected, overwrite it
                    if (!string.IsNullOrEmpty(selectedNode.FilePath))
                    {
                        requestsManager.OverwriteRequest(this.ViewModel.Request, selectedNode.FilePath);
                        // No need to refresh collections since we're just overwriting
                    }
                }
            }
            else
            {
                // No selection, use the old behavior (save to default collection with dialog)
                var saveDialog = new SaveRequestDialog();
                var topLevel = TopLevel.GetTopLevel(this) as Window;
                
                if (topLevel == null) return;
                
                var result = await saveDialog.ShowDialog<string?>(topLevel);
                
                if (!string.IsNullOrEmpty(result))
                {
                    var treeView = this.FindControl<TreeView>("CollectionsTreeView");
                    requestsManager.SaveRequest(this.ViewModel.Request, result);
                    this.ViewModel.Collections.RefreshCollections(treeView);
                }
            }
        }
        catch (Exception ex)
        {
            // Handle save error - you might want to show an error dialog
            Debug.WriteLine($"Error saving request: {ex.Message}");
        }
    }
    
    private void OnSelectedNodeChanged()
    {
        var selectedNode = this.ViewModel.Collections.SelectedNode;
        if (selectedNode != null && !selectedNode.IsFolder && !string.IsNullOrEmpty(selectedNode.FilePath))
        {
            // Load the request when a file is selected
            var requestsManager = new RequestsManager();
            var loadedRequest = requestsManager.LoadRequest(selectedNode.FilePath);
            if (loadedRequest != null)
            {
                this.ViewModel.Request = loadedRequest;
            }
        }
    }
}