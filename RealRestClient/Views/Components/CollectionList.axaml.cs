using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using RealRestClient.Services;
using RealRestClient.ViewModels;
using RealRestClient.ViewModels.Requests;

namespace RealRestClient.Views.Components;

public partial class CollectionList : UserControl
{
    private Node? _draggedNode;

    public CollectionList()
    {
        InitializeComponent();
        this.KeyDown += OnKeyDown;
        this.Loaded += OnHttpViewLoaded;
    }

    private CollectionsViewModel ViewModel => (CollectionsViewModel)DataContext!;

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        RequestsManager requestsManager = new();
        var collections = requestsManager.GetCollections();
        foreach (var collection in collections)
        {
            this.ViewModel.Collections.Add(collection);
        }
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
                var treeView = this.FindControl<TreeView>("CollectionsTreeView");
                node.StopEditing(true);
                this.ViewModel.RefreshCollections(treeView);
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
        var treeView = this.FindControl<TreeView>("CollectionsTreeView");
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
                    this.ViewModel.RefreshCollections(treeView);
                }
            }
        }
    }

    private void CaptureExpandedStateRecursive(ObservableCollection<Node>? nodes,
        Dictionary<string, bool> expandedState, string path)
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

    private void RestoreExpandedStateRecursive(ObservableCollection<Node>? nodes,
        Dictionary<string, bool> expandedState, string path)
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

    // Context Menu Event Handlers
    private void CreateNewRequest_Click(object? sender, RoutedEventArgs e)
    {
        var node = this.ViewModel.SelectedNode;

        // If the context menu was triggered on an item, use that item's collection
        // Otherwise, prompt user to select a collection or create in root
        string? targetCollection = null;

        if (node != null)
        {
            // If it's a folder, use it as the target collection
            if (node.IsFolder)
            {
                targetCollection = node.CollectionName ?? node.Title;
            }
            else
            {
                // If it's a file, use its parent collection
                targetCollection = node.CollectionName;
            }
        }
       
        // Create a new request dialog or add to the selected collection
        var newRequest = CreateNewRequestInCollection(targetCollection);
        
        if (node != null)
        {
            node.IsExpanded = true;
            ExpandNodeInTreeView(node);

        }
        var requestNode = this.ViewModel.Collections.First(x => x.Title == targetCollection)?.SubNodes?.First(x => x.Title == newRequest)!;
        StartRenaming(requestNode);
    }
    
    private void ExpandNodeInTreeView(Node node)
    {
        var treeView = this.FindControl<TreeView>("CollectionsTreeView");
        if (treeView != null)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var treeViewItems = treeView.FindDescendantsOfType<TreeViewItem>();
                foreach (var item in treeViewItems)
                {
                    if ((item.DataContext as Node)?.FilePath == node.FilePath)
                    {
                        item.IsExpanded = true;
                        break;
                    }
                }
            }, DispatcherPriority.Background);
        }
    }


    private void Rename_Click(object? sender, RoutedEventArgs e)
    {
        var menuItem = sender as MenuItem;
        var node = menuItem.DataContext as Node;

        if (node != null)
        {
            StartRenaming(node);
        }
    }

    private void Open_Click(object? sender, RoutedEventArgs e)
    {
        var menuItem = sender as MenuItem;
        var contextMenu = menuItem?.Parent as ContextMenu;
        var node = contextMenu?.PlacementTarget?.DataContext as Node;

        if (node != null && !node.IsFolder)
        {
            // Open the request file
            OpenRequest(node);
        }
        else if (node != null && node.IsFolder)
        {
            // Toggle folder expansion
            node.IsExpanded = !node.IsExpanded;
        }
    }

    private async void Delete_Click(object? sender, RoutedEventArgs e)
    {
        var menuItem = sender as MenuItem;

        if (menuItem?.DataContext is Node node)
        {
            await DeleteNode(node);
        }
    }

    private string CreateNewRequestInCollection(string? collectionName)
    {
        // This method should open a dialog to create a new request
        // You might want to integrate this with your existing request creation logic
        var requestsManager = new RequestsManager();

        // For now, create a simple default request
        // You may want to show a dialog here to get the request name and details
        var requestName = $"New Request {DateTime.Now:HHmmss}";

        if (string.IsNullOrEmpty(collectionName))
        {
            // If no collection specified, use Default collection
            collectionName = "Default";
        }

        // Create a new empty request
        var newRequest = new RequestViewModel
        {
            Url = "https://",
            Method = "GET"
        };

        // Save the new request
        requestsManager.SaveRequestToCollection(newRequest, collectionName, requestName);

        var treeView = this.FindControl<TreeView>("CollectionsTreeView");
        this.ViewModel.RefreshCollections(treeView);
        return requestName;
    }

    private void OpenRequest(Node node)
    {
        if (node.FilePath != null)
        {
            // Load and display the request
            var requestsManager = new RequestsManager();
            var request = requestsManager.LoadRequest(node.FilePath);

            // You'll need to implement this method to set the current request in your main view
            // This might involve navigating to the request view or updating the current request context
            SetCurrentRequest(request, node);
        }
    }

    private async Task DeleteNode(Node node)
    {
        // Show confirmation dialog
        var result = await ShowConfirmationDialog($"Are you sure you want to delete '{node.Title}'?");

        if (result)
        {
            bool success = false;

            try
            {
                if (node.IsFolder && node.FilePath != null)
                {
                    // Delete entire collection directory
                    Directory.Delete(node.FilePath, true);
                    success = true;
                }
                else if (!node.IsFolder && node.FilePath != null)
                {
                    // Delete individual request file
                    File.Delete(node.FilePath);
                    success = true;
                }
            }
            catch
            {
                success = false;
            }

            if (success)
            {
                var treeView = this.FindControl<TreeView>("CollectionsTreeView");
                this.ViewModel.RefreshCollections(treeView);
            }
        }
    }

    private void SetCurrentRequest(object? request, Node node)
    {
        // TODO: Implement this method to set the current request in the main application
        // This might involve raising an event or calling a method on a parent view model

        // For example, you might want to:
        // 1. Find the parent window or main view model
        // 2. Set the current request there
        // 3. Navigate to the request editing view

        // Placeholder implementation:
        if (TopLevel.GetTopLevel(this) is Window window && window.DataContext is AppViewModel appViewModel)
        {
            // Assuming your AppViewModel has a way to set the current request
            // appViewModel.SetCurrentRequest(request);
        }
    }

    private async Task<bool> ShowConfirmationDialog(string message)
    {
        // Simple confirmation dialog implementation
        // You might want to use a more sophisticated dialog system
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window != null)
        {
            var yesButton = new Button { Content = "Yes", Margin = new Thickness(0, 0, 10, 0) };
            var noButton = new Button { Content = "No" };
            var dialog = new Window
            {
                Title = "Confirm Delete",
                Width = 400,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new DockPanel
                {
                    Margin = new Thickness(20),
                    Children =
                    {
                        new TextBlock
                        {
                            Text = message, Margin = new Thickness(0, 0, 0, 20), TextWrapping = TextWrapping.Wrap,
                            [DockPanel.DockProperty] = Dock.Top
                        },
                        new StackPanel
                        {
                            Orientation = Avalonia.Layout.Orientation.Horizontal,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom,
                            [DockPanel.DockProperty] = Dock.Bottom,
                            Children =
                            {
                                yesButton,
                                noButton,
                            }
                        }
                    }
                }
            };

            bool result = false;
            yesButton.Click += (s, e) =>
            {
                result = true;
                dialog.Close();
            };
            noButton.Click += (s, e) =>
            {
                result = false;
                dialog.Close();
            };

            await dialog.ShowDialog(window);
            return result;
        }

        return false;
    }

    private void CreateNewCollection_Click(object? sender, RoutedEventArgs e)
    {
        var collectionName = $"Collection {DateTime.Now:HHmmss}";
        var requestsManager = new RequestsManager();
        requestsManager.CreateCollection(collectionName);
        var treeView = this.FindControl<TreeView>("CollectionsTreeView");
        this.ViewModel.RefreshCollections(treeView);
        var node = this.ViewModel.Collections.First(n => n.Title == collectionName && n.IsFolder);

        StartRenaming(node);
    }
}