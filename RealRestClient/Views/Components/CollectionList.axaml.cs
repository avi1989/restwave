using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using RealRestClient.Services;
using RealRestClient.ViewModels;

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
        foreach(var collection in collections)
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
}