using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
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
        var viewModel = this.ViewModel;

        if (viewModel.Response.IsLoading)
        {
            // If currently loading, cancel the operation
            viewModel.Response.CancelOperation();
            return;
        }

        viewModel.Response.StatusCode = string.Empty;
        viewModel.Response.Body = string.Empty;
        viewModel.Response.Headers.Clear();
        viewModel.Response.StartOperation(); // This creates the cancellation token and sets IsLoading = true

        try
        {
            var request = new HttpRequestMessage(new HttpMethod(viewModel.Request.Method),
                viewModel.Request.Url);
            foreach (var header in viewModel.Request.HeadersInput.Headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }

            if (viewModel.Request.IsBodyEnabled)
            {
                request.Content = new StringContent(viewModel.Request.JsonBodyInput.JsonText,
                    System.Text.Encoding.UTF8, "application/json");
            }

            var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
                viewModel.Response.CancellationToken).ConfigureAwait(false);
            var responseContentType = response.Content.Headers.ContentType?.MediaType ?? "text/plain";

            // Update UI properties on UI thread
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                viewModel.Response.StatusCode = response.StatusCode.ToString();
                foreach (var header in response.Headers)
                {
                    var newItem =
                        new KeyValuePair<string, string>(header.Key, string.Join(", ", header.Value.ToList()));
                    viewModel.Response.Headers.Add(newItem);
                }
            });

            if (responseContentType != "text/event-stream")
            {
                viewModel.Response.IsStreaming = false;
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var bodyContent = response.IsSuccessStatusCode
                    ? content
                    : $"Error {response.StatusCode}: {content}";

                viewModel.Response.Body = bodyContent;
            }
            else
            {
                viewModel.Response.IsStreaming = true;

                await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var reader = new StreamReader(stream);

                while (!viewModel.Response.CancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync().ConfigureAwait(false);
                    if (line == null) break;

                    if (!string.IsNullOrEmpty(line))
                    {
                        // Capture the line in a local variable for the closure
                        var capturedLine = line;
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            viewModel.Response.AppendStreamLine(capturedLine);
                        });
                    }

                    // Add a small delay to prevent overwhelming the UI thread
                    await Task.Delay(1, viewModel.Response.CancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex)
        {
            viewModel.Response.Body = $"Exception: {ex.Message}";
            Debug.WriteLine($"Exception: {ex.Message}");
        }
        finally
        {
            viewModel.Response.CompleteOperation(); // This sets IsLoading = false and cleans up
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
        // In case the selection was set before this handler was attached (e.g., restored from config),
        // load the request now.
        OnSelectedNodeChanged();
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
                        requestsManager.SaveRequestToCollection(this.ViewModel.Request, selectedNode.CollectionName!,
                            requestName);
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
            // Persist last opened file path
            try
            {
                this.configManager.SaveLastOpenedFilePath(selectedNode.FilePath);
            }
            catch
            {
                // Ignore persistence errors
            }

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