using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using RestWave.Services;
using RestWave.ViewModels;
using RestWave.ViewModels.Requests;

namespace RestWave.Views.Components;

public partial class CollectionList : UserControl
{
    private Node? _draggedNode;
    private readonly ConfigManager _configManager = new();
    private readonly HashSet<Node> _subscribedFolderNodes = new();

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
        // Apply persisted expanded folders state and subscribe to changes
        ApplyExpandedFoldersFromConfig();
        SubscribeFolderNodes();
        PersistExpandedFolders();
        // Restore last opened file selection
        ApplyLastOpenedFileFromConfig();
    }

    private void OnHttpViewLoaded(object? sender, RoutedEventArgs e)
    {
        SetupDragAndDrop();
    }

    private void UnsubscribeFolderNodes()
    {
        foreach (var node in _subscribedFolderNodes.ToList())
        {
            node.PropertyChanged -= NodeOnPropertyChanged;
        }
        _subscribedFolderNodes.Clear();
    }

    private void SubscribeFolderNodes()
    {
        UnsubscribeFolderNodes();
        foreach (var node in this.ViewModel.Collections.Where(n => n.IsFolder))
        {
            node.PropertyChanged += NodeOnPropertyChanged;
            _subscribedFolderNodes.Add(node);
        }
    }

    private void NodeOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Node.IsExpanded))
        {
            PersistExpandedFolders();
        }
    }

    private void PersistExpandedFolders()
    {
        var expanded = this.ViewModel.Collections
            .Where(n => n.IsFolder && n.IsExpanded)
            .Select(n => n.FilePath ?? n.Title)
            .ToList();
        _configManager.SaveExpandedFolders(expanded);
    }

    private void ApplyExpandedFoldersFromConfig()
    {
        var cfg = _configManager.LoadConfiguration();
        var expanded = cfg.ExpandedFolders ?? new List<string>();
        foreach (var node in this.ViewModel.Collections.Where(n => n.IsFolder))
        {
            var key = node.FilePath ?? node.Title;
            node.IsExpanded = expanded.Contains(key);
        }
    }

    private void RefreshAndWire(TreeView? treeView)
    {
        this.ViewModel.RefreshCollections(treeView);
        ApplyExpandedFoldersFromConfig();
        SubscribeFolderNodes();
        PersistExpandedFolders();
        ApplyLastOpenedFileFromConfig();
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
                RefreshAndWire(treeView);
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

            if (dataContext is Node node)
            {
                _draggedNode = node; // Allow dragging both files and folders
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
                // Prevent dropping a folder into itself or its descendants
                if (draggedNode.IsFolder && (draggedNode == targetNode || targetNode.IsDescendantOf(draggedNode)))
                {
                    return;
                }

                var requestsManager = new RequestsManager();
                bool success = false;

                if (draggedNode.IsFolder)
                {
                    // Move folder to target folder
                    success = requestsManager.MoveFolder(draggedNode.FilePath!, targetNode.FilePath!);
                }
                else
                {
                    // Move file to target folder
                    success = requestsManager.MoveFileToFolder(draggedNode.FilePath!, targetNode.FilePath!);
                }

                if (success)
                {
                    RefreshAndWire(treeView);
                    targetNode.IsExpanded = true;
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

    private void ApplyLastOpenedFileFromConfig()
    {
        var cfg = _configManager.LoadConfiguration();
        var lastPath = cfg.LastOpenedFilePath;
        if (string.IsNullOrWhiteSpace(lastPath))
            return;

        Node? target = null;
        Node? parentCollection = null;

        foreach (var collection in this.ViewModel.Collections.Where(n => n.IsFolder))
        {
            var match = collection.SubNodes?.FirstOrDefault(f => string.Equals(f.FilePath, lastPath, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                target = match;
                parentCollection = collection;
                break;
            }
        }

        if (target != null)
        {
            if (parentCollection != null)
            {
                parentCollection.IsExpanded = true;
            }
            this.ViewModel.SelectedNode = target;
        }
    }

    // Context Menu Event Handlers
    private void CreateNewRequest_Click(object? sender, RoutedEventArgs e)
    {
        var menuItem = sender as MenuItem;
        var node = menuItem?.DataContext as Node;

        // Fallback to selected node if context menu node is not available
        if (node == null)
        {
            node = this.ViewModel.SelectedNode;
        }

        if (node != null)
        {
            Node targetFolder = node;

            // If it's a file, use its parent folder
            if (!node.IsFolder && node.Parent != null)
            {
                targetFolder = node.Parent;
            }

            if (targetFolder.IsFolder && targetFolder.FilePath != null)
            {
                var requestName = $"New Request {DateTime.Now:HHmmss}";
                var requestsManager = new RequestsManager();

                try
                {
                    System.Diagnostics.Debug.WriteLine($"Creating request '{requestName}' in '{targetFolder.FilePath}'");

                    // Create a new empty request
                    var newRequest = new RequestViewModel
                    {
                        Url = "https://",
                        Method = "GET"
                    };

                    // Save the request to the specific folder
                    requestsManager.SaveRequestToFolder(newRequest, targetFolder.FilePath, requestName);

                    var treeView = this.FindControl<TreeView>("CollectionsTreeView");
                    RefreshAndWire(treeView);

                    // Expand the target folder and find the new request
                    targetFolder.IsExpanded = true;
                    ExpandNodeInTreeView(targetFolder);

                    // Find and rename the newly created request
                    var newRequestPath = Path.Combine(targetFolder.FilePath, $"{requestName}.json");
                    var newRequestNode = FindNodeByPath(this.ViewModel.Collections.ToList(), newRequestPath);

                    if (newRequestNode != null)
                    {
                        StartRenaming(newRequestNode);
                        System.Diagnostics.Debug.WriteLine("Found and starting rename of new request");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Could not find newly created request");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error creating new request: {ex.Message}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("No valid target folder found");
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("No node available for new request creation");
        }
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
        if (menuItem?.DataContext is Node node)
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
            // Ensure selection is updated so other components (HttpView) can react and persist
            this.ViewModel.SelectedNode = node;
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

    private void CreateNewFolder_Click(object? sender, RoutedEventArgs e)
    {
        var menuItem = sender as MenuItem;
        var parentNode = menuItem?.DataContext as Node;

        if (parentNode != null && parentNode.IsFolder && parentNode.FilePath != null)
        {
            var folderName = $"New Folder {DateTime.Now:HHmmss}";
            var requestsManager = new RequestsManager();

            try
            {
                System.Diagnostics.Debug.WriteLine($"Creating folder '{folderName}' in '{parentNode.FilePath}'");
                requestsManager.CreateNestedFolder(parentNode.FilePath, folderName);

                var treeView = this.FindControl<TreeView>("CollectionsTreeView");
                RefreshAndWire(treeView);

                // Find and rename the newly created folder
                var newFolderPath = Path.Combine(parentNode.FilePath, folderName);
                System.Diagnostics.Debug.WriteLine($"Looking for new folder at: {newFolderPath}");

                var newNode = FindNodeByPath(this.ViewModel.Collections.ToList(), newFolderPath);
                if (newNode != null)
                {
                    parentNode.IsExpanded = true;
                    ExpandNodeInTreeView(parentNode);
                    StartRenaming(newNode);
                    System.Diagnostics.Debug.WriteLine("Found and starting rename of new folder");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Could not find newly created folder");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating nested folder: {ex.Message}");
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"Parent node validation failed. ParentNode: {parentNode}, IsFolder: {parentNode?.IsFolder}, FilePath: {parentNode?.FilePath}");
        }
    }

    private Node? FindNodeByPath(Node parentNode, string targetPath)
    {
        if (parentNode.SubNodes == null)
            return null;

        foreach (var child in parentNode.SubNodes)
        {
            if (string.Equals(child.FilePath, targetPath, StringComparison.OrdinalIgnoreCase))
                return child;

            if (child.IsFolder && child.SubNodes != null)
            {
                var found = FindNodeByPath(child, targetPath);
                if (found != null)
                    return found;
            }
        }

        return null;
    }

    private Node? FindNodeByPath(IList<Node> nodes, string targetPath)
    {
        foreach (var node in nodes)
        {
            if (string.Equals(node.FilePath, targetPath, StringComparison.OrdinalIgnoreCase))
                return node;

            if (node.IsFolder && node.SubNodes != null)
            {
                var found = FindNodeByPath(node, targetPath);
                if (found != null)
                    return found;
            }
        }

        return null;
    }
}