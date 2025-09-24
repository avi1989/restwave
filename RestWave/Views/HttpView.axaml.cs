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
using RestWave.Models;
using RestWave.Services;
using RestWave.ViewModels;

namespace RestWave.Views;

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
    private readonly HistoryManager historyManager;
    private readonly SessionManager sessionManager;

    public HttpView()
    {
        InitializeComponent();
        this.configManager = new ConfigManager();
        this.historyManager = new HistoryManager();
        this.sessionManager = new SessionManager();
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

        // Update session with current request
        sessionManager.UpdateCurrentRequest(viewModel.Request);

        viewModel.Response.StatusCode = string.Empty;
        viewModel.Response.Body = string.Empty;
        viewModel.Response.Headers.Clear();
        viewModel.Response.StartOperation(); // This creates the cancellation token and sets IsLoading = true

        var stopwatch = Stopwatch.StartNew();
        string statusCode = string.Empty;
        string errorMessage = string.Empty;
        bool isSuccess = false;
        long responseSize = 0;
        var responseHeaders = new Dictionary<string, string>();
        string responseBody = string.Empty;

        try
        {
            var request = new HttpRequestMessage(new HttpMethod(viewModel.Request.Method),
                viewModel.Request.Url);
            foreach (var header in viewModel.Request.HeadersInput.Headers)
            {
                if (!string.IsNullOrWhiteSpace(header.Value) && !string.IsNullOrWhiteSpace(header.Key))
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }

            if (viewModel.Request.IsBodyEnabled)
            {
                request.Content = new StringContent(viewModel.Request.JsonBodyInput.JsonText,
                    System.Text.Encoding.UTF8, "application/json");
            }

            var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
                viewModel.Response.CancellationToken).ConfigureAwait(false);
            var responseContentType = response.Content.Headers.ContentType?.MediaType ?? "text/plain";

            statusCode = response.StatusCode.ToString();
            isSuccess = response.IsSuccessStatusCode;

            // Collect response headers for history
            foreach (var header in response.Headers)
            {
                responseHeaders[header.Key] = string.Join(", ", header.Value.ToList());
            }

            // Update UI properties on UI thread
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                viewModel.Response.StatusCode = statusCode;
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
                responseSize = System.Text.Encoding.UTF8.GetByteCount(content);
                responseBody = content;
                
                var bodyContent = response.IsSuccessStatusCode
                    ? content
                    : $"Error {response.StatusCode}: {content}";

                viewModel.Response.Body = bodyContent;
            }
            else
            {
                viewModel.Response.IsStreaming = true;
                var streamedContent = new System.Text.StringBuilder();

                await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var reader = new StreamReader(stream);

                while (!viewModel.Response.CancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync().ConfigureAwait(false);
                    if (line == null) break;

                    if (!string.IsNullOrEmpty(line))
                    {
                        responseSize += System.Text.Encoding.UTF8.GetByteCount(line);
                        streamedContent.AppendLine(line); // Collect content for history
                        
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
                
                // Set the complete streamed content for history
                responseBody = streamedContent.ToString();
            }
        }
        catch (Exception ex)
        {
            isSuccess = false;
            errorMessage = ex.Message;
            statusCode = "Error";
            viewModel.Response.Body = $"Exception: {ex.Message}";
            Debug.WriteLine($"Exception: {ex.Message}");
        }
        finally
        {
            stopwatch.Stop();
            viewModel.Response.CompleteOperation(); // This sets IsLoading = false and cleans up

            // Record request history
            try
            {
                var selectedNode = viewModel.Collections.SelectedNode;
                var collectionName = selectedNode?.CollectionName ?? string.Empty;
                var requestName = selectedNode?.Title ?? string.Empty;

                await historyManager.SaveRequestHistoryAsync(
                    viewModel.Request,
                    statusCode,
                    stopwatch.ElapsedMilliseconds,
                    responseSize,
                    isSuccess,
                    errorMessage,
                    responseHeaders,
                    responseBody,
                    collectionName,
                    requestName
                );

                // Refresh the history pane after saving the new request
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await viewModel.RefreshRequestHistoryAsync();
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save request history: {ex.Message}");
            }
        }
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        // Load session and restore state
        await sessionManager.LoadSessionAsync();
        
        // Restore current request from session if no specific request is loaded
        if (string.IsNullOrEmpty(ViewModel.Request.Url))
        {
            var restoredRequest = sessionManager.RestoreCurrentRequest();
            if (!string.IsNullOrEmpty(restoredRequest.Url))
            {
                ViewModel.Request = restoredRequest;
            }
        }
        
        this.ViewModel.Collections.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(CollectionsViewModel.SelectedNode))
            {
                OnSelectedNodeChanged();
            }
        };
        
        // Track request changes for session management
        this.ViewModel.Request.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(ViewModel.Request.Url) ||
                args.PropertyName == nameof(ViewModel.Request.Method))
            {
                sessionManager.UpdateCurrentRequest(ViewModel.Request);
            }
        };

        // Bind the history list to the ViewModel's RequestHistory
        var historyList = this.FindControl<ItemsControl>("RequestHistoryList");
        if (historyList != null)
        {
            historyList.ItemsSource = ViewModel.RequestHistory;
        }

        // Handle empty state visibility
        ViewModel.RequestHistory.CollectionChanged += (s, e) =>
        {
            UpdateEmptyStateVisibility();
        };

        // Set up history pane visibility
        var historyPane = this.FindControl<Border>("HistoryPane");
        var historySplitter = this.FindControl<GridSplitter>("HistorySplitter");
        var grid = historyPane?.Parent as Grid;
        
        if (historyPane != null && historySplitter != null && grid != null)
        {
            // Initially hide the history pane and collapse columns
            historyPane.IsVisible = false;
            historySplitter.IsVisible = false;
            
            // Collapse the history column and splitter column initially
            if (grid.ColumnDefinitions.Count >= 5)
            {
                grid.ColumnDefinitions[3].Width = new GridLength(0); // Splitter column
                grid.ColumnDefinitions[4].Width = new GridLength(0); // History column
            }
        }
        
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

    private async void BtnHistory_OnClick(object? sender, RoutedEventArgs e)
    {
        // Show the history pane
        var historyPane = this.FindControl<Border>("HistoryPane");
        var historySplitter = this.FindControl<GridSplitter>("HistorySplitter");
        var grid = historyPane?.Parent as Grid;
        
        if (historyPane != null && historySplitter != null && grid != null)
        {
            if (historyPane.IsVisible == true)
            {
                HideHistory();
            }
            else
            {
                historyPane.IsVisible = true;
                historySplitter.IsVisible = true;

                // Restore the history column and splitter column widths
                if (grid.ColumnDefinitions.Count >= 5)
                {
                    grid.ColumnDefinitions[3].Width = GridLength.Auto; // Splitter column
                    grid.ColumnDefinitions[4].Width = new GridLength(300); // History column
                }

                // Refresh the request history for the current URL
                await ViewModel.RefreshRequestHistoryAsync();
            }
        }
    }

    private async void BtnViewAllHistory_OnClick(object? sender, RoutedEventArgs e)
    {
        var historyWindow = new HistoryWindow();
        historyWindow.RequestReplay += (s, request) =>
        {
            ViewModel.Request = request;
        };
        
        var topLevel = TopLevel.GetTopLevel(this) as Window;
        if (topLevel != null)
        {
            await historyWindow.ShowDialog(topLevel);
        }
    }

    private void BtnHideHistory_OnClick(object? sender, RoutedEventArgs e)
    {
        this.HideHistory();
    }

    private void HideHistory()
    {
        var historyPane = this.FindControl<Border>("HistoryPane");
        var historySplitter = this.FindControl<GridSplitter>("HistorySplitter");
        var grid = historyPane?.Parent as Grid;
        
        if (historyPane != null && historySplitter != null && grid != null)
        {
            historyPane.IsVisible = false;
            historySplitter.IsVisible = false;
            
            // Collapse the history column and splitter column to give space back to main content
            if (grid.ColumnDefinitions.Count >= 5)
            {
                grid.ColumnDefinitions[3].Width = new GridLength(0); // Splitter column
                grid.ColumnDefinitions[4].Width = new GridLength(0); // History column
            }
        }
    }

    private void HistoryItem_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.Tag is RequestHistoryItem historyItem)
        {
            ViewModel.ReplayHistoryCommand.Execute(historyItem);
        }
    }

    private void UpdateEmptyStateVisibility()
    {
        var emptyMessage = this.FindControl<StackPanel>("EmptyHistoryMessage");
        if (emptyMessage != null)
        {
            emptyMessage.IsVisible = ViewModel.RequestHistory.Count == 0;
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
