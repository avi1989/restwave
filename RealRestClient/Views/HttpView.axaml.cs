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
    private Node? _draggedNode;

    public HttpView()
    {
        InitializeComponent();
        this.configManager = new ConfigManager();
        this.config = configManager.LoadConfiguration();
        
        // Add keyboard event handling
        this.KeyDown += OnKeyDown;
        
        // Set up drag and drop after initialization
        this.Loaded += OnHttpViewLoaded;
    }

    private void OnHttpViewLoaded(object? sender, RoutedEventArgs e)
    {
        SetupDragAndDrop();
    }

    private void SetupDragAndDrop()
    {
        var treeView = this.FindControl<TreeView>("CollectionsTreeView");
        if (treeView != null)
        {
            DragDrop.SetAllowDrop(treeView, true);
            
            treeView.AddHandler(DragDrop.DragOverEvent, TreeView_DragOver);
            treeView.AddHandler(DragDrop.DropEvent, TreeView_Drop);
            treeView.AddHandler(InputElement.PointerPressedEvent, TreeView_PointerPressed, RoutingStrategies.Tunnel);
            treeView.AddHandler(InputElement.PointerMovedEvent, TreeView_PointerMoved, RoutingStrategies.Tunnel);
        }
    }

    private void TreeView_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Visual visual && visual is IInputElement inputElement)
        {
            var hitTest = inputElement.InputHitTest(e.GetPosition(visual));
            var dataContext = (hitTest as Control)?.DataContext;
            
            if (dataContext is Node node && !node.IsFolder)
            {
                _draggedNode = node;
            }
            else
            {
                _draggedNode = null;
            }
        }
    }

    private async void TreeView_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_draggedNode != null && sender is Visual visual && e.GetCurrentPoint(visual).Properties.IsLeftButtonPressed)
        {
            var dragData = new DataObject();
            dragData.Set("Node", _draggedNode);
            
            var result = await DragDrop.DoDragDrop(e, dragData, DragDropEffects.Move);
            
            if (result == DragDropEffects.Move)
            {
                _draggedNode = null;
            }
        }
    }

    private void TreeView_DragOver(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains("Node") && sender is Visual visual && visual is IInputElement inputElement)
        {
            var hitTest = inputElement.InputHitTest(e.GetPosition(visual));
            var dataContext = (hitTest as Control)?.DataContext;
            
            if (dataContext is Node targetNode && targetNode.IsFolder)
            {
                e.DragEffects = DragDropEffects.Move;
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void TreeView_Drop(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains("Node") && sender is Visual visual && visual is IInputElement inputElement)
        {
            var draggedNode = e.Data.Get("Node") as Node;
            var hitTest = inputElement.InputHitTest(e.GetPosition(visual));
            var dataContext = (hitTest as Control)?.DataContext;
            
            if (draggedNode != null && dataContext is Node targetNode && targetNode.IsFolder)
            {
                // Move the file to the target collection
                var requestsManager = new RequestsManager();
                if (requestsManager.MoveFile(draggedNode.FilePath!, targetNode.CollectionName!))
                {
                    RefreshCollections();
                }
            }
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.F2 && ViewModel.SelectedNode != null)
        {
            StartRenaming(ViewModel.SelectedNode);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter || e.Key == Key.Escape)
        {
            if (ViewModel.SelectedNode?.IsEditing == true)
            {
                FinishRenaming(ViewModel.SelectedNode, e.Key == Key.Enter);
                e.Handled = true;
            }
        }
    }

    private void StartRenaming(Node node)
    {
        node.StartEditing();
        
        // Find the editing TextBox and focus it
        var treeView = this.FindControl<TreeView>("CollectionsTreeView");
        if (treeView != null)
        {
            // Use a short delay to ensure the UI has been updated
            Dispatcher.UIThread.Post(() =>
            {
                // Find all visible TextBoxes with the right name
                var allTextBoxes = treeView.FindDescendantsOfType<TextBox>();
                foreach (var textBox in allTextBoxes)
                {
                    if (textBox.Name == "EditingTextBox" && textBox.IsVisible && textBox.DataContext == node)
                    {
                        textBox.Focus();
                        textBox.SelectAll();
                        break;
                    }
                }
            }, DispatcherPriority.Background);
        }
    }

    private void FinishRenaming(Node node, bool save)
    {
        if (save && !string.IsNullOrWhiteSpace(node.EditingText))
        {
            var requestsManager = new RequestsManager();
            bool success = false;
            
            if (node.IsFolder)
            {
                success = requestsManager.RenameCollection(node.FilePath!, node.EditingText.Trim());
            }
            else
            {
                success = requestsManager.RenameFile(node.FilePath!, node.EditingText.Trim());
            }
            
            if (success)
            {
                node.StopEditing(true);
                RefreshCollections();
            }
            else
            {
                node.StopEditing(false);
                // Could show an error message here
            }
        }
        else
        {
            node.StopEditing(false);
        }
    }

    private void EditingTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is TextBox textBox && textBox.DataContext is Node node)
        {
            if (e.Key == Key.Enter)
            {
                FinishRenaming(node, true);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                FinishRenaming(node, false);
                e.Handled = true;
            }
        }
    }

    private void EditingTextBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox && textBox.DataContext is Node node)
        {
            FinishRenaming(node, true);
        }
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
        
        // Subscribe to selection changes
        this.ViewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(HttpViewModel.SelectedNode))
            {
                OnSelectedNodeChanged();
            }
        };
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

    private async void BtnSave_OnClick(object? sender, RoutedEventArgs e)
    {
        var requestsManager = new RequestsManager();
        var selectedNode = this.ViewModel.SelectedNode;

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
                        requestsManager.SaveRequestToCollection(this.ViewModel.Request, selectedNode.CollectionName!, requestName);
                        RefreshCollections();
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
                    requestsManager.SaveRequest(this.ViewModel.Request, result);
                    RefreshCollections();
                }
            }
        }
        catch (Exception ex)
        {
            // Handle save error - you might want to show an error dialog
            Debug.WriteLine($"Error saving request: {ex.Message}");
        }
    }

    private void RefreshCollections()
    {
        var treeView = this.FindControl<TreeView>("CollectionsTreeView");
        
        // Capture expanded state before refreshing
        var expandedState = new Dictionary<string, bool>();
        if (treeView != null)
        {
            CaptureTreeViewExpandedState(treeView, expandedState);
        }
        
        this.ViewModel.Collections.Clear();
        RequestsManager requestsManager = new();
        var collections = requestsManager.GetCollections();
        foreach (var collection in collections)
        {
            this.ViewModel.Collections.Add(collection);
        }
        
        // Restore expanded state after a short delay to allow UI to update
        if (treeView != null && expandedState.Count > 0)
        {
            Dispatcher.UIThread.Post(() =>
            {
                RestoreTreeViewExpandedState(treeView, expandedState);
            }, DispatcherPriority.Background);
        }
    }

    private void CaptureTreeViewExpandedState(TreeView treeView, Dictionary<string, bool> expandedState)
    {
        var treeViewItems = treeView.FindDescendantsOfType<TreeViewItem>().ToList();
        foreach (var item in treeViewItems)
        {
            if (item.DataContext is Node node && node.IsFolder)
            {
                var path = GetNodePath(node);
                expandedState[path] = item.IsExpanded;
            }
        }
    }

    private void RestoreTreeViewExpandedState(TreeView treeView, Dictionary<string, bool> expandedState)
    {
        var treeViewItems = treeView.FindDescendantsOfType<TreeViewItem>().ToList();
        foreach (var item in treeViewItems)
        {
            if (item.DataContext is Node node && node.IsFolder)
            {
                var path = GetNodePath(node);
                if (expandedState.TryGetValue(path, out bool wasExpanded))
                {
                    item.IsExpanded = wasExpanded;
                }
            }
        }
    }

    private string GetNodePath(Node node)
    {
        // Create a unique path for the node based on its hierarchy
        var path = node.Title;
        if (!string.IsNullOrEmpty(node.CollectionName))
        {
            path = $"{node.CollectionName}/{node.Title}";
        }
        return path;
    }

    private Dictionary<string, bool> CaptureExpandedState(ObservableCollection<Node> nodes)
    {
        var expandedState = new Dictionary<string, bool>();
        CaptureExpandedStateRecursive(nodes, expandedState, "");
        return expandedState;
    }

    private void CaptureExpandedStateRecursive(ObservableCollection<Node>? nodes, Dictionary<string, bool> expandedState, string path)
    {
        if (nodes == null) return;
        
        foreach (var node in nodes)
        {
            var nodePath = string.IsNullOrEmpty(path) ? node.Title : $"{path}/{node.Title}";
            if (node.IsFolder)
            {
                expandedState[nodePath] = node.IsExpanded;
                if (node.SubNodes != null)
                {
                    CaptureExpandedStateRecursive(node.SubNodes, expandedState, nodePath);
                }
            }
        }
    }

    private void RestoreExpandedState(ObservableCollection<Node> nodes, Dictionary<string, bool> expandedState)
    {
        RestoreExpandedStateRecursive(nodes, expandedState, "");
    }

    private void RestoreExpandedStateRecursive(ObservableCollection<Node>? nodes, Dictionary<string, bool> expandedState, string path)
    {
        if (nodes == null) return;
        
        foreach (var node in nodes)
        {
            var nodePath = string.IsNullOrEmpty(path) ? node.Title : $"{path}/{node.Title}";
            if (node.IsFolder && expandedState.TryGetValue(nodePath, out bool isExpanded))
            {
                node.IsExpanded = isExpanded;
                if (node.SubNodes != null)
                {
                    RestoreExpandedStateRecursive(node.SubNodes, expandedState, nodePath);
                }
            }
        }
    }

    private void OnSelectedNodeChanged()
    {
        var selectedNode = this.ViewModel.SelectedNode;
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